using Microsoft.AspNetCore.Mvc;

namespace Relovia.Api.Common.Errors;

public static class ApiValidation
{
    public static IActionResult InvalidModelState(ActionContext context)
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    public static ActionResult Invalid(HttpContext httpContext, IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>(errors))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" },
        };
    }
}
