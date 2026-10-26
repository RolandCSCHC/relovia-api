using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Housing;

public sealed class HousingService(ReloviaDbContext db)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string OptionNotFoundTitle = "Home was not found.";

    public async Task<HousingResult<IReadOnlyList<HousingOptionResponse>>> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new HousingResult<IReadOnlyList<HousingOptionResponse>>.NotFound(RelocationNotFoundTitle);
        }

        var options = await db.HousingOptions.AsNoTracking()
            .Where(option => option.RelocationId == relocationId)
            .ToListAsync(cancellationToken);
        return new HousingResult<IReadOnlyList<HousingOptionResponse>>.Success(Order(options));
    }

    public async Task<HousingResult<HousingOptionResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        SaveHousingOptionRequest request,
        CancellationToken cancellationToken)
    {
        var relocation = await FindRelocationAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new HousingResult<HousingOptionResponse>.NotFound(RelocationNotFoundTitle);
        }

        if (!relocation.IsInternational)
        {
            return new HousingResult<HousingOptionResponse>.Invalid(InternationalOnly());
        }

        var errors = HousingRules.Validate(request);
        if (errors.Count > 0)
        {
            return new HousingResult<HousingOptionResponse>.Invalid(errors);
        }

        var option = new HousingOption { RelocationId = relocationId };
        Apply(option, request);
        if (option.Status == HousingStatus.Chosen
            && await AnotherChosenAsync(relocationId, option.Id, cancellationToken))
        {
            return new HousingResult<HousingOptionResponse>.Invalid(AlreadyChosen());
        }

        db.HousingOptions.Add(option);
        if (!await SaveAsync(cancellationToken))
        {
            return new HousingResult<HousingOptionResponse>.Invalid(AlreadyChosen());
        }

        return new HousingResult<HousingOptionResponse>.Success(HousingOptionResponse.From(option));
    }

    public async Task<HousingResult<HousingOptionResponse>> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid optionId,
        SaveHousingOptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new HousingResult<HousingOptionResponse>.NotFound(RelocationNotFoundTitle);
        }

        var errors = HousingRules.Validate(request);
        if (errors.Count > 0)
        {
            return new HousingResult<HousingOptionResponse>.Invalid(errors);
        }

        var option = await FindAsync(relocationId, optionId, cancellationToken);
        if (option is null)
        {
            return new HousingResult<HousingOptionResponse>.NotFound(OptionNotFoundTitle);
        }

        Apply(option, request);
        if (option.Status == HousingStatus.Chosen
            && await AnotherChosenAsync(relocationId, option.Id, cancellationToken))
        {
            return new HousingResult<HousingOptionResponse>.Invalid(AlreadyChosen());
        }

        if (!await SaveAsync(cancellationToken))
        {
            return new HousingResult<HousingOptionResponse>.Invalid(AlreadyChosen());
        }

        return new HousingResult<HousingOptionResponse>.Success(HousingOptionResponse.From(option));
    }

    public async Task<HousingResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsRelocationAsync(userId, relocationId, cancellationToken))
        {
            return new HousingResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var option = await FindAsync(relocationId, optionId, cancellationToken);
        if (option is null)
        {
            return new HousingResult<Guid>.NotFound(OptionNotFoundTitle);
        }

        db.HousingOptions.Remove(option);
        await db.SaveChangesAsync(cancellationToken);
        return new HousingResult<Guid>.Success(optionId);
    }

    private static void Apply(HousingOption option, SaveHousingOptionRequest request)
    {
        option.Name = HousingRules.NormalizeName(request.Name);
        option.Area = HousingRules.NormalizeOptional(request.Area);
        option.Size = HousingRules.NormalizeOptional(request.Size);
        option.Notes = HousingRules.NormalizeOptional(request.Notes);
        option.Furnishing = request.Furnishing;
        option.AvailableOn = request.AvailableOn;
        option.Status = request.Status;
        var hasMoney = request.Rent is not null || request.Deposit is not null;
        option.Rent = request.Rent;
        option.Deposit = request.Deposit;
        option.CurrencyCode = hasMoney ? HousingRules.NormalizeCurrency(request.CurrencyCode) : null;
    }

    private static IReadOnlyList<HousingOptionResponse> Order(IReadOnlyList<HousingOption> options)
    {
        return options
            .OrderBy(Rank)
            .ThenBy(option => option.Name, StringComparer.Ordinal)
            .ThenBy(option => option.Id)
            .Select(HousingOptionResponse.From)
            .ToArray();
    }

    private static int Rank(HousingOption option)
    {
        return option.Status switch
        {
            HousingStatus.Chosen => 0,
            HousingStatus.Offered => 1,
            HousingStatus.Viewing => 2,
            HousingStatus.Considering => 3,
            HousingStatus.Dropped => 4,
            _ => 5,
        };
    }

    private async Task<bool> AnotherChosenAsync(
        Guid relocationId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        return await db.HousingOptions.AsNoTracking().AnyAsync(
            option => option.RelocationId == relocationId
                && option.Id != optionId
                && option.Status == HousingStatus.Chosen,
            cancellationToken);
    }

    private async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsOneChosen(exception))
        {
            return false;
        }
    }

    private static bool IsOneChosen(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && postgres.ConstraintName == "ux_housing_options_one_chosen";
    }

    private static Dictionary<string, string[]> AlreadyChosen()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["status"] = [HousingRules.AlreadyChosenMessage],
        };
    }

    private static Dictionary<string, string[]> InternationalOnly()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["relocation"] = [HousingRules.InternationalOnlyMessage],
        };
    }

    private async Task<HousingOption?> FindAsync(
        Guid relocationId,
        Guid optionId,
        CancellationToken cancellationToken)
    {
        return await db.HousingOptions.SingleOrDefaultAsync(
            option => option.Id == optionId && option.RelocationId == relocationId,
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
