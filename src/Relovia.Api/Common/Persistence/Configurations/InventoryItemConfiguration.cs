using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .HasMaxLength(InventoryRules.NameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(item => item.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(item => item.Room)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(item => item.Acquisition)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.MovingAction)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(item => item.PackingStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(item => item.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.EstimatedPrice)
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(item => item.ActualPrice)
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(item => item.CurrencyCode)
            .HasMaxLength(BudgetRules.CurrencyCodeLength)
            .IsRequired(false)
            .HasConversion(
                code => code == null ? null : BudgetRules.NormalizeCurrencyCode(code),
                code => code);

        builder.Property(item => item.Notes)
            .HasMaxLength(InventoryRules.NotesMaxLength)
            .IsRequired(false);

        builder.Property(item => item.Code)
            .HasMaxLength(InventoryTemplateRules.CodeMaxLength)
            .IsRequired(false)
            .HasConversion(
                code => code == null ? null : InventoryTemplateRules.NormalizeCode(code),
                code => code);

        builder.HasIndex(item => item.BudgetCategoryId)
            .HasDatabaseName("ix_inventory_items_budget_category_id");

        builder.Property(item => item.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(item => item.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(item => new { item.RelocationId, item.Room, item.Name })
            .HasDatabaseName("ix_inventory_items_relocation_id_room_name");

        builder.HasIndex(item => new { item.RelocationId, item.Code })
            .IsUnique()
            .HasFilter("code IS NOT NULL")
            .HasDatabaseName("ux_inventory_items_relocation_id_code");

        builder.HasOne(item => item.Relocation)
            .WithMany()
            .HasForeignKey(item => item.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // A plain key, checked at the end of the statement, so deleting the relocation can remove
        // the items and the categories together. The service checks the category is this relocation's.
        builder.HasOne(item => item.BudgetCategory)
            .WithMany()
            .HasForeignKey(item => item.BudgetCategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(item => item.SourceTemplateItem)
            .WithMany()
            .HasForeignKey(item => item.SourceTemplateItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("ck_inventory_items_name", "char_length(btrim(name)) > 0");
            table.HasCheckConstraint(
                "ck_inventory_items_category",
                "category IS NULL OR category IN ('Furniture', 'Appliances', 'Linens', 'Cookware', 'Electronics', 'Decor', 'Other')");
            table.HasCheckConstraint(
                "ck_inventory_items_room",
                "room IS NULL OR room IN ('Kitchen', 'Bathroom', 'Bedroom', 'LivingRoom', 'DiningRoom', 'Office', 'Other')");
            table.HasCheckConstraint(
                "ck_inventory_items_quantity",
                $"quantity >= {InventoryRules.MinQuantity} AND quantity <= {InventoryRules.MaxQuantity}");
            table.HasCheckConstraint(
                "ck_inventory_items_acquisition",
                "acquisition IN ('Owned', 'ToPurchase')");
            table.HasCheckConstraint(
                "ck_inventory_items_moving_action",
                "moving_action IS NULL OR moving_action IN ('Bring', 'Sell', 'Donate', 'Store', 'Discard')");
            table.HasCheckConstraint(
                "ck_inventory_items_packing_status",
                "packing_status IS NULL OR packing_status IN ('NotPacked', 'Packed', 'Unpacked')");
            table.HasCheckConstraint(
                "ck_inventory_items_packing_matches_action",
                "(moving_action = 'Bring') = (packing_status IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_inventory_items_priority",
                "priority IN ('Low', 'Normal', 'High')");
            table.HasCheckConstraint(
                "ck_inventory_items_estimated_price",
                "estimated_price IS NULL OR estimated_price > 0");
            table.HasCheckConstraint(
                "ck_inventory_items_actual_price",
                "actual_price IS NULL OR actual_price > 0");
            table.HasCheckConstraint(
                "ck_inventory_items_currency",
                """
                (
                    estimated_price IS NULL
                    AND actual_price IS NULL
                    AND currency_code IS NULL
                ) OR (
                    currency_code ~ '^[A-Z]{3}$'
                    AND (estimated_price IS NOT NULL OR actual_price IS NOT NULL)
                )
                """);
            table.HasCheckConstraint(
                "ck_inventory_items_budget_category",
                "budget_category_id IS NULL OR acquisition = 'ToPurchase'");
            table.HasCheckConstraint(
                "ck_inventory_items_priced_shopping",
                "acquisition <> 'ToPurchase' OR (estimated_price IS NULL AND actual_price IS NULL) OR budget_category_id IS NOT NULL");
            table.HasCheckConstraint(
                "ck_inventory_items_notes",
                "notes IS NULL OR char_length(btrim(notes)) > 0");
            table.HasCheckConstraint(
                "ck_inventory_items_code",
                "code IS NULL OR code ~ '^[A-Za-z0-9_-]+$'");
        });
    }
}
