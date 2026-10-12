using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class RelocationConfiguration : IEntityTypeConfiguration<Relocation>
{
    public void Configure(EntityTypeBuilder<Relocation> builder)
    {
        builder.HasKey(relocation => relocation.Id);

        builder.Property(relocation => relocation.Name)
            .HasMaxLength(RelocationRules.NameMaxLength)
            .IsRequired()
            .HasConversion(name => name.Trim(), name => name);

        builder.Property(relocation => relocation.OriginCountryCode)
            .HasMaxLength(RelocationRules.CountryCodeLength)
            .IsRequired()
            .HasConversion(code => CountryCodes.Normalize(code), code => code);

        builder.Property(relocation => relocation.DestinationCountryCode)
            .HasMaxLength(RelocationRules.CountryCodeLength)
            .IsRequired()
            .HasConversion(code => CountryCodes.Normalize(code), code => code);

        builder.Property(relocation => relocation.OriginCity)
            .HasMaxLength(RelocationRules.CityMaxLength)
            .IsRequired()
            .HasConversion(city => city.Trim(), city => city);

        builder.Property(relocation => relocation.DestinationCity)
            .HasMaxLength(RelocationRules.CityMaxLength)
            .IsRequired()
            .HasConversion(city => city.Trim(), city => city);

        builder.Property(relocation => relocation.TargetDate)
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(relocation => relocation.Purpose)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(relocation => relocation.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(RelocationStatus.Draft);

        builder.Property(relocation => relocation.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(relocation => relocation.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Ignore(relocation => relocation.IsInternational);

        builder.HasOne(relocation => relocation.User)
            .WithMany(user => user.Relocations)
            .HasForeignKey(relocation => relocation.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_relocations_origin_country_code",
                "origin_country_code ~ '^[A-Z]{2}$'");
            table.HasCheckConstraint(
                "ck_relocations_destination_country_code",
                "destination_country_code ~ '^[A-Z]{2}$'");
        });
    }
}
