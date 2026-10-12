using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed class StarterChecklistTemplatePersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public StarterChecklistTemplatePersistenceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Published_starter_templates_are_available_for_selection()
    {
        await using var db = _postgres.CreateContext();
        var templates = await db.ChecklistTemplates
            .Include(template => template.Tasks)
            .ThenInclude(task => task.Dependencies)
            .Where(template =>
                template.Code == StarterChecklistTemplates.DomesticCode
                || template.Code == StarterChecklistTemplates.InternationalCode)
            .OrderBy(template => template.Code)
            .ToListAsync();

        Assert.Equal(
            [StarterChecklistTemplates.DomesticCode, StarterChecklistTemplates.InternationalCode],
            templates.Select(template => template.Code));
        Assert.All(templates, template =>
        {
            Assert.Equal(1, template.Version);
            Assert.NotNull(template.PublishedAt);
            Assert.Null(template.DestinationCountryCode);
            Assert.Null(template.Purpose);
        });

        var domestic = templates.Single(template => template.Scope == ChecklistScope.Domestic);
        var international = templates.Single(template => template.Scope == ChecklistScope.International);
        Assert.Contains(domestic.Tasks, task => task.Code == "give-notice");
        Assert.Contains(international.Tasks, task => task.Code == "give-notice");
        Assert.Equal(
            "check-passport",
            international.Tasks.Single(task => task.Code == "book-travel").Dependencies.Single().DependsOnCode);
        Assert.Equal(
            "book-movers",
            domestic.Tasks.Single(task => task.Code == "unpack-essentials").Dependencies.Single().DependsOnCode);
    }

    [Fact]
    public async Task Selection_uses_the_published_starter_for_the_move_type()
    {
        await using var db = _postgres.CreateContext();
        var templates = await db.ChecklistTemplates
            .AsNoTracking()
            .Include(template => template.Tasks)
            .ThenInclude(task => task.Dependencies)
            .ToListAsync();

        var domestic = ChecklistSelector.Select(Move("US", "US"), templates);
        Assert.Equal([StarterChecklistTemplates.DomesticCode], domestic.Templates.Select(template => template.Code));
        Assert.Equal(
            ["give-notice", "book-movers", "transfer-utilities", "change-address", "unpack-essentials"],
            domestic.Tasks.Select(task => task.Code));
        Assert.Equal(["give-notice"], domestic.Tasks.Single(task => task.Code == "book-movers").DependsOnCodes);

        var international = ChecklistSelector.Select(Move("US", "DE"), templates);
        Assert.Equal(
            [StarterChecklistTemplates.InternationalCode],
            international.Templates.Select(template => template.Code));
        Assert.Equal(
            ["check-passport", "give-notice", "arrange-housing", "notify-bank", "book-travel", "register-arrival"],
            international.Tasks.Select(task => task.Code));
        Assert.DoesNotContain(international.Tasks, task => task.Code == "book-movers");
    }

    private static Relocation Move(string origin, string destination)
    {
        return new Relocation
        {
            Name = "Move",
            OriginCountryCode = origin,
            OriginCity = "From",
            DestinationCountryCode = destination,
            DestinationCity = "To",
            Purpose = RelocationPurpose.Work,
            TargetDate = new DateOnly(2026, 12, 1),
        };
    }

    [Fact]
    public async Task Published_starter_template_cannot_be_changed()
    {
        await using var db = _postgres.CreateContext();
        var template = await db.ChecklistTemplates.SingleAsync(candidate =>
            candidate.Code == StarterChecklistTemplates.DomesticCode);
        template.Title = "Changed";

        await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
    }
}
