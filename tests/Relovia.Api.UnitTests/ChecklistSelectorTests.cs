using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class ChecklistSelectorTests
{
    [Fact]
    public void A_domestic_move_selects_domestic_and_both_templates()
    {
        var selection = ChecklistSelector.Select(
            Move("US", "US"),
            [
                Template("domestic-core", ChecklistScope.Domestic, taskCode: "give-notice"),
                Template("international-core", ChecklistScope.International, taskCode: "check-passport"),
                Template("shared", ChecklistScope.Both, taskCode: "change-address"),
                Template("draft", ChecklistScope.Domestic, taskCode: "secret", published: false),
            ]);

        Assert.Equal(["domestic-core", "shared"], selection.Templates.Select(template => template.Code));
        Assert.Equal(["change-address", "give-notice"], selection.Tasks.Select(task => task.Code).Order());
    }

    [Fact]
    public void An_international_move_selects_international_and_both_templates()
    {
        var selection = ChecklistSelector.Select(
            Move("US", "DE"),
            [
                Template("domestic-core", ChecklistScope.Domestic, taskCode: "book-movers"),
                Template("international-core", ChecklistScope.International, taskCode: "check-passport"),
                Template("shared", ChecklistScope.Both, taskCode: "change-address"),
            ]);

        Assert.Equal(["international-core", "shared"], selection.Templates.Select(template => template.Code));
        Assert.Equal(["change-address", "check-passport"], selection.Tasks.Select(task => task.Code).Order());
    }

    [Fact]
    public void Destination_country_and_purpose_match_only_when_the_template_sets_them()
    {
        var selection = ChecklistSelector.Select(
            Move("DE", "de", RelocationPurpose.Work),
            [
                Template("any-domestic", ChecklistScope.Domestic, taskCode: "any"),
                Template("germany", ChecklistScope.Domestic, taskCode: "germany", country: "DE"),
                Template("france", ChecklistScope.Domestic, taskCode: "france", country: "FR"),
                Template("work", ChecklistScope.Domestic, taskCode: "work", purpose: RelocationPurpose.Work),
                Template("study", ChecklistScope.Domestic, taskCode: "study", purpose: RelocationPurpose.Study),
            ]);

        Assert.Equal(
            ["any-domestic", "germany", "work"],
            selection.Templates.Select(template => template.Code));
    }

    [Fact]
    public void A_newer_published_version_replaces_the_older_one()
    {
        var older = Template("domestic-core", ChecklistScope.Domestic, version: 1, taskCode: "old-task");
        var newer = Template("domestic-core", ChecklistScope.Domestic, version: 2, taskCode: "new-task");
        var draft = Template("domestic-core", ChecklistScope.Domestic, version: 3, taskCode: "draft-task", published: false);

        var selection = ChecklistSelector.Select(Move("US", "US"), [older, newer, draft]);

        var template = Assert.Single(selection.Templates);
        Assert.Equal(2, template.Version);
        Assert.Equal("new-task", Assert.Single(selection.Tasks).Code);
    }

    [Fact]
    public void Tasks_that_share_a_code_keep_the_more_specific_template_and_combine_dependencies()
    {
        var general = Template("domestic-core", ChecklistScope.Domestic);
        var notice = Task(general, "give-notice", "Give notice", days: 60, TaskPhase.Preparation, TaskPriority.Normal);
        Task(general, "book-movers", "Book movers", days: 30, TaskPhase.Moving, TaskPriority.High, "give-notice", "missing-task");

        var germany = Template("de-work", ChecklistScope.Domestic, country: "DE", purpose: RelocationPurpose.Work);
        var specificNotice = Task(
            germany,
            "give-notice",
            "Give notice to the landlord",
            days: 90,
            TaskPhase.Housing,
            TaskPriority.High,
            "register-locally");
        Task(germany, "register-locally", "Register locally", days: 14, TaskPhase.Administration, TaskPriority.Normal);

        var selection = ChecklistSelector.Select(Move("DE", "DE", RelocationPurpose.Work), [general, germany]);

        Assert.Equal(["de-work", "domestic-core"], selection.Templates.Select(template => template.Code));
        Assert.Equal(
            ["give-notice", "book-movers", "register-locally"],
            selection.Tasks.Select(task => task.Code));

        var selectedNotice = selection.Tasks.Single(task => task.Code == "give-notice");
        Assert.Equal(specificNotice.Id, selectedNotice.SourceTemplateTaskId);
        Assert.Equal("Give notice to the landlord", selectedNotice.Title);
        Assert.Equal(90, selectedNotice.DaysBeforeMove);
        Assert.Equal(TaskPhase.Housing, selectedNotice.Phase);
        Assert.Equal(TaskPriority.High, selectedNotice.Priority);
        Assert.Equal(["register-locally"], selectedNotice.DependsOnCodes);
        Assert.NotEqual(notice.Id, selectedNotice.SourceTemplateTaskId);

        var movers = selection.Tasks.Single(task => task.Code == "book-movers");
        Assert.Equal(["give-notice"], movers.DependsOnCodes);
    }

    [Fact]
    public void The_same_specificity_keeps_the_template_code_that_sorts_first()
    {
        var alpha = Template("alpha", ChecklistScope.Both);
        Task(alpha, "give-notice", "From alpha", days: 10, TaskPhase.Preparation, TaskPriority.Low, "alpha-step");
        Task(alpha, "alpha-step", "Alpha step", days: 20, TaskPhase.Preparation, TaskPriority.Low);

        var beta = Template("beta", ChecklistScope.Both);
        Task(beta, "give-notice", "From beta", days: 10, TaskPhase.Moving, TaskPriority.High, "beta-step");
        Task(beta, "beta-step", "Beta step", days: 5, TaskPhase.Moving, TaskPriority.High);

        var selection = ChecklistSelector.Select(Move("US", "CA"), [beta, alpha]);

        var notice = selection.Tasks.Single(task => task.Code == "give-notice");
        Assert.Equal("From alpha", notice.Title);
        Assert.Equal(TaskPhase.Preparation, notice.Phase);
        Assert.Equal(["alpha-step", "beta-step"], notice.DependsOnCodes);
    }

    private static Relocation Move(
        string origin,
        string destination,
        RelocationPurpose purpose = RelocationPurpose.Work)
    {
        return new Relocation
        {
            Name = "Move",
            OriginCountryCode = origin,
            OriginCity = "From",
            DestinationCountryCode = destination,
            DestinationCity = "To",
            Purpose = purpose,
            TargetDate = new DateOnly(2026, 12, 1),
        };
    }

    private static ChecklistTemplate Template(
        string code,
        ChecklistScope scope,
        int version = 1,
        string? taskCode = null,
        string? country = null,
        RelocationPurpose? purpose = null,
        bool published = true)
    {
        var template = new ChecklistTemplate
        {
            Code = code,
            Version = version,
            Title = code,
            Scope = scope,
            DestinationCountryCode = country,
            Purpose = purpose,
            PublishedAt = published ? new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero) : null,
        };
        if (taskCode is not null)
        {
            Task(template, taskCode, taskCode, days: 1, TaskPhase.Preparation, TaskPriority.Normal);
        }

        return template;
    }

    private static TaskTemplate Task(
        ChecklistTemplate template,
        string code,
        string title,
        int days,
        TaskPhase phase,
        TaskPriority priority,
        params string[] dependencies)
    {
        var task = new TaskTemplate
        {
            ChecklistTemplateId = template.Id,
            Code = code,
            Title = title,
            DaysBeforeMove = days,
            Phase = phase,
            Priority = priority,
        };
        foreach (var dependency in dependencies)
        {
            task.Dependencies.Add(new TaskTemplateDependency
            {
                TaskTemplateId = task.Id,
                DependsOnCode = dependency,
            });
        }

        template.Tasks.Add(task);
        return task;
    }
}
