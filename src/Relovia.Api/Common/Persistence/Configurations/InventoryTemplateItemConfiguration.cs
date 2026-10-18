using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class InventoryTemplateItemConfiguration : IEntityTypeConfiguration<InventoryTemplateItem>
{
    public void Configure(EntityTypeBuilder<InventoryTemplateItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Code)
            .HasMaxLength(InventoryTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => InventoryTemplateRules.NormalizeCode(code), code => code);

        builder.Property(item => item.Name)
            .HasMaxLength(InventoryRules.NameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(item => item.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(item => item.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(item => new { item.InventoryListTemplateId, item.Code })
            .IsUnique()
            .HasDatabaseName("ux_inventory_template_items_template_id_code");

        builder.HasData(StarterInventoryLists.Items.Cast<object>().ToArray());

        builder.HasOne(item => item.InventoryListTemplate)
            .WithMany(template => template.Items)
            .HasForeignKey(item => item.InventoryListTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("inventory_template_items", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_template_items_code",
                "code ~ '^[A-Za-z0-9_-]+$'");
            table.HasCheckConstraint(
                "ck_inventory_template_items_name",
                "char_length(btrim(name)) > 0");
            table.HasCheckConstraint(
                "ck_inventory_template_items_category",
                "category IN ('Furniture', 'Appliances', 'Linens', 'Cookware', 'Electronics', 'Decor', 'Other')");
            table.HasCheckConstraint(
                "ck_inventory_template_items_quantity",
                $"quantity >= {InventoryRules.MinQuantity} AND quantity <= {InventoryRules.MaxQuantity}");
            table.HasCheckConstraint(
                "ck_inventory_template_items_priority",
                "priority IN ('Low', 'Normal', 'High')");
        });
    }
}
