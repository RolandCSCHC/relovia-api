using System.Text.Json.Serialization;

namespace Relovia.Api.Features.Applications;

public sealed class SaveJobApplicationRequest
{
    [JsonRequired]
    public string Employer { get; set; } = string.Empty;

    [JsonRequired]
    public string Role { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateOnly? AppliedOn { get; set; }

    public DateOnly? NextOn { get; set; }

    [JsonRequired]
    public JobApplicationStatus Status { get; set; }

    public bool? IncludesSupport { get; set; }

    public string? Notes { get; set; }
}
