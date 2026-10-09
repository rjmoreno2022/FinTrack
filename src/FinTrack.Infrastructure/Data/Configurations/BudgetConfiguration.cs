using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.LimitAmount)
            .HasPrecision(18, 4);

        builder.Property(b => b.SpentAmount)
            .HasPrecision(18, 4);

        builder.Property(b => b.Currency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(b => b.Period)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.HasIndex(b => b.UserId);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => new { b.StartDate, b.EndDate, b.IsActive });

        builder.Navigation(b => b.Category).AutoInclude();
    }
}
