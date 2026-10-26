using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Housing;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class HousingOptionConfiguration : IEntityTypeConfiguration<HousingOption>
{
    public void Configure(EntityTypeBuilder<HousingOption> builder)
    {
        builder.HasKey(option => option.Id);

        builder.Property(option => option.Name)
            .HasMaxLength(HousingRules.NameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(option => option.Area)
            .HasMaxLength(HousingRules.AreaMaxLength)
            .IsRequired(false)
            .HasConversion(
                area => string.IsNullOrWhiteSpace(area) ? null : area.Trim(),
                area => area);

        builder.Property(option => option.Rent)
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(option => option.Deposit)
            .HasColumnType("numeric(18,4)")
            .IsRequired(false);

        builder.Property(option => option.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired(false)
            .HasConversion(
                code => HousingRules.NormalizeCurrency(code),
                code => code);

        builder.Property(option => option.Size)
            .HasMaxLength(HousingRules.SizeMaxLength)
            .IsRequired(false)
            .HasConversion(
                size => string.IsNullOrWhiteSpace(size) ? null : size.Trim(),
                size => size);

        builder.Property(option => option.Furnishing)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(option => option.AvailableOn)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(option => option.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(option => option.Notes)
            .HasMaxLength(HousingRules.NotesMaxLength)
            .IsRequired(false)
            .HasConversion(
                notes => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                notes => notes);

        builder.Property(option => option.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(option => option.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(option => option.RelocationId)
            .HasDatabaseName("ix_housing_options_relocation_id");

        builder.HasIndex(option => option.RelocationId)
            .IsUnique()
            .HasFilter("status = 'Chosen'")
            .HasDatabaseName("ux_housing_options_one_chosen");

        builder.HasOne(option => option.Relocation)
            .WithMany()
            .HasForeignKey(option => option.RelocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("housing_options", table =>
        {
            table.HasCheckConstraint(
                "ck_housing_options_name",
                $"char_length(btrim(name)) > 0 AND char_length(name) <= {HousingRules.NameMaxLength}");
            table.HasCheckConstraint(
                "ck_housing_options_area",
                $"area IS NULL OR (char_length(btrim(area)) > 0 AND char_length(area) <= {HousingRules.AreaMaxLength})");
            table.HasCheckConstraint(
                "ck_housing_options_size",
                $"size IS NULL OR (char_length(btrim(size)) > 0 AND char_length(size) <= {HousingRules.SizeMaxLength})");
            table.HasCheckConstraint(
                "ck_housing_options_notes",
                $"notes IS NULL OR (char_length(btrim(notes)) > 0 AND char_length(notes) <= {HousingRules.NotesMaxLength})");
            table.HasCheckConstraint(
                "ck_housing_options_status",
                "status IN ('Considering', 'Viewing', 'Offered', 'Chosen', 'Dropped')");
            table.HasCheckConstraint(
                "ck_housing_options_furnishing",
                "furnishing IS NULL OR furnishing IN ('Furnished', 'Unfurnished')");
            table.HasCheckConstraint(
                "ck_housing_options_rent",
                "rent IS NULL OR rent > 0");
            table.HasCheckConstraint(
                "ck_housing_options_deposit",
                "deposit IS NULL OR deposit > 0");
            table.HasCheckConstraint(
                "ck_housing_options_currency",
                """
                (
                    rent IS NULL
                    AND deposit IS NULL
                    AND currency_code IS NULL
                ) OR (
                    currency_code ~ '^[A-Z]{3}$'
                    AND (rent IS NOT NULL OR deposit IS NOT NULL)
                )
                """);
        });
    }
}
