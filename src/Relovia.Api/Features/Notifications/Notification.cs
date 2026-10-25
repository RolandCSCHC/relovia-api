using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Notifications;

/// <summary>
/// One reminder for a task, calendar event, or document. The source row is not a foreign key,
/// so deleting that task or file leaves the reminder until the relocation itself is deleted.
/// </summary>
public sealed class Notification : ICreationTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public Guid RelocationId { get; set; }

    public ReminderKind Kind { get; set; }

    public Guid SourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly OccursOn { get; set; }

    /// <summary>False when the user asked for email only. The row still exists so the email is sent once.</summary>
    public bool ShowInApp { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Relocation Relocation { get; set; } = null!;
}
