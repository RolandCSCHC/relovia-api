using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Relocations;

public sealed class RelocationService(ReloviaDbContext db)
{
    public async Task<IReadOnlyList<RelocationResponse>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var relocations = await db.Relocations.AsNoTracking()
            .Where(relocation => relocation.UserId == userId)
            .OrderBy(relocation => relocation.TargetDate == null)
            .ThenBy(relocation => relocation.TargetDate)
            .ThenBy(relocation => relocation.Name)
            .ThenBy(relocation => relocation.Id)
            .ToListAsync(cancellationToken);

        return relocations.Select(RelocationResponse.From).ToArray();
    }

    public async Task<RelocationResponse?> FindAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        var relocation = await db.Relocations.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == relocationId && candidate.UserId == userId,
                cancellationToken);

        return relocation is null ? null : RelocationResponse.From(relocation);
    }

    public async Task<RelocationSaveResult> CreateAsync(
        Guid userId,
        SaveRelocationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = RelocationRules.Validate(request);
        if (errors.Count > 0)
        {
            return new RelocationSaveResult.Invalid(errors);
        }

        var relocation = new Relocation
        {
            UserId = userId,
        };
        Apply(relocation, request);
        db.Relocations.Add(relocation);
        await db.SaveChangesAsync(cancellationToken);

        return new RelocationSaveResult.Saved(RelocationResponse.From(relocation));
    }

    public async Task<RelocationSaveResult> UpdateAsync(
        Guid userId,
        Guid relocationId,
        SaveRelocationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = RelocationRules.Validate(request);
        if (errors.Count > 0)
        {
            return new RelocationSaveResult.Invalid(errors);
        }

        var relocation = await db.Relocations.SingleOrDefaultAsync(
            candidate => candidate.Id == relocationId && candidate.UserId == userId,
            cancellationToken);
        if (relocation is null)
        {
            return new RelocationSaveResult.NotFound();
        }

        var previousTargetDate = relocation.TargetDate;
        Apply(relocation, request);
        if (previousTargetDate != relocation.TargetDate)
        {
            var tasks = await db.RelocationTasks
                .Where(task => task.RelocationId == relocation.Id)
                .ToListAsync(cancellationToken);
            foreach (var task in tasks)
            {
                task.DueDate = ChecklistDueDate.On(relocation.TargetDate, task.DaysBeforeMove);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return new RelocationSaveResult.Saved(RelocationResponse.From(relocation));
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid relocationId, CancellationToken cancellationToken)
    {
        var relocation = await db.Relocations.SingleOrDefaultAsync(
            candidate => candidate.Id == relocationId && candidate.UserId == userId,
            cancellationToken);
        if (relocation is null)
        {
            return false;
        }

        var dependencies = await db.TaskDependencies
            .Where(dependency => dependency.RelocationId == relocation.Id)
            .ToListAsync(cancellationToken);
        db.TaskDependencies.RemoveRange(dependencies);

        var tasks = await db.RelocationTasks
            .Where(task => task.RelocationId == relocation.Id)
            .ToListAsync(cancellationToken);
        db.RelocationTasks.RemoveRange(tasks);

        db.Relocations.Remove(relocation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(Relocation relocation, SaveRelocationRequest request)
    {
        relocation.Name = request.Name.Trim();
        relocation.OriginCountryCode = CountryCodes.Normalize(request.OriginCountryCode);
        relocation.OriginCity = request.OriginCity.Trim();
        relocation.DestinationCountryCode = CountryCodes.Normalize(request.DestinationCountryCode);
        relocation.DestinationCity = request.DestinationCity.Trim();
        relocation.TargetDate = request.TargetDate;
        relocation.Purpose = request.Purpose;
        relocation.Status = request.Status;
    }
}
