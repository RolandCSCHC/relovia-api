using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed class ChecklistTemplatePersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public ChecklistTemplatePersistenceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Draft_template_stores_task_codes_phases_and_dependency_codes()
    {
        Guid templateId;

        await using (var db = _postgres.CreateContext())
        {
            var template = Template("draft-core");
            var notice = Task(template, "give-notice", "Give notice", daysBeforeMove: 60);
            var movers = Task(template, "book-movers", "Book movers", daysBeforeMove: 30);
            movers.ReplaceDependencies(["give-notice"]);
            template.Tasks.Add(notice);
            template.Tasks.Add(movers);
            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();
            templateId = template.Id;
        }

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.ChecklistTemplates
                .Include(template => template.Tasks)
                .ThenInclude(task => task.Dependencies)
                .SingleAsync(template => template.Id == templateId);
            var scope = await db.Database
                .SqlQueryRaw<string>("""SELECT scope AS "Value" FROM checklist_templates WHERE id = {0}""", templateId)
                .SingleAsync();
            var priority = await db.Database
                .SqlQueryRaw<string>(
                    """SELECT priority AS "Value" FROM task_templates WHERE checklist_template_id = {0} AND code = 'book-movers'""",
                    templateId)
                .SingleAsync();

            Assert.Equal("draft-core", stored.Code);
            Assert.Equal(1, stored.Version);
            Assert.Equal(ChecklistScope.International, stored.Scope);
            Assert.Equal("International", scope);
            Assert.Equal("DE", stored.DestinationCountryCode);
            Assert.Equal(RelocationPurpose.Work, stored.Purpose);
            Assert.Null(stored.PublishedAt);
            Assert.Equal(["book-movers", "give-notice"], stored.Tasks.Select(task => task.Code).Order());
            var movers = stored.Tasks.Single(task => task.Code == "book-movers");
            Assert.Equal(30, movers.DaysBeforeMove);
            Assert.Equal(TaskPhase.Moving, movers.Phase);
            Assert.Equal(TaskPriority.High, movers.Priority);
            Assert.Equal("High", priority);
            Assert.Equal("give-notice", Assert.Single(movers.Dependencies).DependsOnCode);
        }
    }

    [Fact]
    public async Task Optional_destination_country_and_purpose_stay_unset()
    {
        Guid templateId;

        await using (var db = _postgres.CreateContext())
        {
            var template = Template("both-core");
            template.Scope = ChecklistScope.Both;
            template.DestinationCountryCode = "  ";
            template.Purpose = null;
            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();
            templateId = template.Id;
        }

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.ChecklistTemplates.SingleAsync(template => template.Id == templateId);

            Assert.Null(stored.DestinationCountryCode);
            Assert.Null(stored.Purpose);
        }
    }

    [Fact]
    public async Task Code_and_version_cannot_be_stored_twice()
    {
        await using var db = _postgres.CreateContext();
        db.ChecklistTemplates.Add(Template("duplicate-core"));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        db.ChecklistTemplates.Add(Template("duplicate-core"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ux_checklist_templates_code_version", postgres.ConstraintName);
    }

    [Fact]
    public async Task Task_code_is_unique_inside_one_template_and_reusable_on_another()
    {
        Guid firstId;
        Guid secondId;

        await using (var db = _postgres.CreateContext())
        {
            var first = Template("task-code-a");
            first.Tasks.Add(Task(first, "give-notice", "Give notice", daysBeforeMove: 60));
            var second = Template("task-code-b");
            second.Scope = ChecklistScope.Domestic;
            second.DestinationCountryCode = null;
            second.Purpose = null;
            second.Tasks.Add(Task(second, "give-notice", "Give notice", daysBeforeMove: 30));
            db.ChecklistTemplates.AddRange(first, second);
            await db.SaveChangesAsync();
            firstId = first.Id;
            secondId = second.Id;

            db.ChangeTracker.Clear();
            db.TaskTemplates.Add(Task(firstId, "give-notice", "Give notice again", daysBeforeMove: 10));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("ux_task_templates_template_id_code", postgres.ConstraintName);
        }

        await using (var db = _postgres.CreateContext())
        {
            var codes = await db.TaskTemplates
                .Where(task => task.ChecklistTemplateId == firstId || task.ChecklistTemplateId == secondId)
                .Select(task => task.Code)
                .ToListAsync();

            Assert.Equal(["give-notice", "give-notice"], codes.Order());
        }
    }

    [Fact]
    public async Task Published_version_cannot_be_changed_and_a_new_version_can()
    {
        Guid publishedId;

        await using (var db = _postgres.CreateContext())
        {
            var template = Template("published-core");
            var task = Task(template, "give-notice", "Give notice", daysBeforeMove: 60);
            template.Tasks.Add(task);
            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();

            template.Title = "Final title";
            task.DaysBeforeMove = 45;
            template.PublishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            publishedId = template.Id;
        }

        await using (var db = _postgres.CreateContext())
        {
            var published = await db.ChecklistTemplates
                .Include(template => template.Tasks)
                .SingleAsync(template => template.Id == publishedId);
            published.Title = "Changed after publish";

            await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
        }

        await using (var db = _postgres.CreateContext())
        {
            var task = await db.TaskTemplates.SingleAsync(candidate => candidate.ChecklistTemplateId == publishedId);
            task.Title = "Changed task";

            await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
        }

        await using (var db = _postgres.CreateContext())
        {
            db.TaskTemplates.Add(Task(publishedId, "book-movers", "Book movers", daysBeforeMove: 30));

            await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
        }

        await using (var db = _postgres.CreateContext())
        {
            var task = await db.TaskTemplates
                .Include(candidate => candidate.Dependencies)
                .SingleAsync(candidate => candidate.ChecklistTemplateId == publishedId);
            task.ReplaceDependencies(["book-movers"]);

            await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
        }

        await using (var db = _postgres.CreateContext())
        {
            var published = await db.ChecklistTemplates.SingleAsync(template => template.Id == publishedId);
            db.ChecklistTemplates.Remove(published);

            await Assert.ThrowsAsync<PublishedTemplateChangeException>(() => db.SaveChangesAsync());
        }

        await using (var db = _postgres.CreateContext())
        {
            var next = Template("published-core");
            next.Version = 2;
            next.Title = "Next version";
            db.ChecklistTemplates.Add(next);
            await db.SaveChangesAsync();

            var stored = await db.ChecklistTemplates
                .Include(template => template.Tasks)
                .SingleAsync(template => template.Id == publishedId);
            Assert.Equal("Final title", stored.Title);
            Assert.NotNull(stored.PublishedAt);
            Assert.Equal(45, Assert.Single(stored.Tasks).DaysBeforeMove);
        }
    }

    [Fact]
    public async Task Deleting_a_draft_template_deletes_its_tasks()
    {
        Guid templateId;
        Guid dependencyId;

        await using (var db = _postgres.CreateContext())
        {
            var template = Template("draft-to-delete");
            var task = Task(template, "give-notice", "Give notice", daysBeforeMove: 60);
            task.ReplaceDependencies(["book-movers"]);
            template.Tasks.Add(task);
            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();
            templateId = template.Id;
            dependencyId = task.Dependencies.Single().Id;
        }

        await using (var db = _postgres.CreateContext())
        {
            var template = await db.ChecklistTemplates.SingleAsync(candidate => candidate.Id == templateId);
            db.ChecklistTemplates.Remove(template);
            await db.SaveChangesAsync();
        }

        await using (var db = _postgres.CreateContext())
        {
            Assert.False(await db.ChecklistTemplates.AnyAsync(template => template.Id == templateId));
            Assert.False(await db.TaskTemplates.AnyAsync(task => task.ChecklistTemplateId == templateId));
            Assert.False(await db.TaskTemplateDependencies.AnyAsync(dependency => dependency.Id == dependencyId));
        }
    }

    private static ChecklistTemplate Template(string code)
    {
        return new ChecklistTemplate
        {
            Code = code,
            Version = 1,
            Title = "International core",
            Scope = ChecklistScope.International,
            DestinationCountryCode = "de",
            Purpose = RelocationPurpose.Work,
        };
    }

    private static TaskTemplate Task(ChecklistTemplate template, string code, string title, int daysBeforeMove)
    {
        return Task(template.Id, code, title, daysBeforeMove);
    }

    private static TaskTemplate Task(Guid templateId, string code, string title, int daysBeforeMove)
    {
        return new TaskTemplate
        {
            ChecklistTemplateId = templateId,
            Code = code,
            Title = title,
            DaysBeforeMove = daysBeforeMove,
            Phase = TaskPhase.Moving,
            Priority = TaskPriority.High,
        };
    }
}
