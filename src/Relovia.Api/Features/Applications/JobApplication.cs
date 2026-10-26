using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Applications;

/// <summary>
/// A job application the user is tracking. The next date is not a calendar event.
/// </summary>
public sealed class JobApplication : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RelocationId { get; set; }

    public string Employer { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateOnly? AppliedOn { get; set; }

    public DateOnly? NextOn { get; set; }

    public JobApplicationStatus Status { get; set; }

    /// <summary>True when the role includes visa or relocation support. Empty when the user does not know.</summary>
    public bool? IncludesSupport { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Relocation Relocation { get; set; } = null!;
}
