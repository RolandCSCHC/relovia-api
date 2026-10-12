using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.HasKey(template => template.Id);

        builder.Property(template => template.Code)
            .HasMaxLength(ChecklistTemplateRules.CodeMaxLength)
            .IsRequired()
            .HasConversion(code => ChecklistTemplateRules.NormalizeCode(code), code => code);

        builder.Property(template => template.Title)
            .HasMaxLength(ChecklistTemplateRules.TitleMaxLength)
            .IsRequired()
            .HasConversion(title => title.Trim(), title => title);

        builder.Property(template => template.Scope)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(template => template.DestinationCountryCode)
            .HasMaxLength(RelocationRules.CountryCodeLength)
            .HasConversion(
                code => string.IsNullOrWhiteSpace(code) ? null : CountryCodes.Normalize(code),
                code => code);

        builder.Property(template => template.Purpose)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(template => template.PublishedAt)
            .HasColumnType("timestamptz");

        builder.Property(template => template.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(template => template.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Ignore(template => template.IsPublished);

        builder.HasIndex(template => new { template.Code, template.Version })
            .IsUnique()
            .HasDatabaseName("ux_checklist_templates_code_version");

        builder.HasData(StarterChecklistTemplates.Templates.Cast<object>().ToArray());

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_checklist_templates_version", "version >= 1");
            table.HasCheckConstraint(
                "ck_checklist_templates_scope",
                "scope IN ('Domestic', 'International', 'Both')");
            table.HasCheckConstraint(
                "ck_checklist_templates_destination_country_code",
                "destination_country_code IS NULL OR destination_country_code ~ '^[A-Z]{2}$'");
            table.HasCheckConstraint(
                "ck_checklist_templates_purpose",
                "purpose IS NULL OR purpose IN ('Work', 'Study', 'Family', 'Lifestyle', 'Other')");
        });
    }
}
