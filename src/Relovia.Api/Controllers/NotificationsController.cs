using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Notifications;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    [EndpointName("listNotifications")]
    [EndpointSummary("Lists the signed-in user's in-app reminders, unread first.")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await notifications.ListAsync(userId, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    [EndpointName("markNotificationRead")]
    [EndpointSummary("Marks one in-app reminder as read. A second call keeps the first time.")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await notifications.MarkReadAsync(userId, notificationId, cancellationToken);
        return result switch
        {
            NotificationResult<NotificationResponse>.Success ok => Ok(ok.Value),
            NotificationResult<NotificationResponse>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            _ => throw new InvalidOperationException("Unexpected notification result."),
        };
    }

    [HttpPost("read")]
    [EndpointName("markNotificationsRead")]
    [EndpointSummary("Marks every in-app reminder as read.")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await notifications.MarkAllReadAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("preferences")]
    [EndpointName("getNotificationPreference")]
    [EndpointSummary("Returns how the signed-in user wants to be reminded. The default is in the app, 3 days before.")]
    public async Task<ActionResult<NotificationPreferenceResponse>> GetPreference(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await notifications.GetPreferenceAsync(userId, cancellationToken));
    }

    [HttpPut("preferences")]
    [EndpointName("updateNotificationPreference")]
    [EndpointSummary("Sets the channel and how many days before a date to remind the signed-in user.")]
    public async Task<ActionResult<NotificationPreferenceResponse>> UpdatePreference(
        SaveNotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await notifications.UpdatePreferenceAsync(userId, request, cancellationToken);
        return result switch
        {
            NotificationResult<NotificationPreferenceResponse>.Success ok => Ok(ok.Value),
            NotificationResult<NotificationPreferenceResponse>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            _ => throw new InvalidOperationException("Unexpected notification result."),
        };
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }
}
