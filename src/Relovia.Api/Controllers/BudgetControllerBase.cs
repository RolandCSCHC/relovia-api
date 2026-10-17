using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Common.Errors;
using Relovia.Api.Features.Budgets;

namespace Relovia.Api.Controllers;

/// <summary>Shared by the budget and expense controllers: the signed-in user and the result mapping.</summary>
public abstract class BudgetControllerBase : ControllerBase
{
    protected bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    protected ActionResult Map<T>(BudgetResult<T> result, Func<T, ActionResult> success)
    {
        return result switch
        {
            BudgetResult<T>.Success ok => success(ok.Value),
            BudgetResult<T>.Invalid invalid => ApiValidation.Invalid(HttpContext, invalid.Errors),
            BudgetResult<T>.NotFound missing => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: missing.Title),
            BudgetResult<T>.Conflict conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: conflict.Title),
            _ => throw new InvalidOperationException("Unexpected budget result."),
        };
    }
}
