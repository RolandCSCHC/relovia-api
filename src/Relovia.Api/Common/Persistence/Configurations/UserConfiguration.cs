using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired()
            .HasConversion(email => email.Trim().ToLowerInvariant(), email => email);

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        builder.Property(user => user.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(user => user.PreferredCurrency)
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("USD");

        builder.Property(user => user.PreferredLanguage)
            .HasMaxLength(35)
            .IsRequired()
            .HasDefaultValue("en");

        builder.Property(user => user.InterestNotifiedAt)
            .HasColumnType("timestamptz");

        builder.Property(user => user.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(user => user.UpdatedAt)
            .HasColumnType("timestamptz");
    }
}
