using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relovia.Api.Common.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext httpContext, HealthReport report)
    {
        httpContext.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        };

        return httpContext.Response.WriteAsJsonAsync(body, JsonSerializerOptions.Web);
    }
}
