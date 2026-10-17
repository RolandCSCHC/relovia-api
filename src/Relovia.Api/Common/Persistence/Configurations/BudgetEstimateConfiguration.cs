using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class BudgetEstimateConfiguration : IEntityTypeConfiguration<BudgetEstimate>
{
    public void Configure(EntityTypeBuilder<BudgetEstimate> builder)
    {
        builder.HasKey(estimate => estimate.Id);

        builder.Property(estimate => estimate.Label)
            .HasMaxLength(BudgetRules.LabelMaxLength)
            .IsRequired()
            .HasConversion(label => label.Trim(), label => label);

        builder.Property(estimate => estimate.Amount)
            .HasColumnType("numeric(18,4)");

        builder.Property(estimate => estimate.CurrencyCode)
            .HasMaxLength(BudgetRules.CurrencyCodeLength)
            .IsRequired()
            .HasConversion(code => BudgetRules.NormalizeCurrencyCode(code), code => code);

        builder.Property(estimate => estimate.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(estimate => estimate.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(estimate => new { estimate.RelocationId, estimate.CategoryId })
            .HasDatabaseName("ix_budget_estimates_relocation_id_category_id");

        builder.HasOne(estimate => estimate.Category)
            .WithMany(category => category.Estimates)
            .HasForeignKey(estimate => new { estimate.RelocationId, estimate.CategoryId })
            .HasPrincipalKey(category => new { category.RelocationId, category.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("budget_estimates", table =>
        {
            table.HasCheckConstraint("ck_budget_estimates_amount", "amount > 0");
            table.HasCheckConstraint("ck_budget_estimates_currency_code", "currency_code ~ '^[A-Z]{3}$'");
        });
    }
}
