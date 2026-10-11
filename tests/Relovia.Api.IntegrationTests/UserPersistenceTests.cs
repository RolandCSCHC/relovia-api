using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.IntegrationTests;

public sealed class UserPersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public UserPersistenceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Saved_user_round_trips_with_normalized_email_and_timestamps()
    {
        Guid userId;
        DateTimeOffset createdAt;

        await using (var db = _postgres.CreateContext())
        {
            var user = new User
            {
                Email = "  Person@Example.com ",
                DisplayName = "Person Example",
                AvatarUrl = "https://example.com/avatar.png",
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
            createdAt = user.CreatedAt;
        }

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.Users.SingleAsync(user => user.Id == userId);
            var storedEmail = await db.Database
                .SqlQueryRaw<string>("""SELECT email AS "Value" FROM users WHERE id = {0}""", userId)
                .SingleAsync();

            Assert.Equal(7, stored.Id.Version);
            Assert.Equal("person@example.com", stored.Email);
            Assert.Equal(stored.Email, storedEmail);
            Assert.Equal("Person Example", stored.DisplayName);
            Assert.Equal("https://example.com/avatar.png", stored.AvatarUrl);
            Assert.Equal("USD", stored.PreferredCurrency);
            Assert.Equal("en", stored.PreferredLanguage);
            Assert.Equal(TimeSpan.Zero, stored.CreatedAt.Offset);
            Assert.Equal(createdAt, stored.CreatedAt);
            Assert.Equal(stored.CreatedAt, stored.UpdatedAt);

            // Timestamps are stored as microseconds. Wait until the clock enters the next one.
            var nextMicrosecond = createdAt.AddTicks(TimeSpan.TicksPerMicrosecond);
            while (DateTimeOffset.UtcNow < nextMicrosecond)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));
            }

            stored.DisplayName = "Updated Name";
            await db.SaveChangesAsync();
        }

        await using (var db = _postgres.CreateContext())
        {
            var updated = await db.Users.SingleAsync(user => user.Id == userId);

            Assert.Equal("Updated Name", updated.DisplayName);
            Assert.Equal(createdAt, updated.CreatedAt);
            Assert.True(updated.UpdatedAt > createdAt);
        }
    }

    [Fact]
    public async Task Normalized_email_cannot_be_stored_twice()
    {
        await using var db = _postgres.CreateContext();
        db.Users.Add(new User
        {
            Email = "Second@Example.com",
            DisplayName = "First",
        });
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        db.Users.Add(new User
        {
            Email = "  second@example.com ",
            DisplayName = "Second",
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("users", postgres.TableName);
        Assert.Equal("ux_users_email", postgres.ConstraintName);
    }
}
