using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class InventoryListTemplateConfiguration : IEntityTypeConfiguration<InventoryListTemplate>
{
    public void Configure(EntityTypeBuilder<InventoryListTemplate> builder)
    {
        builder.HasKey(template => template.Id);

        builder.Property(template => template.Code)
            .HasMaxLength(InventoryTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => InventoryTemplateRules.NormalizeCode(code), code => code);

        builder.Property(template => template.Title)
            .HasMaxLength(InventoryRules.NameMaxLength)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(template => template.Room)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(template => template.Scope)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(template => template.PublishedAt)
            .HasColumnType("timestamptz");

        builder.Property(template => template.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(template => template.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Ignore(template => template.IsPublished);

        builder.HasIndex(template => new { template.Code, template.Version })
            .IsUnique()
            .HasDatabaseName("ux_inventory_list_templates_code_version");

        builder.HasData(StarterInventoryLists.Templates.Cast<object>().ToArray());

        builder.ToTable("inventory_list_templates", table =>
        {
            table.HasCheckConstraint("ck_inventory_list_templates_version", "version >= 1");
            table.HasCheckConstraint(
                "ck_inventory_list_templates_scope",
                "scope IN ('Furnished', 'Unfurnished', 'Both')");
            table.HasCheckConstraint(
                "ck_inventory_list_templates_room",
                "room IN ('Kitchen', 'Bathroom', 'Bedroom', 'LivingRoom', 'DiningRoom', 'Office', 'Other')");
            table.HasCheckConstraint(
                "ck_inventory_list_templates_code",
                "code ~ '^[A-Za-z0-9_-]+$'");
            table.HasCheckConstraint(
                "ck_inventory_list_templates_title",
                "char_length(btrim(title)) > 0");
        });
    }
}
