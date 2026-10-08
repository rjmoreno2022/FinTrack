using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.FromCurrency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(r => r.ToCurrency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(r => r.Rate)
            .HasPrecision(18, 6);

        builder.Property(r => r.Source)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(r => new { r.FromCurrency, r.ToCurrency, r.Date });
    }
}
