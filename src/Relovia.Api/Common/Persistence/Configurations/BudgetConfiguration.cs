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

        builder.Property(budget => budget.MonthlyNetIncome)
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(budget => budget.MonthlyNetIncomeCurrency)
            .HasMaxLength(BudgetRules.CurrencyCodeLength)
            .IsRequired(false)
            .HasConversion(
                code => string.IsNullOrWhiteSpace(code) ? null : BudgetRules.NormalizeCurrencyCode(code),
                code => code);

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
            table.HasCheckConstraint(
                "ck_budgets_monthly_net_income",
                """
                (
                    monthly_net_income IS NULL
                    AND monthly_net_income_currency IS NULL
                ) OR (
                    monthly_net_income > 0
                    AND monthly_net_income_currency ~ '^[A-Z]{3}$'
                )
                """);
        });
    }
}
