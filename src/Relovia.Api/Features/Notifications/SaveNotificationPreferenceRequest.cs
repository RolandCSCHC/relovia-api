using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Notifications;

public sealed class SaveNotificationPreferenceRequest
{
    [JsonRequired]
    public NotificationChannel Channel { get; set; }

    [JsonRequired]
    public int DaysBefore { get; set; }
}
