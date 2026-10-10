using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Relovia.Api.Common.Errors;

namespace Relovia.Api.UnitTests;

public sealed class ApiValidationTests
{
    [Fact]
    public void Invalid_model_state_returns_a_validation_problem()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1"
        };
        httpContext.Request.Path = "/api/users";
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        actionContext.ModelState.AddModelError("email", "Email is required.");

        var result = Assert.IsType<BadRequestObjectResult>(ApiValidation.InvalidModelState(actionContext));
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("/api/users", problem.Instance);
        Assert.Equal("trace-1", problem.Extensions["traceId"]);
        Assert.Equal("Email is required.", Assert.Single(problem.Errors["email"]));
        Assert.Contains("application/problem+json", result.ContentTypes);
    }
}
