using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/relocations/{relocationId:guid}/budget")]
public sealed class BudgetController(BudgetService budgets) : BudgetControllerBase
{
    [HttpGet]
    [EndpointName("getBudget")]
    [EndpointSummary("Returns the relocation's budget with estimates compared to actual expenses.")]
    public async Task<ActionResult<BudgetResponse>> Get(Guid relocationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await budgets.GetAsync(userId, relocationId, cancellationToken), Ok);
    }

    [HttpPut]
    [EndpointName("saveBudget")]
    [EndpointSummary("Sets up the budget with its currency, or changes the currency while it has no estimates.")]
    public async Task<ActionResult<BudgetResponse>> Save(
        Guid relocationId,
        SaveBudgetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await budgets.SaveAsync(userId, relocationId, request, cancellationToken), Ok);
    }

    [HttpPost("categories")]
    [EndpointName("createBudgetCategory")]
    [EndpointSummary("Adds a budget category.")]
    public async Task<ActionResult<BudgetCategoryResponse>> CreateCategory(
        Guid relocationId,
        SaveBudgetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.CreateCategoryAsync(userId, relocationId, request, cancellationToken),
            category => Created($"/api/relocations/{relocationId}/budget/categories/{category.Id}", category));
    }

    [HttpPut("categories/{categoryId:guid}")]
    [EndpointName("updateBudgetCategory")]
    [EndpointSummary("Renames a budget category.")]
    public async Task<ActionResult<BudgetCategoryResponse>> UpdateCategory(
        Guid relocationId,
        Guid categoryId,
        SaveBudgetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.UpdateCategoryAsync(userId, relocationId, categoryId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("categories/{categoryId:guid}")]
    [EndpointName("deleteBudgetCategory")]
    [EndpointSummary("Deletes a budget category and its estimates. Categories with expenses are refused.")]
    public async Task<IActionResult> DeleteCategory(
        Guid relocationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.DeleteCategoryAsync(userId, relocationId, categoryId, cancellationToken),
            _ => NoContent());
    }

    [HttpPost("estimates")]
    [EndpointName("createBudgetEstimate")]
    [EndpointSummary("Adds an estimate in the budget currency.")]
    public async Task<ActionResult<BudgetEstimateResponse>> CreateEstimate(
        Guid relocationId,
        SaveBudgetEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.CreateEstimateAsync(userId, relocationId, request, cancellationToken),
            estimate => Created($"/api/relocations/{relocationId}/budget/estimates/{estimate.Id}", estimate));
    }

    [HttpPut("estimates/{estimateId:guid}")]
    [EndpointName("updateBudgetEstimate")]
    [EndpointSummary("Updates an estimate.")]
    public async Task<ActionResult<BudgetEstimateResponse>> UpdateEstimate(
        Guid relocationId,
        Guid estimateId,
        SaveBudgetEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.UpdateEstimateAsync(userId, relocationId, estimateId, request, cancellationToken),
            Ok);
    }

    [HttpDelete("estimates/{estimateId:guid}")]
    [EndpointName("deleteBudgetEstimate")]
    [EndpointSummary("Deletes an estimate.")]
    public async Task<IActionResult> DeleteEstimate(
        Guid relocationId,
        Guid estimateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Map(
            await budgets.DeleteEstimateAsync(userId, relocationId, estimateId, cancellationToken),
            _ => NoContent());
    }
}
