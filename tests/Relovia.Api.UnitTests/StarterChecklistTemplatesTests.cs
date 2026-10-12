using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class StarterChecklistTemplatesTests
{
    [Fact]
    public void Domestic_and_international_templates_are_published_without_a_country_or_purpose()
    {
        Assert.Equal(
            [ChecklistScope.Domestic, ChecklistScope.International],
            StarterChecklistTemplates.Templates.Select(template => template.Scope));

        foreach (var template in StarterChecklistTemplates.Templates)
        {
            Assert.Equal(1, template.Version);
            Assert.Equal(StarterChecklistTemplates.PublishedAt, template.PublishedAt);
            Assert.Null(template.DestinationCountryCode);
            Assert.Null(template.Purpose);
            Assert.True(ChecklistTemplateRules.IsCode(template.Code));
        }
    }

    [Fact]
    public void Task_codes_are_unique_inside_each_template_and_dependencies_are_valid()
    {
        foreach (var template in StarterChecklistTemplates.Templates)
        {
            var tasks = StarterChecklistTemplates.Tasks
                .Where(task => task.ChecklistTemplateId == template.Id)
                .ToList();
            Assert.Equal(tasks.Count, tasks.Select(task => task.Code).Distinct().Count());

            foreach (var task in tasks)
            {
                var dependencyCodes = StarterChecklistTemplates.Dependencies
                    .Where(dependency => dependency.TaskTemplateId == task.Id)
                    .Select(dependency => dependency.DependsOnCode);
                Assert.Empty(ChecklistTemplateRules.ValidateDependencies(task.Code, dependencyCodes));
            }
        }
    }

    [Fact]
    public void Give_notice_is_shared_so_overlapping_templates_can_dedupe()
    {
        var codes = StarterChecklistTemplates.Tasks
            .Where(task => task.Code == "give-notice")
            .Select(task => task.ChecklistTemplateId);

        Assert.Equal(
            [StarterChecklistTemplates.DomesticTemplateId, StarterChecklistTemplates.InternationalTemplateId],
            codes);
    }
}
