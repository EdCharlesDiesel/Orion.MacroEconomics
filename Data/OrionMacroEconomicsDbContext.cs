using Microsoft.EntityFrameworkCore;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Data;

public class OrionMacroEconomicsDbContext(DbContextOptions<OrionMacroEconomicsDbContext> options) : DbContext(options)
{
      public DbSet<TradeSetup> TradeSetups => Set<TradeSetup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrionMacroEconomicsDbContext).Assembly);

        // Or configure inline if you prefer
        modelBuilder.Entity<TradeSetup>(entity =>
        {
            // Table configuration
            entity.ToTable("trade_setups", schema: "trading"); // Add schema if needed

            // Primary key
            entity.HasKey(e => e.Id);

            // Use identity column (auto-increment)
            entity.Property(e => e.Id)
                  .UseIdentityColumn();

            // Timestamp with server default
            entity.Property(e => e.LoggedAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("NOW()")
                  .IsRequired();

            // Boolean with default
            entity.Property(e => e.IsOpen)
                  .HasDefaultValue(true)
                  .IsRequired();

            // JSON column for flexible data
            entity.Property(e => e.ChecksDetail)
                  .HasColumnType("jsonb")
                  .IsRequired(false);

            // Decimal precision for financial calculations
            entity.Property(e => e.Atr14)
                  .HasPrecision(10, 5);

            entity.Property(e => e.Atr20)
                  .HasPrecision(10, 5);

            entity.Property(e => e.SlPips)
                  .HasPrecision(8, 1);

            entity.Property(e => e.Tp1Pips)
                  .HasPrecision(8, 1);

            entity.Property(e => e.Tp2Pips)
                  .HasPrecision(8, 1);

            entity.Property(e => e.LotSize)
                  .HasPrecision(10, 2);

            entity.Property(e => e.RiskAmount)
                  .HasPrecision(12, 2);

            entity.Property(e => e.AccountBalance)
                  .HasPrecision(14, 2);

            entity.Property(e => e.RiskPct)
                  .HasPrecision(5, 2);

            entity.Property(e => e.EntryPrice)
                  .HasPrecision(12, 5);

            entity.Property(e => e.ClosePrice)
                  .HasPrecision(12, 5);

            entity.Property(e => e.PipsGained)
                  .HasPrecision(10, 1);

            entity.Property(e => e.RMultiple)
                  .HasPrecision(8, 2);

            // String length constraints
            entity.Property(e => e.Instrument)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Ticker)
                  .HasMaxLength(20);

            entity.Property(e => e.Direction)
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.Session)
                  .HasMaxLength(20);

            entity.Property(e => e.Score)
                  .HasMaxLength(20);

            entity.Property(e => e.Verdict)
                  .HasMaxLength(20);

            entity.Property(e => e.Outcome)
                  .HasMaxLength(10);

            entity.Property(e => e.Notes)
                  .HasMaxLength(2000);

            // Indexes for common queries
            entity.HasIndex(e => e.LoggedAt)
                  .HasDatabaseName("IX_trade_setups_logged_at");

            entity.HasIndex(e => e.Outcome)
                  .HasDatabaseName("IX_trade_setups_outcome");

            entity.HasIndex(e => e.IsOpen)
                  .HasDatabaseName("IX_trade_setups_is_open");

            entity.HasIndex(e => e.Direction)
                  .HasDatabaseName("IX_trade_setups_direction");

            entity.HasIndex(e => e.Instrument)
                  .HasDatabaseName("IX_trade_setups_instrument");

            // Composite index for common query pattern
            entity.HasIndex(e => new { e.IsOpen, e.Outcome, e.LoggedAt })
                  .HasDatabaseName("IX_trade_setups_open_outcome_date");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit trail - auto-set timestamps
        foreach (var entry in ChangeTracker.Entries<TradeSetup>())
        {
            if (entry.State == EntityState.Modified && entry.Property(e => e.IsOpen).IsModified)
            {
                // If trade is being closed, you might want to track when
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}