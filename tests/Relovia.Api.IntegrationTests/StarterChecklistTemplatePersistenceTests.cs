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
    public async Task Published_starter_template_cannot_be_changed()
    {
        await using var db = _postgres.CreateContext();
        var template = await db.ChecklistTemplates.SingleAsync(candidate =>
            candidate.Code == StarterChecklistTemplates.DomesticCode);
        template.Title = "Changed";

        await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
    }
}
