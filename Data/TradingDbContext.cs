using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Data;

public sealed class TradingDbContext(DbContextOptions<TradingDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketDataSnapshot>(entity =>
        {
            entity.ToTable("market_data_snapshots");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DataType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(50).IsRequired();

            entity.Property(x => x.Payload)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.Provider,
                x.DataType,
                x.Symbol,
                x.FromUtc,
                x.ToUtc
            });
        });
    }
}