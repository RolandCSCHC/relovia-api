using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.ExchangeRates;

namespace Relovia.Api.Features.Budgets;

public sealed class ExpenseService(ReloviaDbContext db, ExchangeRateService exchangeRates)
{
    public const string ExpenseNotFoundTitle = "Expense was not found.";

    public async Task<BudgetResult<IReadOnlyList<ExpenseResponse>>> ListAsync(
        Guid userId,
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<IReadOnlyList<ExpenseResponse>>.NotFound(BudgetService.RelocationNotFoundTitle);
        }

        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.RelocationId == relocationId)
            .OrderByDescending(expense => expense.PaidOn)
            .ThenByDescending(expense => expense.Id)
            .ToListAsync(cancellationToken);

        return new BudgetResult<IReadOnlyList<ExpenseResponse>>.Success(
            await ToResponsesAsync(relocationId, expenses, cancellationToken));
    }

    public async Task<BudgetResult<ExpenseResponse>> CreateAsync(
        Guid userId,
        Guid relocationId,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<ExpenseResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<ExpenseResponse>.NotFound(BudgetService.RelocationNotFoundTitle);
        }

        var links = await LinkErrorsAsync(relocationId, request, cancellationToken);
        if (links.Count > 0)
        {
            return new BudgetResult<ExpenseResponse>.Invalid(links);
        }

        var expense = new Expense { RelocationId = relocationId };
        Apply(expense, request);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<ExpenseResponse>.Success(
            (await ToResponsesAsync(relocationId, [expense], cancellationToken))[0]);
    }

    public async Task<BudgetResult<ExpenseResponse>> UpdateAsync(
        Guid userId,
        Guid relocationId,
        Guid expenseId,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var errors = BudgetRules.Validate(request);
        if (errors.Count > 0)
        {
            return new BudgetResult<ExpenseResponse>.Invalid(errors);
        }

        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<ExpenseResponse>.NotFound(BudgetService.RelocationNotFoundTitle);
        }

        var expense = await db.Expenses.SingleOrDefaultAsync(
            candidate => candidate.Id == expenseId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (expense is null)
        {
            return new BudgetResult<ExpenseResponse>.NotFound(ExpenseNotFoundTitle);
        }

        var links = await LinkErrorsAsync(relocationId, request, cancellationToken);
        if (links.Count > 0)
        {
            return new BudgetResult<ExpenseResponse>.Invalid(links);
        }

        Apply(expense, request);
        await db.SaveChangesAsync(cancellationToken);

        return new BudgetResult<ExpenseResponse>.Success(
            (await ToResponsesAsync(relocationId, [expense], cancellationToken))[0]);
    }

    public async Task<BudgetResult<Guid>> DeleteAsync(
        Guid userId,
        Guid relocationId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        if (!await RelocationExistsAsync(userId, relocationId, cancellationToken))
        {
            return new BudgetResult<Guid>.NotFound(BudgetService.RelocationNotFoundTitle);
        }

        var expense = await db.Expenses.SingleOrDefaultAsync(
            candidate => candidate.Id == expenseId && candidate.RelocationId == relocationId,
            cancellationToken);
        if (expense is null)
        {
            return new BudgetResult<Guid>.NotFound(ExpenseNotFoundTitle);
        }

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(cancellationToken);
        return new BudgetResult<Guid>.Success(expense.Id);
    }

    /// <summary>Adds each expense's amount in the budget currency, converted at the latest rate.</summary>
    private async Task<ExpenseResponse[]> ToResponsesAsync(
        Guid relocationId,
        IReadOnlyList<Expense> expenses,
        CancellationToken cancellationToken)
    {
        var budgetCurrency = await db.Budgets.AsNoTracking()
            .Where(budget => budget.RelocationId == relocationId)
            .Select(budget => budget.CurrencyCode)
            .SingleOrDefaultAsync(cancellationToken);
        if (budgetCurrency is null)
        {
            return expenses.Select(expense => ExpenseResponse.From(expense, null)).ToArray();
        }

        ExchangeRateTable? rates = null;
        if (expenses.Any(expense => expense.CurrencyCode != budgetCurrency))
        {
            rates = await exchangeRates.GetAsync(budgetCurrency, cancellationToken);
        }

        return expenses
            .Select(expense => ExpenseResponse.From(
                expense,
                BudgetComparison.Convert(expense.Amount, expense.CurrencyCode, budgetCurrency, rates)))
            .ToArray();
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

    /// <summary>The category and the optional task must belong to the same relocation as the expense.</summary>
    private async Task<Dictionary<string, string[]>> LinkErrorsAsync(
        Guid relocationId,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var categoryExists = await db.BudgetCategories.AnyAsync(
            category => category.Id == request.CategoryId && category.RelocationId == relocationId,
            cancellationToken);
        if (!categoryExists)
        {
            errors["categoryId"] = ["Choose a category from this budget."];
        }

        if (request.TaskId is Guid taskId)
        {
            var taskExists = await db.RelocationTasks.AnyAsync(
                task => task.Id == taskId && task.RelocationId == relocationId,
                cancellationToken);
            if (!taskExists)
            {
                errors["taskId"] = ["Choose a task from this relocation."];
            }
        }

        return errors;
    }

    private static void Apply(Expense expense, SaveExpenseRequest request)
    {
        expense.CategoryId = request.CategoryId;
        expense.TaskId = request.TaskId;
        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.CurrencyCode = BudgetRules.NormalizeCurrencyCode(request.CurrencyCode);
        expense.PaidOn = request.PaidOn;
        expense.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
    }
}
