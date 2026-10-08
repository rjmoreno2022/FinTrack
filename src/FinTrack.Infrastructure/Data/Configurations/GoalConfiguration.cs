using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        builder.Property(g => g.TargetAmount)
            .HasPrecision(18, 4);

        builder.Property(g => g.CurrentAmount)
            .HasPrecision(18, 4);

        builder.Property(g => g.Currency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(g => g.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(g => g.Status);
    }
}
