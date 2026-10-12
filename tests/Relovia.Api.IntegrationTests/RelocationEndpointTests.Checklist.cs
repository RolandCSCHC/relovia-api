using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task Confirm_persists_only_the_owners_tasks_and_reports_progress()
    {
        var owner = await SignInAsync("checklist-owner@example.com");
        var other = await SignInAsync("checklist-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var targetDate = new DateOnly(2026, 12, 1);

        var created = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Checklist", targetDate: targetDate, destinationCountry: "US"));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var relocationId = created.Body!.Id;

        var anonymous = await CreateClient().Client.GetAsync($"/api/relocations/{relocationId}/checklist");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var suggestion = await SendAsync<ChecklistSuggestionResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/checklist",
            body: null);
        Assert.Equal(HttpStatusCode.OK, suggestion.Status);
        Assert.Equal([StarterChecklistTemplates.DomesticCode], suggestion.Body!.TemplateCodes);
        var notice = suggestion.Body.Tasks.Single(task => task.Code == "give-notice");
        var movers = suggestion.Body.Tasks.Single(task => task.Code == "book-movers");
        var unpack = suggestion.Body.Tasks.Single(task => task.Code == "unpack-essentials");
        Assert.Equal(new DateOnly(2026, 10, 2), notice.DueDate);
        Assert.Equal(new DateOnly(2026, 12, 2), unpack.DueDate);
        Assert.Equal(["give-notice"], movers.DependsOnCodes);
        Assert.DoesNotContain(suggestion.Body.Tasks, task => task.Code == "check-passport");

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.RelocationTasks.CountAsync(task => task.RelocationId == relocationId);
            Assert.Equal(0, stored);
        }

        var hidden = await SendAsync<ChecklistSuggestionResponse>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/checklist",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);

        var rejected = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/checklist",
            new { tasks = new[] { Payload(notice, code: "not-a-task") } });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.Status);
        Assert.Equal(
            "Choose a task from the suggestion.",
            rejected.Body!.GetProperty("errors").GetProperty("tasks[0].code")[0].GetString());

        var confirmed = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/checklist",
            new
            {
                tasks = new[]
                {
                    Payload(notice, title: "Edited notice", daysBeforeMove: 10),
                    Payload(movers),
                },
            });
        Assert.Equal(HttpStatusCode.Created, confirmed.Status);
        Assert.Equal($"/api/relocations/{relocationId}/tasks", confirmed.Response.Headers.Location?.OriginalString);
        Assert.Equal(["book-movers", "give-notice"], confirmed.Body!.Tasks.Select(task => task.Code));
        Assert.Equal(0, confirmed.Body.Progress.Done);
        Assert.Equal(2, confirmed.Body.Progress.Counted);
        var confirmedNotice = confirmed.Body.Tasks.Single(task => task.Code == "give-notice");
        var confirmedMovers = confirmed.Body.Tasks.Single(task => task.Code == "book-movers");
        Assert.Equal("Edited notice", confirmedNotice.Title);
        Assert.Equal(new DateOnly(2026, 11, 21), confirmedNotice.DueDate);
        Assert.Equal(RelocationTaskStatus.Todo, confirmedNotice.Status);
        Assert.Equal(notice.SourceTemplateTaskId, confirmedNotice.SourceTemplateTaskId);
        Assert.False(Assert.Single(confirmedMovers.Dependencies).IsSatisfied);
        Assert.Equal(confirmedNotice.Id, confirmedMovers.Dependencies[0].TaskId);

        await using (var db = _postgres.CreateContext())
        {
            var stored = await db.RelocationTasks
                .Include(task => task.Dependencies)
                .Where(task => task.RelocationId == relocationId)
                .ToListAsync();
            Assert.Equal(2, stored.Count);
            Assert.Equal(
                confirmedNotice.Id,
                Assert.Single(stored.Single(task => task.Code == "book-movers").Dependencies).DependsOnTaskId);
            var otherTasks = await db.RelocationTasks.CountAsync(task => task.Relocation.UserId == other.UserId);
            Assert.Equal(0, otherTasks);
        }

        var otherList = await SendAsync<RelocationTasksResponse>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/tasks",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, otherList.Status);

        var otherUpdate = await SendAsync<RelocationTaskResponse>(
            otherClient,
            other.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/tasks/{confirmedMovers.Id}",
            UpdatePayload(confirmedMovers, status: "Done"));
        Assert.Equal(HttpStatusCode.NotFound, otherUpdate.Status);

        var otherConfirm = await SendAsync<RelocationTasksResponse>(
            otherClient,
            other.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/checklist",
            new { tasks = new[] { Payload(movers) } });
        Assert.Equal(HttpStatusCode.NotFound, otherConfirm.Status);

        var done = await SendAsync<RelocationTaskResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/tasks/{confirmedMovers.Id}",
            UpdatePayload(confirmedMovers, status: "Done"));
        Assert.Equal(HttpStatusCode.OK, done.Status);
        Assert.Equal(RelocationTaskStatus.Done, done.Body!.Status);

        var listed = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/tasks",
            body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal(1, listed.Body!.Progress.Done);
        Assert.Equal(2, listed.Body.Progress.Counted);
        Assert.Equal(0.5m, listed.Body.Progress.Ratio);
        Assert.False(listed.Body.Tasks.Single(task => task.Code == "book-movers").Dependencies.Single().IsSatisfied);

        var skipped = await SendAsync<RelocationTaskResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/tasks/{confirmedNotice.Id}",
            UpdatePayload(confirmedNotice, title: "Changed again", status: "Skipped"));
        Assert.Equal(HttpStatusCode.OK, skipped.Status);
        Assert.True(skipped.Body!.Dependencies.Count == 0);

        var afterSkip = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/tasks",
            body: null);
        Assert.Equal(1, afterSkip.Body!.Progress.Done);
        Assert.Equal(1, afterSkip.Body.Progress.Counted);
        Assert.Equal(1m, afterSkip.Body.Progress.Ratio);
        Assert.True(afterSkip.Body.Tasks.Single(task => task.Code == "book-movers").Dependencies.Single().IsSatisfied);

        await using (var db = _postgres.CreateContext())
        {
            var templateTitle = await db.TaskTemplates
                .Where(task => task.Id == notice.SourceTemplateTaskId)
                .Select(task => task.Title)
                .SingleAsync();
            Assert.Equal("Give notice on your current home", templateTitle);
        }

        var again = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/checklist",
            new { tasks = new[] { Payload(movers) } });
        Assert.Equal(HttpStatusCode.Conflict, again.Status);

        var deleted = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);

        await using (var db = _postgres.CreateContext())
        {
            Assert.Equal(0, await db.RelocationTasks.CountAsync(task => task.RelocationId == relocationId));
            Assert.Equal(0, await db.TaskDependencies.CountAsync(dependency => dependency.RelocationId == relocationId));
        }
    }

    [Fact]
    public async Task A_dependency_cycle_is_rejected_without_writing_tasks()
    {
        var owner = await SignInAsync("checklist-cycle@example.com");
        using var client = CreateClient();

        await using (var db = _postgres.CreateContext())
        {
            var template = new ChecklistTemplate
            {
                Code = "cycle-jp",
                Version = 1,
                Title = "Cycle",
                Scope = ChecklistScope.International,
                DestinationCountryCode = "JP",
                PublishedAt = StarterChecklistTemplates.PublishedAt,
            };
            var first = new TaskTemplate
            {
                Code = "cycle-a",
                Title = "Cycle A",
                DaysBeforeMove = 3,
                Phase = TaskPhase.Preparation,
                Priority = TaskPriority.Normal,
            };
            var second = new TaskTemplate
            {
                Code = "cycle-b",
                Title = "Cycle B",
                DaysBeforeMove = 2,
                Phase = TaskPhase.Preparation,
                Priority = TaskPriority.Normal,
            };
            first.ReplaceDependencies(["cycle-b"]);
            second.ReplaceDependencies(["cycle-a"]);
            template.Tasks.Add(first);
            template.Tasks.Add(second);
            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        var created = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Cycle", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "JP"));
        Assert.Equal(HttpStatusCode.Created, created.Status);

        var suggestion = await SendAsync<ChecklistSuggestionResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{created.Body!.Id}/checklist",
            body: null);
        Assert.Equal(HttpStatusCode.OK, suggestion.Status);
        var cycleA = suggestion.Body!.Tasks.Single(task => task.Code == "cycle-a");
        var cycleB = suggestion.Body.Tasks.Single(task => task.Code == "cycle-b");

        var rejected = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{created.Body.Id}/checklist",
            new { tasks = new[] { Payload(cycleA), Payload(cycleB) } });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.Status);
        Assert.Equal(
            TaskDependencyRules.CycleMessage,
            rejected.Body!.GetProperty("errors").GetProperty("tasks")[0].GetString());

        await using var check = _postgres.CreateContext();
        Assert.Equal(0, await check.RelocationTasks.CountAsync(task => task.RelocationId == created.Body.Id));
    }

    [Fact]
    public async Task Deleting_a_template_task_clears_the_source_without_deleting_the_user_task()
    {
        Guid relocationTaskId;

        await using (var db = _postgres.CreateContext())
        {
            var template = new ChecklistTemplate
            {
                Code = "draft-source",
                Title = "Draft",
                Scope = ChecklistScope.Domestic,
            };
            var source = new TaskTemplate
            {
                Code = "draft-task",
                Title = "Draft task",
                DaysBeforeMove = 1,
                Phase = TaskPhase.Preparation,
                Priority = TaskPriority.Low,
            };
            template.Tasks.Add(source);
            var relocation = new Relocation
            {
                User = new User { Email = "source-owner@example.com", DisplayName = "Source" },
                Name = "Source",
                OriginCountryCode = "US",
                OriginCity = "Austin",
                DestinationCountryCode = "US",
                DestinationCity = "Seattle",
                TargetDate = new DateOnly(2026, 12, 1),
                Purpose = RelocationPurpose.Work,
            };
            var task = new RelocationTask
            {
                Relocation = relocation,
                Code = "draft-task",
                Title = "Keep me",
                DaysBeforeMove = 1,
                DueDate = new DateOnly(2026, 11, 30),
                Phase = TaskPhase.Preparation,
                Priority = TaskPriority.Low,
                SourceTemplateTask = source,
            };
            db.ChecklistTemplates.Add(template);
            db.RelocationTasks.Add(task);
            await db.SaveChangesAsync();
            relocationTaskId = task.Id;
            db.ChecklistTemplates.Remove(template);
            await db.SaveChangesAsync();
        }

        await using var check = _postgres.CreateContext();
        var stored = await check.RelocationTasks.SingleAsync(task => task.Id == relocationTaskId);
        Assert.Equal("Keep me", stored.Title);
        Assert.Null(stored.SourceTemplateTaskId);
    }

    [Fact]
    public async Task A_dependency_cannot_point_at_a_task_in_another_relocation()
    {
        await using var db = _postgres.CreateContext();
        var user = new User { Email = "cross-owner@example.com", DisplayName = "Cross" };
        var origin = RelocationFor(user, "US", "US");
        var other = RelocationFor(user, "US", "DE");
        var originTask = TaskFor(origin, "notice");
        var otherTask = TaskFor(other, "passport");
        db.AddRange(originTask, otherTask);
        await db.SaveChangesAsync();

        db.TaskDependencies.Add(new TaskDependency
        {
            RelocationId = origin.Id,
            TaskId = originTask.Id,
            DependsOnTaskId = otherTask.Id,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
    }

    private static object Payload(
        SuggestedTaskResponse task,
        string? code = null,
        string? title = null,
        int? daysBeforeMove = null)
    {
        return new
        {
            code = code ?? task.Code,
            title = title ?? task.Title,
            daysBeforeMove = daysBeforeMove ?? task.DaysBeforeMove,
            phase = task.Phase.ToString(),
            priority = task.Priority.ToString(),
        };
    }

    private static object UpdatePayload(RelocationTaskResponse task, string? title = null, string? status = null)
    {
        return new
        {
            title = title ?? task.Title,
            dueDate = task.DueDate,
            phase = task.Phase.ToString(),
            priority = task.Priority.ToString(),
            status = status ?? task.Status.ToString(),
        };
    }

    private static Relocation RelocationFor(User user, string origin, string destination)
    {
        return new Relocation
        {
            User = user,
            Name = $"{origin}-{destination}",
            OriginCountryCode = origin,
            OriginCity = "From",
            DestinationCountryCode = destination,
            DestinationCity = "To",
            TargetDate = new DateOnly(2026, 12, 1),
            Purpose = RelocationPurpose.Work,
        };
    }

    private static RelocationTask TaskFor(Relocation relocation, string code)
    {
        return new RelocationTask
        {
            Relocation = relocation,
            Code = code,
            Title = code,
            DaysBeforeMove = 1,
            DueDate = new DateOnly(2026, 11, 30),
            Phase = TaskPhase.Preparation,
            Priority = TaskPriority.Normal,
        };
    }
}
