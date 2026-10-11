using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed class RelocationPersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public RelocationPersistenceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Saved_relocation_stores_normalized_codes_string_enums_and_a_calendar_date()
    {
        Guid relocationId;

        await using (var db = _postgres.CreateContext())
        {
            var relocation = new Relocation
            {
                User = new User
                {
                    Email = "mover@example.com",
                    DisplayName = "Mover",
                },
                Name = "  Summer move ",
                OriginCountryCode = " us ",
                OriginCity = "  Austin ",
                DestinationCountryCode = "de",
                DestinationCity = " Berlin ",
                TargetDate = new DateOnly(2026, 8, 1),
                Purpose = RelocationPurpose.Work,
                Status = RelocationStatus.Active,
            };
            db.Relocations.Add(relocation);
            await db.SaveChangesAsync();
            relocationId = relocation.Id;
        }

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.Relocations.SingleAsync(relocation => relocation.Id == relocationId);
            var purpose = await db.Database
                .SqlQueryRaw<string>("""SELECT purpose AS "Value" FROM relocations WHERE id = {0}""", relocationId)
                .SingleAsync();
            var status = await db.Database
                .SqlQueryRaw<string>("""SELECT status AS "Value" FROM relocations WHERE id = {0}""", relocationId)
                .SingleAsync();
            var targetDateType = await db.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT data_type AS "Value"
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'relocations'
                      AND column_name = 'target_date'
                    """)
                .SingleAsync();
            var internationalColumn = await db.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT column_name AS "Value"
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'relocations'
                      AND column_name = 'is_international'
                    """)
                .ToListAsync();

            Assert.Equal(7, stored.Id.Version);
            Assert.Equal("Summer move", stored.Name);
            Assert.Equal("US", stored.OriginCountryCode);
            Assert.Equal("Austin", stored.OriginCity);
            Assert.Equal("DE", stored.DestinationCountryCode);
            Assert.Equal("Berlin", stored.DestinationCity);
            Assert.Equal(new DateOnly(2026, 8, 1), stored.TargetDate);
            Assert.Equal("date", targetDateType);
            Assert.Equal(RelocationPurpose.Work, stored.Purpose);
            Assert.Equal("Work", purpose);
            Assert.Equal(RelocationStatus.Active, stored.Status);
            Assert.Equal("Active", status);
            Assert.True(stored.IsInternational);
            Assert.Empty(internationalColumn);
            Assert.Equal(TimeSpan.Zero, stored.CreatedAt.Offset);
            Assert.Equal(stored.CreatedAt, stored.UpdatedAt);
        }
    }

    [Fact]
    public async Task Deleting_a_user_is_restricted_while_a_relocation_exists()
    {
        Guid userId;

        await using (var db = _postgres.CreateContext())
        {
            var relocation = new Relocation
            {
                User = new User
                {
                    Email = "owner@example.com",
                    DisplayName = "Owner",
                },
                Name = "Home",
                OriginCountryCode = "US",
                OriginCity = "Austin",
                DestinationCountryCode = "US",
                DestinationCity = "Seattle",
                TargetDate = new DateOnly(2026, 9, 1),
                Purpose = RelocationPurpose.Family,
            };
            db.Relocations.Add(relocation);
            await db.SaveChangesAsync();
            userId = relocation.UserId;

            Assert.False(relocation.IsInternational);
            Assert.Equal(RelocationStatus.Draft, relocation.Status);
        }

        await using (var db = _postgres.CreateContext())
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM users WHERE id = {userId}"));

            Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
            Assert.Equal("relocations", exception.TableName);
            Assert.Equal("fk_relocations_users_user_id", exception.ConstraintName);
        }
    }

    [Fact]
    public async Task Country_code_must_be_two_letters()
    {
        await using var db = _postgres.CreateContext();
        db.Relocations.Add(new Relocation
        {
            User = new User
            {
                Email = "bad-code@example.com",
                DisplayName = "Bad Code",
            },
            Name = "Bad",
            OriginCountryCode = "U",
            OriginCity = "Austin",
            DestinationCountryCode = "US",
            DestinationCity = "Seattle",
            TargetDate = new DateOnly(2026, 9, 1),
            Purpose = RelocationPurpose.Other,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("ck_relocations_origin_country_code", postgres.ConstraintName);
    }
}
