using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 4);

        builder.Property(t => t.Currency)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Tags)
            .HasMaxLength(255);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(t => t.Category).AutoInclude();

        builder.HasIndex(t => new { t.AccountId, t.Date });
        builder.HasIndex(t => new { t.CategoryId, t.Date });
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.Type);
    }
}
