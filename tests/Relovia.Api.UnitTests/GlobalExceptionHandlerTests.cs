using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Relovia.Api.Common.Errors;

namespace Relovia.Api.UnitTests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Production_response_hides_the_exception_message()
    {
        var body = await HandleAsync(Environments.Production, "database password=secret");

        Assert.Equal(StatusCodes.Status500InternalServerError, body.StatusCode);
        Assert.Equal("application/problem+json", body.ContentType);
        Assert.DoesNotContain("database password=secret", body.Json);
        Assert.Contains("traceId", body.Json);
    }

    [Fact]
    public async Task Development_response_includes_the_exception_message()
    {
        var body = await HandleAsync(Environments.Development, "missing column");

        Assert.Contains("missing column", body.Json);
    }

    private static async Task<HandledResponse> HandleAsync(string environmentName, string message)
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new TestEnvironment(environmentName));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/example";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException(message),
            CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return new HandledResponse(context.Response.StatusCode, context.Response.ContentType, json);
    }

    private sealed record HandledResponse(int StatusCode, string? ContentType, string Json);

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Relovia.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
