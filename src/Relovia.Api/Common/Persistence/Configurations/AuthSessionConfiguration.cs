using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.Common.Persistence.Configurations;

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenHash)
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnType("timestamptz");

        builder.Property(session => session.AbsoluteExpiresAt)
            .HasColumnType("timestamptz");

        builder.Property(session => session.RevokedAt)
            .HasColumnType("timestamptz");

        builder.Property(session => session.CreatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(session => session.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_auth_sessions_token_hash");

        builder.HasOne(session => session.User)
            .WithMany(user => user.AuthSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
