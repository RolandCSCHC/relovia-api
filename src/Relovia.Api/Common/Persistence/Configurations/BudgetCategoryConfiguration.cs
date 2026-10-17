using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        builder.HasKey(category => category.Id);

        // Estimates and expenses reference (relocation_id, id) so they cannot point at another relocation's category.
        builder.HasAlternateKey(category => new { category.RelocationId, category.Id })
            .HasName("ak_budget_categories_relocation_id_id");

        builder.Property(category => category.Name)
            .HasMaxLength(BudgetRules.CategoryNameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(category => category.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(category => category.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(category => new { category.RelocationId, category.Name })
            .IsUnique()
            .HasDatabaseName("ux_budget_categories_relocation_id_name");

        builder.HasOne(category => category.Budget)
            .WithMany(budget => budget.Categories)
            .HasForeignKey(category => category.RelocationId)
            .HasPrincipalKey(budget => budget.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("budget_categories");
    }
}
