using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    [Fact]
    public async Task A_calendar_event_stays_separate_from_a_task_due_date()
    {
        var owner = await SignInAsync("calendar-owner@example.com");
        var other = await SignInAsync("calendar-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Wellington", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "NZ"));
        var relocationId = relocation.Body!.Id;
        var calendarPath = $"/api/relocations/{relocationId}/calendar";
        var eventsPath = $"/api/relocations/{relocationId}/events";

        var anonymous = await CreateClient().Client.GetAsync(calendarPath);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var task = await SendAsync<RelocationTaskResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            $"/api/relocations/{relocationId}/tasks",
            new
            {
                title = "Give notice",
                dueDate = new DateOnly(2026, 11, 1),
                phase = "Housing",
                priority = "High",
            });
        Assert.Equal(HttpStatusCode.Created, task.Status);

        var milestone = await SendAsync<CalendarEventResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            eventsPath,
            new
            {
                title = " Move day ",
                kind = "Milestone",
                occursOn = new DateOnly(2026, 12, 1),
                startsAt = (DateTimeOffset?)null,
                endsAt = (DateTimeOffset?)null,
                location = "  ",
            });
        Assert.Equal(HttpStatusCode.Created, milestone.Status);
        Assert.Equal($"/api/relocations/{relocationId}/events/{milestone.Body!.Id}", milestone.Response.Headers.Location?.OriginalString);
        Assert.Equal("Move day", milestone.Body.Title);
        Assert.Equal(CalendarEventKind.Milestone, milestone.Body.Kind);
        Assert.Equal(new DateOnly(2026, 12, 1), milestone.Body.OccursOn);
        Assert.Null(milestone.Body.StartsAt);
        Assert.Null(milestone.Body.EndsAt);
        Assert.Null(milestone.Body.Location);

        var appointment = await SendAsync<CalendarEventResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            eventsPath,
            new
            {
                title = "Key handover",
                kind = "Appointment",
                occursOn = (DateOnly?)null,
                startsAt = new DateTimeOffset(2026, 11, 15, 15, 0, 0, TimeSpan.Zero),
                endsAt = new DateTimeOffset(2026, 11, 15, 16, 0, 0, TimeSpan.Zero),
                location = " Landlord's office ",
            });
        Assert.Equal(HttpStatusCode.Created, appointment.Status);
        Assert.Equal("Landlord's office", appointment.Body!.Location);

        var listed = await SendAsync<RelocationCalendarResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            calendarPath,
            body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal([appointment.Body.Id, milestone.Body.Id], listed.Body!.Events.Select(calendarEvent => calendarEvent.Id));
        Assert.Equal(task.Body!.Id, listed.Body.TaskDueDates.Single().TaskId);
        Assert.Equal(new DateOnly(2026, 11, 1), listed.Body.TaskDueDates[0].DueDate);

        var moved = await SendAsync<RelocationTaskResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{relocationId}/tasks/{task.Body.Id}",
            new
            {
                title = "Give notice",
                dueDate = new DateOnly(2026, 11, 2),
                phase = "Housing",
                priority = "High",
                status = "Todo",
            });
        Assert.Equal(HttpStatusCode.OK, moved.Status);

        var afterTaskEdit = await SendAsync<RelocationCalendarResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            calendarPath,
            body: null);
        Assert.Equal(new DateOnly(2026, 11, 2), afterTaskEdit.Body!.TaskDueDates.Single().DueDate);
        Assert.Equal(appointment.Body.StartsAt, afterTaskEdit.Body.Events[0].StartsAt);
        Assert.Equal(appointment.Body.EndsAt, afterTaskEdit.Body.Events[0].EndsAt);
        Assert.Equal(milestone.Body.OccursOn, afterTaskEdit.Body.Events[1].OccursOn);
        Assert.Equal("Move day", afterTaskEdit.Body.Events[1].Title);

        var renamed = await SendAsync<CalendarEventResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{eventsPath}/{milestone.Body.Id}",
            new
            {
                title = "Keys on move day",
                kind = "Milestone",
                occursOn = new DateOnly(2026, 12, 1),
                location = (string?)null,
            });
        Assert.Equal(HttpStatusCode.OK, renamed.Status);

        var tasks = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{relocationId}/tasks",
            body: null);
        Assert.Equal(new DateOnly(2026, 11, 2), tasks.Body!.Tasks.Single().DueDate);
        Assert.Equal("Give notice", tasks.Body.Tasks.Single().Title);

        var removedTask = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}/tasks/{task.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removedTask.Status);
        var afterTaskDelete = await SendAsync<RelocationCalendarResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            calendarPath,
            body: null);
        Assert.Empty(afterTaskDelete.Body!.TaskDueDates);
        Assert.Equal(2, afterTaskDelete.Body.Events.Count);

        var hidden = await SendAsync<JsonElement>(otherClient, other.Token, HttpMethod.Get, calendarPath, body: null);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);
        var rejected = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Delete,
            $"{eventsPath}/{appointment.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, rejected.Status);

        var milestoneWithTime = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            eventsPath,
            new
            {
                title = "Move day",
                kind = "Milestone",
                occursOn = new DateOnly(2026, 12, 1),
                startsAt = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero),
            });
        var endedTooSoon = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            eventsPath,
            new
            {
                title = "Key handover",
                kind = "Appointment",
                startsAt = new DateTimeOffset(2026, 11, 15, 15, 0, 0, TimeSpan.Zero),
                endsAt = new DateTimeOffset(2026, 11, 15, 15, 0, 0, TimeSpan.Zero),
            });
        Assert.Equal(HttpStatusCode.BadRequest, milestoneWithTime.Status);
        Assert.Equal(HttpStatusCode.BadRequest, endedTooSoon.Status);

        await using (var db = _postgres.CreateContext())
        {
            db.CalendarEvents.Add(new CalendarEvent
            {
                RelocationId = relocationId,
                Title = "Broken",
                Kind = CalendarEventKind.Milestone,
                OccursOn = new DateOnly(2026, 12, 1),
                StartsAt = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero),
            });
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        var deleted = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"{eventsPath}/{appointment.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);

        var removedRelocation = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocationId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, removedRelocation.Status);
        await using var remaining = _postgres.CreateContext();
        Assert.Empty(await remaining.CalendarEvents.Where(calendarEvent => calendarEvent.RelocationId == relocationId).ToListAsync());
    }
}
