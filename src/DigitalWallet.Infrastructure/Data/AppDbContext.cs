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

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        // 1. CardHolder Mapping
        modelBuilder.Entity<CardHolder>(entity =>
        {
            entity.ToTable("CardHolder");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerNo).HasMaxLength(50).IsRequired();
            entity.HasAlternateKey(e => e.CustomerNo); 

            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)").IsRequired();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Filtered unique index on Email (ignores NULLs)
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasFilter("[Email] IS NOT NULL");

            // Soft delete global filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 2. Card Mapping
        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("Card");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CardHolderCusNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CardNumber).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => e.CardNumber).IsUnique();

            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)").IsRequired();

            // Enum Converters & Defaults
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValueSql("'Pending'"); // Ensures default is 'Pending' in SQL

            entity.Property(e => e.CardType)
                  .HasConversion(
                      v => v == CardType.Virtual ? "V" : "P",
                      v => v == "V" ? CardType.Virtual : CardType.Physical
                  )
                  .HasMaxLength(1)
                  .IsRequired();

            entity.Property(e => e.Brand)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Optimistic concurrency row version
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(c => c.CardHolder)
                  .WithMany(ch => ch.Cards)
                  .HasForeignKey(c => c.CardHolderCusNo)
                  .HasPrincipalKey(ch => ch.CustomerNo)
                  .OnDelete(DeleteBehavior.Restrict);

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

            entity.Property(e => e.Category)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Foreign Key: CardTransaction -> Card
            entity.HasOne(t => t.Card)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(t => t.CardId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 4. Budget Mapping
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("Budget");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.Month).IsRequired();
            entity.Property(e => e.LimitAmount).HasColumnType("decimal(18,2)").IsRequired();
            
            entity.Property(e => e.SpentAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired()
                  .HasDefaultValue(0m); 

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.Property(e => e.WarningThreshold80).HasDefaultValue(false);
            entity.Property(e => e.WarningThreshold100).HasDefaultValue(false);

            // Unique constraint: one budget per card per month/year
            entity.HasIndex(b => new { b.CardId, b.Year, b.Month }).IsUnique();

            entity.HasOne(b => b.Card)
                  .WithMany() // Left empty so you don't need a list in your Card class
                  .HasForeignKey(b => b.CardId)
                  .OnDelete(DeleteBehavior.Restrict); 

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 5. Transfer Mapping
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("Transfer");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.TransferDate).IsRequired();

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValueSql("'Pending'");

            // SQL Defaults for Base Entity properties
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(t => t.FromCard)
                  .WithMany()
                  .HasForeignKey(t => t.FromCardId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToCard)
                  .WithMany()
                  .HasForeignKey(t => t.ToCardId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 6. ProcessLog Mapping 
        modelBuilder.Entity<ProcessLog>(entity =>
        {
            entity.ToTable("ProcessLog");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Timestamp)
                  .IsRequired()
                  .HasDefaultValueSql("SYSDATETIME()");

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