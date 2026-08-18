using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CardHolder> CardHolders => Set<CardHolder>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardTransaction> CardTransactions => Set<CardTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<ProcessLog> ProcessLogs => Set<ProcessLog>();

    private const string UtcNowSql = "SYSUTCDATETIME() AT TIME ZONE 'UTC'";

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        // 1. CardHolder Mapping
        modelBuilder.Entity<CardHolder>(entity =>
        {
            entity.ToTable("CardHolder");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)").IsRequired();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNowSql);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.CustomerNo).IsUnique().HasDatabaseName("IX_CardHolder_CustomerNo");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_CardHolder_Username");

            // Filtered unique index on Email (ignores NULLs)
            entity.HasIndex(e => e.Email)
                  .IsUnique().HasDatabaseName("IX_CardHolder_Email")
                  .HasFilter("[IsDeleted] = 0 AND [Email] IS NOT NULL");

            // Soft delete global filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 2. Card Mapping
        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("Card");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CardHolderId).IsRequired();
            entity.Property(e => e.CardNumberHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
            entity.Property(e => e.Last4).HasMaxLength(4).IsUnicode(false).IsFixedLength().IsRequired();
            entity.Property(e => e.ExpiryYear).IsRequired();
            entity.Property(e => e.ExpiryMonth).IsRequired();
            // Optimistic concurrency row version
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e=> e.MainCardId).IsRequired(false);

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNowSql);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Enum Converters & Defaults
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValueSql("'Active'");

            // C = Credit, D = Debit, V = Virtual. CHK_Card_CardType in the database
            // is what guards against a value outside this set.
            entity.Property(e => e.CardType)
                  .HasConversion(
                      v => v == CardType.Credit ? "C" : v == CardType.Debit ? "D" : "V",
                      v => v == "C" ? CardType.Credit : v == "D" ? CardType.Debit : CardType.Virtual
                  )
                  .HasMaxLength(1)
                  .IsFixedLength()
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.Brand)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            entity.HasOne(c => c.CardHolder)
                  .WithMany(ch => ch.Cards)
                  .HasForeignKey(c => c.CardHolderId);
                  //.HasPrincipalKey(ch => ch.Id);// no need for this since CardHolder.Id is the primary key
                  // if you are gonna use this, you should add hasAlternateKey to targeted column.

            // Self-reference: a virtual card points at the credit card it draws from.
            entity.HasOne(c => c.MainCard)
                  .WithMany(c => c.VirtualCards)
                  .HasForeignKey(c => c.MainCardId);

            entity.HasIndex(e => e.CardHolderId)
                  .HasDatabaseName("IX_Card_CardHolderId")
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(e => e.MainCardId).HasDatabaseName("IX_Card_MainCardId")
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(e => e.CardNumberHash)
                  .IsUnique().HasDatabaseName("IX_Card_CardNumberHash");

            // Soft delete global filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 3. CardTransaction Mapping
        modelBuilder.Entity<CardTransaction>(entity =>
        {
            entity.ToTable("CardTransaction");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CardId).IsRequired();

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNowSql);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.Property(e => e.Category)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            // Incoming / Outgoing. CHK_CardTransaction_Direction guards the set.
            entity.Property(e => e.Direction)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsUnicode(false)
                  .IsRequired();

            // Foreign Key: CardTransaction -> Card
            entity.HasOne(t => t.Card)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(t => t.CardId);

            entity.HasIndex(e => new { e.CardId, e.TransactionDate })
                  .HasDatabaseName("IX_CardTransaction_CardId_TransactionDate")
                  .IsDescending(false, true)
                  .HasFilter("[IsDeleted] = 0");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 4. Budget Mapping
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("Budget");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LimitAmount).HasColumnType("decimal(18,2)").IsRequired();
            // Budget is the contended row for every spend and every virtual card
            // allocation, so the concurrency guard belongs here.
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.WarningThreshold80).HasDefaultValue(false);
            entity.Property(e => e.WarningThreshold100).HasDefaultValue(false);
            entity.Property(e => e.CardId).IsRequired();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNowSql);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.Property(e => e.SpentAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired()
                  .HasDefaultValue(0m);

            // Credit cards only: the sum of child virtual card limits.
            entity.Property(e => e.ReservedAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired()
                  .HasDefaultValue(0m);

            entity.HasOne(b => b.Card)
                  .WithOne(c => c.Budget)
                  .HasForeignKey<Budget>(b => b.CardId);

            // One budget per card. Filtered so a soft-deleted budget does not
            // block creating a replacement for the same card.
            entity.HasIndex(b => b.CardId)
                  .IsUnique().HasDatabaseName("IX_Budget_CardId")
                  .HasFilter("[IsDeleted] = 0");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 5. Transfer Mapping
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("Transfer");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FromCardId).IsRequired();
            entity.Property(e => e.ToCardId).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.TransferDate).IsRequired();

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValueSql("'Pending'");

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNowSql);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(t => t.FromCard)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict) // cascade would delete one leg of transfer.
                  .HasForeignKey(t => t.FromCardId);

            entity.HasOne(t => t.ToCard)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasForeignKey(t => t.ToCardId);

            entity.HasIndex(e => new { e.FromCardId, e.TransferDate })
                  .HasDatabaseName("IX_Transfer_FromCardId_TransferDate")
                  .IsDescending(false, true)
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(e => new { e.ToCardId, e.TransferDate })
                  .HasDatabaseName("IX_Transfer_ToCardId_TransferDate")
                  .IsDescending(false, true)
                  .HasFilter("[IsDeleted] = 0");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 6. ProcessLog Mapping 
        modelBuilder.Entity<ProcessLog>(entity =>
        {
            entity.ToTable("ProcessLog");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityId).IsRequired(false);

            entity.Property(e => e.Timestamp)
                  .IsRequired()
                  .HasDefaultValueSql(UtcNowSql);

            entity.Property(e => e.Message).HasMaxLength(500).IsRequired();

            entity.Property(e => e.Level)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.ProcessName)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();
        });
    }
}