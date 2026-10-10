using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.HasKey(login => login.Id);

        builder.Property(login => login.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(login => login.ProviderSubject)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(login => login.CreatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(login => new { login.Provider, login.ProviderSubject })
            .IsUnique()
            .HasDatabaseName("ux_external_logins_provider_subject");

        builder.HasIndex(login => new { login.UserId, login.Provider })
            .IsUnique()
            .HasDatabaseName("ux_external_logins_user_provider");

        builder.HasOne(login => login.User)
            .WithMany(user => user.ExternalLogins)
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
