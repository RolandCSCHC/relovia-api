namespace Relovia.Api.Features.Applications;

public sealed record JobApplicationResponse(
    Guid Id,
    string Employer,
    string Role,
    string? Location,
    DateOnly? AppliedOn,
    DateOnly? NextOn,
    JobApplicationStatus Status,
    bool? IncludesSupport,
    string? Notes)
{
    public static JobApplicationResponse From(JobApplication application)
    {
        return new JobApplicationResponse(
            application.Id,
            application.Employer,
            application.Role,
            application.Location,
            application.AppliedOn,
            application.NextOn,
            application.Status,
            application.IncludesSupport,
            application.Notes);
    }
}
