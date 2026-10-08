using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("Debts");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Creditor)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.OriginalAmount)
            .HasPrecision(18, 4);

        builder.Property(d => d.RemainingAmount)
            .HasPrecision(18, 4);

        builder.Property(d => d.Currency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.InterestRate)
            .HasPrecision(5, 2);

        builder.HasIndex(d => new { d.Status, d.DueDate });

        builder.HasMany(d => d.Payments)
            .WithOne(p => p.Debt)
            .HasForeignKey(p => p.DebtId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Payments).AutoInclude();
    }
}
