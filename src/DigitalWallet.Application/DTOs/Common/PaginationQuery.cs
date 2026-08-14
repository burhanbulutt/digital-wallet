namespace DigitalWallet.Application.DTOs.Common;
public record PaginationQuery
{
    public const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly int _pageSize = DefaultPageSize;
    private readonly int _page = 1;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}