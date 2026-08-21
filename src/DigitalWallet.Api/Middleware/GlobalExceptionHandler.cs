using System.Diagnostics;
using DigitalWallet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        // this is only for client disconnection. If server cancels the operation this block is skipped and
        // OperationCanceledException is mapped to 500.
        if (exception is OperationCanceledException
            && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request {Path} aborted by the client.", context.Request.Path);
            return true;
        }

        if (context.Response.HasStarted)
        {
            _logger.LogError(exception,
                "Exception after the response started; cannot write ProblemDetails.");
            return false;
        }

        var (status, title, mapped) = Map(exception);

        if (!mapped && exception is DomainException)
        {
            _logger.LogError(
                "{Type} has no explicit arm in {Handler} and defaulted to 400. "
              + "Add one, or confirm 400 is right for it.",
                exception.GetType().Name, nameof(GlobalExceptionHandler));
        }

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        // no explicit logging for 4xx, because they are in Process log table

        var detail = exception is DomainException
            ? exception.Message
            : "An unexpected error occurred.";

        context.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
                }
            }
        });
    }

    private static (int Status, string Title, bool Mapped) Map(Exception exception) => exception switch
    {
        CardNotFoundException or CardHolderNotFoundException
            or UnauthorizedCardAccessException // A 403 reveals the card exists
            => (StatusCodes.Status404NotFound, "Resource not found.", true),

        ConcurrencyConflictException or DuplicateCardException
            => (StatusCodes.Status409Conflict,
                "The operation conflicted with the current state.", true),

        InsufficientBalanceException or BudgetExceededException
            or CreditLimitExceededException or CardLimitExceededException
            or CardStateConflictException
            => (StatusCodes.Status409Conflict,
                "The account state does not allow this operation.", true),

        InvalidAmountException or InvalidCardException
            or InvalidMainCardException or InvalidTransferException
            => (StatusCodes.Status400BadRequest, "The request is not valid.", true),

        DomainException
            => (StatusCodes.Status400BadRequest, "The request is not valid.", false),

        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", false)
    };
}
