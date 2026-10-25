using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Calendar;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}")]
public sealed class CalendarController(CalendarService calendar) : ControllerBase
{
    [HttpGet("calendar")]
    [EndpointName("getRelocationCalendar")]
    [EndpointSummary("Lists calendar events and, separately, task due dates.")]
    public async Task<ActionResult<RelocationCalendarResponse>> Get(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await calendar.GetAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPost("events")]
    [EndpointName("createCalendarEvent")]
    [EndpointSummary("Adds a milestone or an appointment. A task due date is left unchanged.")]
    public async Task<ActionResult<CalendarEventResponse>> Create(
        Guid relocationId,
        SaveCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await calendar.CreateAsync(userId, relocationId, request, cancellationToken),
            calendarEvent => Created(
                $"/api/relocations/{relocationId}/events/{calendarEvent.Id}",
                calendarEvent));
    }

    [HttpPut("events/{eventId:guid}")]
    [EndpointName("updateCalendarEvent")]
    [EndpointSummary("Updates a milestone or an appointment. A task due date is left unchanged.")]
    public async Task<ActionResult<CalendarEventResponse>> Update(
        Guid relocationId,
        Guid eventId,
        SaveCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await calendar.UpdateAsync(userId, relocationId, eventId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("events/{eventId:guid}")]
    [EndpointName("deleteCalendarEvent")]
    [EndpointSummary("Deletes a calendar event. The relocation's tasks stay.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await calendar.DeleteAsync(userId, relocationId, eventId, cancellationToken),
            _ => NoContent());
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    private ActionResult Map<T>(CalendarResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            CalendarResult<T>.Success ok => success(ok.Value),
            CalendarResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            CalendarResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected calendar result."),
        };
    }
}
