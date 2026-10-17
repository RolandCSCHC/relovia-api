using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(budget => budget.RelocationId)
            .HasName("pk_budgets");

        builder.Property(budget => budget.CurrencyCode)
            .HasMaxLength(BudgetRules.CurrencyCodeLength)
            .IsRequired()
            .HasConversion(code => BudgetRules.NormalizeCurrencyCode(code), code => code);

        builder.Property(budget => budget.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(budget => budget.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasOne(budget => budget.Relocation)
            .WithOne()
            .HasForeignKey<Budget>(budget => budget.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("budgets", table =>
        {
            table.HasCheckConstraint("ck_budgets_currency_code", "currency_code ~ '^[A-Z]{3}$'");
        });
    }
}
