using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/expenses")]
public sealed class ExpensesController(ExpenseService expenses) : BudgetControllerBase
{
    [HttpGet]
    [EndpointName("listExpenses")]
    [EndpointSummary("Lists the relocation's expenses, newest first, in the currency they were paid in.")]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> List(
        Guid relocationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await expenses.ListAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPost]
    [EndpointName("createExpense")]
    [EndpointSummary("Records an expense with its original amount and currency.")]
    public async Task<ActionResult<ExpenseResponse>> Create(
        Guid relocationId,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await expenses.CreateAsync(userId, relocationId, request, cancellationToken),
            expense => Created($"/api/relocations/{relocationId}/expenses/{expense.Id}", expense));
    }

    [HttpPut("{expenseId:guid}")]
    [EndpointName("updateExpense")]
    [EndpointSummary("Updates an expense.")]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid relocationId,
        Guid expenseId,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await expenses.UpdateAsync(userId, relocationId, expenseId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("{expenseId:guid}")]
    [EndpointName("deleteExpense")]
    [EndpointSummary("Deletes an expense.")]
    public async Task<IActionResult> Delete(
        Guid relocationId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await expenses.DeleteAsync(userId, relocationId, expenseId, cancellationToken),
            _ => NoContent());
    }
}
