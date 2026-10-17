using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(expense => expense.Id);

        builder.Property(expense => expense.Description)
            .HasMaxLength(BudgetRules.DescriptionMaxLength)
            .IsRequired()
            .HasConversion(description => description.Trim(), description => description);

        // The amount and currency are the original payment. Nothing recomputes them.
        builder.Property(expense => expense.Amount)
            .HasColumnType("numeric(18,4)");

        builder.Property(expense => expense.CurrencyCode)
            .HasMaxLength(BudgetRules.CurrencyCodeLength)
            .IsRequired()
            .HasConversion(code => BudgetRules.NormalizeCurrencyCode(code), code => code);

        builder.Property(expense => expense.PaidOn)
            .HasColumnType("date");

        builder.Property(expense => expense.Notes)
            .HasMaxLength(BudgetRules.NotesMaxLength)
            .IsRequired(false);

        builder.Property(expense => expense.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(expense => expense.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(expense => new { expense.RelocationId, expense.PaidOn })
            .HasDatabaseName("ix_expenses_relocation_id_paid_on");

        builder.HasIndex(expense => new { expense.RelocationId, expense.CategoryId })
            .HasDatabaseName("ix_expenses_relocation_id_category_id");

        builder.HasIndex(expense => expense.TaskId)
            .HasDatabaseName("ix_expenses_task_id");

        // Deleting a relocation removes its expenses directly. The category link only blocks
        // deleting a category on its own; the service reports that as a conflict first.
        builder.HasOne(expense => expense.Relocation)
            .WithMany()
            .HasForeignKey(expense => expense.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(expense => expense.Category)
            .WithMany(category => category.Expenses)
            .HasForeignKey(expense => new { expense.RelocationId, expense.CategoryId })
            .HasPrincipalKey(category => new { category.RelocationId, category.Id })
            .OnDelete(DeleteBehavior.NoAction);

        // A plain key because a composite one cannot be nulled without nulling relocation_id.
        // The service checks that the task belongs to the same relocation.
        builder.HasOne(expense => expense.Task)
            .WithMany()
            .HasForeignKey(expense => expense.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("expenses", table =>
        {
            table.HasCheckConstraint("ck_expenses_amount", "amount > 0");
            table.HasCheckConstraint("ck_expenses_currency_code", "currency_code ~ '^[A-Z]{3}$'");
        });
    }
}
