using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Applications;

public sealed class JobApplicationService(ReloviaDbContext db)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string ApplicationNotFoundTitle = "Application was not found.";

    public async Task<JobApplicationResult<IReadOnlyList<JobApplicationResponse>>> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new JobApplicationResult<IReadOnlyList<JobApplicationResponse>>.NotFound(RelocationNotFoundTitle);
        }

        var applications = await db.JobApplications.AsNoTracking()
            .Where(application => application.RelocationId == relocationId)
            .OrderBy(application => application.NextOn == null)
            .ThenBy(application => application.NextOn)
            .ThenBy(application => application.Employer)
            .ThenBy(application => application.Role)
            .ThenBy(application => application.Id)
            .ToListAsync(cancellationToken);
        return new JobApplicationResult<IReadOnlyList<JobApplicationResponse>>.Success(
            applications.Select(JobApplicationResponse.From).ToArray());
    }

    public async Task<JobApplicationResult<JobApplicationResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        SaveJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new JobApplicationResult<JobApplicationResponse>.NotFound(RelocationNotFoundTitle);
        }

        if (!relocation.IsInternational)
        {
            return new JobApplicationResult<JobApplicationResponse>.Invalid(InternationalOnly());
        }

        var errors = JobApplicationRules.Validate(request);
        if (errors.Count > 0)
        {
            return new JobApplicationResult<JobApplicationResponse>.Invalid(errors);
        }

        var application = new JobApplication { RelocationId = relocationId };
        Apply(application, request);
        db.JobApplications.Add(application);
        await db.SaveChangesAsync(cancellationToken);
        return new JobApplicationResult<JobApplicationResponse>.Success(JobApplicationResponse.From(application));
    }

    public async Task<JobApplicationResult<JobApplicationResponse>> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid applicationId,
        SaveJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new JobApplicationResult<JobApplicationResponse>.NotFound(RelocationNotFoundTitle);
        }

        var errors = JobApplicationRules.Validate(request);
        if (errors.Count > 0)
        {
            return new JobApplicationResult<JobApplicationResponse>.Invalid(errors);
        }

        var application = await FindAsync(relocationId, applicationId, cancellationToken);
        if (application is null)
        {
            return new JobApplicationResult<JobApplicationResponse>.NotFound(ApplicationNotFoundTitle);
        }

        Apply(application, request);
        await db.SaveChangesAsync(cancellationToken);
        return new JobApplicationResult<JobApplicationResponse>.Success(JobApplicationResponse.From(application));
    }

    public async Task<JobApplicationResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new JobApplicationResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var application = await FindAsync(relocationId, applicationId, cancellationToken);
        if (application is null)
        {
            return new JobApplicationResult<Guid>.NotFound(ApplicationNotFoundTitle);
        }

        db.JobApplications.Remove(application);
        await db.SaveChangesAsync(cancellationToken);
        return new JobApplicationResult<Guid>.Success(applicationId);
    }

    private static void Apply(JobApplication application, SaveJobApplicationRequest request)
    {
        application.Employer = JobApplicationRules.NormalizeRequired(request.Employer);
        application.Role = JobApplicationRules.NormalizeRequired(request.Role);
        application.Location = JobApplicationRules.NormalizeOptional(request.Location);
        application.Notes = JobApplicationRules.NormalizeOptional(request.Notes);
        application.AppliedOn = request.AppliedOn;
        application.NextOn = request.NextOn;
        application.Status = request.Status;
        application.IncludesSupport = request.IncludesSupport;
    }

    private static Dictionary<string, string[]> InternationalOnly()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["relocation"] = [JobApplicationRules.InternationalOnlyMessage],
        };
    }

    private async Task<JobApplication?> FindAsync(
        Guid relocationId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return await db.JobApplications.SingleOrDefaultAsync(
            application => application.Id == applicationId && application.RelocationId == relocationId,
            cancellationToken);
    }

    private async Task<Relocation?> FindRelocationAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AsNoTracking().SingleOrDefaultAsync(
            relocation => relocation.Id == relocationId && relocation.UserId == userId,
            cancellationToken);
    }

    private async Task<bool> OwnsRelocationAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AsNoTracking()
            .AnyAsync(relocation => relocation.Id == relocationId && relocation.UserId == userId, cancellationToken);
    }
}
