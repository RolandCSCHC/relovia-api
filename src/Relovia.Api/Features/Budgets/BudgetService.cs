using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.ExchangeRates;
using Relovia.Api.Features.Inventory;

namespace Relovia.Api.Features.Budgets;

public sealed class BudgetService(ReloviaDbContext db, ExchangeRateService exchangeRates)
{
    public const string RelocationNotFoundTitle = "Relocation was not found.";

    public const string CategoryNotFoundTitle = "Category was not found.";

    public const string EstimateNotFoundTitle = "Estimate was not found.";

    public const string BudgetNotSetUpTitle = "Set the budget currency first.";

    public const string CategoryInUseTitle = "Move or delete this category's expenses and shopping items first.";

    public async Task<BudgetResult<BudgetResponse>> GetAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetResponse>.NotFound(RelocationNotFoundTitle);
        }

        return new BudgetResult<BudgetResponse>.Success(await BuildAsync(relocationId, cancellationToken));
    }

    /// <summary>
    /// Sets up the budget on first use, with the starter categories, or changes its currency. The
    /// currency is only the one totals are shown in, so changing it never alters an estimate or expense.
    /// </summary>
    public async Task<BudgetResult<BudgetResponse>> SaveAsync(
        Guid userId,
        Guid relocationId,
        SaveBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetResponse>.NotFound(RelocationNotFoundTitle);
        }

        var currencyCode = BudgetRules.NormalizeCurrencyCode(request.CurrencyCode);
        var budget = await db.Budgets.SingleOrDefaultAsync(
            candidate => candidate.RelocationId == relocationId,
            cancellationToken);
        if (budget is null)
        {
            budget = new Budget { RelocationId = relocationId, CurrencyCode = currencyCode };
            foreach (var name in StarterBudgetCategories.Names)
            {
                budget.Categories.Add(new BudgetCategory { RelocationId = relocationId, Name = name });
            }

            db.Budgets.Add(budget);
        }
        else
        {
            budget.CurrencyCode = currencyCode;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new BudgetResult<BudgetResponse>.Success(await BuildAsync(relocationId, cancellationToken));
    }

    /// <summary>
    /// Stores the monthly net income, or clears it when both fields are empty. The budget currency
    /// has to exist first. The amount and currency are kept as entered and are not an estimate.
    /// </summary>
    public async Task<BudgetResult<BudgetResponse>> SaveIncomeAsync(
        Guid userId,
        Guid relocationId,
        SaveMonthlyNetIncomeRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetResponse>.NotFound(RelocationNotFoundTitle);
        }

        var budget = await db.Budgets.SingleOrDefaultAsync(
            candidate => candidate.RelocationId == relocationId,
            cancellationToken);
        if (budget is null)
        {
            return new BudgetResult<BudgetResponse>.Conflict(BudgetNotSetUpTitle);
        }

        var currency = string.IsNullOrWhiteSpace(request.CurrencyCode)
            ? null
            : BudgetRules.NormalizeCurrencyCode(request.CurrencyCode);
        budget.MonthlyNetIncome = currency is null ? null : request.Amount;
        budget.MonthlyNetIncomeCurrency = currency;
        await db.SaveChangesAsync(cancellationToken);
        return new BudgetResult<BudgetResponse>.Success(await BuildAsync(relocationId, cancellationToken));
    }

    public async Task<BudgetResult<BudgetCategoryResponse>> CreateCategoryAsync(
        Guid userId,
        Guid relocationId,
        SaveBudgetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetCategoryResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetCategoryResponse>.NotFound(RelocationNotFoundTitle);
        }

        if (!await db.Budgets.AnyAsync(budget => budget.RelocationId == relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetCategoryResponse>.Conflict(BudgetNotSetUpTitle);
        }

        var name = request.Name.Trim();
        if (await NameTakenAsync(relocationId, name, exceptId: null, cancellationToken))
        {
            return new BudgetResult<BudgetCategoryResponse>.Invalid(NameTakenErrors());
        }

        var category = new BudgetCategory { RelocationId = relocationId, Name = name };
        db.BudgetCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<BudgetCategoryResponse>.Success(
            await CategoryResponseAsync(relocationId, category.Id, cancellationToken));
    }

    public async Task<BudgetResult<BudgetCategoryResponse>> UpdateCategoryAsync(
        Guid userId,
        Guid relocationId,
        Guid categoryId,
        SaveBudgetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetCategoryResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetCategoryResponse>.NotFound(RelocationNotFoundTitle);
        }

        var category = await db.BudgetCategories.SingleOrDefaultAsync(
            candidate => candidate.Id == categoryId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (category is null)
        {
            return new BudgetResult<BudgetCategoryResponse>.NotFound(CategoryNotFoundTitle);
        }

        var name = request.Name.Trim();
        if (await NameTakenAsync(relocationId, name, category.Id, cancellationToken))
        {
            return new BudgetResult<BudgetCategoryResponse>.Invalid(NameTakenErrors());
        }

        category.Name = name;
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<BudgetCategoryResponse>.Success(
            await CategoryResponseAsync(relocationId, category.Id, cancellationToken));
    }

    public async Task<BudgetResult<Guid>> DeleteCategoryAsync(
        Guid userId,
        Guid relocationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var category = await db.BudgetCategories.SingleOrDefaultAsync(
            candidate => candidate.Id == categoryId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (category is null)
        {
            return new BudgetResult<Guid>.NotFound(CategoryNotFoundTitle);
        }

        var hasExpenses = await db.Expenses.AnyAsync(
            expense => expense.RelocationId == relocationId && expense.CategoryId == category.Id,
            cancellationToken);
        var hasShopping = await db.InventoryItems.AnyAsync(
            item => item.RelocationId == relocationId && item.BudgetCategoryId == category.Id,
            cancellationToken);
        if (hasExpenses || hasShopping)
        {
            return new BudgetResult<Guid>.Conflict(CategoryInUseTitle);
        }

        // The category's estimates go with it (ON DELETE CASCADE).
        db.BudgetCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return new BudgetResult<Guid>.Success(category.Id);
    }

    public async Task<BudgetResult<BudgetEstimateResponse>> CreateEstimateAsync(
        Guid userId,
        Guid relocationId,
        SaveBudgetEstimateRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetEstimateResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetEstimateResponse>.NotFound(RelocationNotFoundTitle);
        }

        if (!await CategoryExistsAsync(relocationId, request.CategoryId, cancellationToken))
        {
            return new BudgetResult<BudgetEstimateResponse>.Invalid(CategoryErrors());
        }

        var estimate = new BudgetEstimate
        {
            RelocationId = relocationId,
            CategoryId = request.CategoryId,
            Label = request.Label.Trim(),
            Amount = request.Amount,
            CurrencyCode = BudgetRules.NormalizeCurrencyCode(request.CurrencyCode),
        };
        db.BudgetEstimates.Add(estimate);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<BudgetEstimateResponse>.Success(
            await EstimateResponseAsync(relocationId, estimate, cancellationToken));
    }

    public async Task<BudgetResult<BudgetEstimateResponse>> UpdateEstimateAsync(
        Guid userId,
        Guid relocationId,
        Guid estimateId,
        SaveBudgetEstimateRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<BudgetEstimateResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<BudgetEstimateResponse>.NotFound(RelocationNotFoundTitle);
        }

        var estimate = await db.BudgetEstimates.SingleOrDefaultAsync(
            candidate => candidate.Id == estimateId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (estimate is null)
        {
            return new BudgetResult<BudgetEstimateResponse>.NotFound(EstimateNotFoundTitle);
        }

        if (!await CategoryExistsAsync(relocationId, request.CategoryId, cancellationToken))
        {
            return new BudgetResult<BudgetEstimateResponse>.Invalid(CategoryErrors());
        }

        estimate.CategoryId = request.CategoryId;
        estimate.Label = request.Label.Trim();
        estimate.Amount = request.Amount;
        estimate.CurrencyCode = BudgetRules.NormalizeCurrencyCode(request.CurrencyCode);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<BudgetEstimateResponse>.Success(
            await EstimateResponseAsync(relocationId, estimate, cancellationToken));
    }

    public async Task<BudgetResult<Guid>> DeleteEstimateAsync(
        Guid userId,
        Guid relocationId,
        Guid estimateId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<Guid>.NotFound(RelocationNotFoundTitle);
        }

        var estimate = await db.BudgetEstimates.SingleOrDefaultAsync(
            candidate => candidate.Id == estimateId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (estimate is null)
        {
            return new BudgetResult<Guid>.NotFound(EstimateNotFoundTitle);
        }

        db.BudgetEstimates.Remove(estimate);
        await db.SaveChangesAsync(cancellationToken);
        return new BudgetResult<Guid>.Success(estimate.Id);
    }

    private async Task<bool> RelocationExistsAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        return await db.Relocations.AnyAsync(
            relocation => relocation.Id == relocationId && relocation.UserId == userId,
            cancellationToken);
    }

    private async Task<bool> CategoryExistsAsync(
        Guid relocationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await db.BudgetCategories.AnyAsync(
            category => category.Id == categoryId && category.RelocationId == relocationId,
            cancellationToken);
    }

    private async Task<bool> NameTakenAsync(
        Guid relocationId,
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var lowered = name.ToLowerInvariant();
        return await db.BudgetCategories.AnyAsync(
            category => category.RelocationId == relocationId
                && category.Id != exceptId
                && category.Name.ToLower() == lowered,
            cancellationToken);
    }

    private async Task<BudgetResponse> BuildAsync(Guid relocationId, CancellationToken cancellationToken)
    {
        var stored = await db.Budgets.AsNoTracking()
            .Where(budget => budget.RelocationId == relocationId)
            .Select(budget => new
            {
                budget.CurrencyCode,
                budget.MonthlyNetIncome,
                budget.MonthlyNetIncomeCurrency,
            })
            .SingleOrDefaultAsync(cancellationToken);
        var currencyCode = stored?.CurrencyCode;
        var categories = await db.BudgetCategories.AsNoTracking()
            .Include(category => category.Estimates)
            .Where(category => category.RelocationId == relocationId)
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.RelocationId == relocationId)
            .ToListAsync(cancellationToken);
        var shopping = await db.InventoryItems.AsNoTracking()
            .Where(item => item.RelocationId == relocationId)
            .ToListAsync(cancellationToken);

        // Rates are only requested when something is in another currency.
        ExchangeRateTable? rates = null;
        if (currencyCode is not null
            && BudgetComparison.ForeignCurrencies(currencyCode, categories, expenses, shopping).Count > 0)
        {
            rates = await exchangeRates.GetAsync(currencyCode, cancellationToken);
        }

        return BudgetComparison.Build(
            currencyCode,
            categories,
            expenses,
            rates,
            shopping,
            stored?.MonthlyNetIncome,
            stored?.MonthlyNetIncomeCurrency);
    }

    private async Task<BudgetCategoryResponse> CategoryResponseAsync(
        Guid relocationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var budget = await BuildAsync(relocationId, cancellationToken);
        return budget.Categories.Single(category => category.Id == categoryId);
    }

    private async Task<BudgetEstimateResponse> EstimateResponseAsync(
        Guid relocationId,
        BudgetEstimate estimate,
        CancellationToken cancellationToken)
    {
        var budget = await BuildAsync(relocationId, cancellationToken);
        return budget.Categories
            .SelectMany(category => category.Estimates)
            .Single(candidate => candidate.Id == estimate.Id);
    }

    private static Dictionary<string, string[]> NameTakenErrors()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["name"] = ["A category with this name already exists."],
        };
    }

    private static Dictionary<string, string[]> CategoryErrors()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["categoryId"] = ["Choose a category from this budget."],
        };
    }
}
