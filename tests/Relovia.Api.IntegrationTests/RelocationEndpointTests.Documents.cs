using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Relovia.Api.Common.Security;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests
{
    private static readonly byte[] VisaPdf = "%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n"u8.ToArray();

    [Fact]
    public async Task A_document_can_be_downloaded_only_by_the_owner()
    {
        var owner = await SignInAsync("documents-owner@example.com");
        var other = await SignInAsync("documents-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Papers", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        var path = $"/api/relocations/{relocation.Body!.Id}/documents";

        var anonymous = await CreateClient().Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var created = await UploadAsync<JsonElement>(
            ownerClient,
            owner.Token,
            path,
            VisaPdf,
            "scans/visa.pdf",
            "application/octet-stream");
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var document = created.Body;
        var documentId = document.GetProperty("id").GetGuid();
        Assert.Contains(
            $"/documents/{documentId}/content",
            created.Response.Headers.Location?.OriginalString,
            StringComparison.Ordinal);
        Assert.Equal("visa.pdf", document.GetProperty("fileName").GetString());
        Assert.Equal("application/pdf", document.GetProperty("contentType").GetString());
        Assert.Equal(VisaPdf.LongLength, document.GetProperty("sizeBytes").GetInt64());
        Assert.False(document.TryGetProperty("storageKey", out _));

        var stored = Path.Combine(_factory.StorageRoot, documentId.ToString("N"));
        Assert.True(File.Exists(stored));
        Assert.Equal(VisaPdf, await File.ReadAllBytesAsync(stored));

        var listed = await SendAsync<JsonElement>(ownerClient, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal("visa.pdf", listed.Body[0].GetProperty("fileName").GetString());
        Assert.False(listed.Body[0].TryGetProperty("storageKey", out _));

        var hiddenList = await SendAsync<JsonElement>(otherClient, other.Token, HttpMethod.Get, path, body: null);
        Assert.Equal(HttpStatusCode.NotFound, hiddenList.Status);

        var download = await DownloadAsync(ownerClient, owner.Token, $"{path}/{documentId}/content");
        Assert.Equal(HttpStatusCode.OK, download.Status);
        Assert.Equal(VisaPdf, download.Bytes);
        Assert.Equal("application/pdf", download.Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", download.Response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("visa.pdf", download.Response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("nosniff", download.Response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("no-store", download.Response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);

        var stolen = await DownloadAsync(otherClient, other.Token, $"{path}/{documentId}/content");
        Assert.Equal(HttpStatusCode.NotFound, stolen.Status);
        Assert.NotEqual(VisaPdf, stolen.Bytes);
        Assert.True(File.Exists(stored));

        var rejectedDelete = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Delete,
            $"{path}/{documentId}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, rejectedDelete.Status);
        Assert.True(File.Exists(stored));

        var deleted = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/{documentId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);
        Assert.False(File.Exists(stored));
    }

    [Fact]
    public async Task An_empty_or_unrecognized_upload_is_not_stored()
    {
        var owner = await SignInAsync("documents-invalid@example.com");
        using var client = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Papers", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        var path = $"/api/relocations/{relocation.Body!.Id}/documents";

        var empty = await UploadAsync<JsonElement>(client, owner.Token, path, [], "empty.pdf", "application/pdf");
        var html = await UploadAsync<JsonElement>(
            client,
            owner.Token,
            path,
            "<html></html>"u8.ToArray(),
            "notes.html",
            "text/html");

        Assert.Equal(HttpStatusCode.BadRequest, empty.Status);
        Assert.Equal(HttpStatusCode.BadRequest, html.Status);
        Assert.Empty(Directory.GetFiles(_factory.StorageRoot));
    }

    [Fact]
    public async Task Deleting_a_relocation_removes_its_stored_file()
    {
        var owner = await SignInAsync("documents-cascade@example.com");
        using var client = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Papers", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        var created = await UploadAsync<DocumentResponse>(
            client,
            owner.Token,
            $"/api/relocations/{relocation.Body!.Id}/documents",
            VisaPdf,
            "lease.pdf",
            "application/pdf");
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var stored = Path.Combine(_factory.StorageRoot, created.Body!.Id.ToString("N"));
        Assert.True(File.Exists(stored));

        var deleted = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{relocation.Body.Id}",
            body: null);

        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);
        Assert.False(File.Exists(stored));
    }

    [Fact]
    public async Task A_document_keeps_its_category_expiration_and_tasks_on_this_relocation()
    {
        var owner = await SignInAsync("documents-details-owner@example.com");
        var other = await SignInAsync("documents-details-other@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        var relocation = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Papers", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        var elsewhere = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Other move", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "FR"));
        var relocationId = relocation.Body!.Id;
        var path = $"/api/relocations/{relocationId}/documents";
        var tasksPath = $"/api/relocations/{relocationId}/tasks";
        var visaTask = await AddTaskAsync(ownerClient, owner.Token, tasksPath, "Apply for a visa");
        var leaseTask = await AddTaskAsync(ownerClient, owner.Token, tasksPath, "Sign the lease");
        var foreignTask = await AddTaskAsync(
            ownerClient,
            owner.Token,
            $"/api/relocations/{elsewhere.Body!.Id}/tasks",
            "Register an address");

        var created = await UploadAsync<JsonElement>(
            ownerClient,
            owner.Token,
            path,
            VisaPdf,
            "visa.pdf",
            "application/pdf");
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var documentId = created.Body.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, created.Body.GetProperty("category").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.Body.GetProperty("expiresOn").ValueKind);
        Assert.Empty(created.Body.GetProperty("taskIds").EnumerateArray());
        var stored = Path.Combine(_factory.StorageRoot, documentId.ToString("N"));

        var before = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            tasksPath,
            body: null);
        Assert.All(before.Body!.Tasks, task => Assert.Empty(task.DocumentIds));

        var updated = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{documentId}",
            new
            {
                category = "Visa",
                expiresOn = new DateOnly(2020, 1, 15),
                taskIds = new[] { visaTask, visaTask },
            });
        Assert.Equal(HttpStatusCode.OK, updated.Status);
        Assert.Equal("Visa", updated.Body.GetProperty("category").GetString());
        Assert.Equal("2020-01-15", updated.Body.GetProperty("expiresOn").GetString());
        Assert.Equal([visaTask], updated.Body.GetProperty("taskIds").EnumerateArray().Select(id => id.GetGuid()));
        Assert.Equal(VisaPdf, await File.ReadAllBytesAsync(stored));

        var listed = await SendAsync<JsonElement>(ownerClient, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal("Visa", listed.Body[0].GetProperty("category").GetString());
        var linked = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            tasksPath,
            body: null);
        Assert.Equal([documentId], linked.Body!.Tasks.Single(task => task.Id == visaTask).DocumentIds);
        Assert.Empty(linked.Body.Tasks.Single(task => task.Id == leaseTask).DocumentIds);

        var unknownCategory = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{documentId}",
            new { category = "Passport", expiresOn = (DateOnly?)null, taskIds = new[] { visaTask } });
        var foreignLink = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{documentId}",
            new { category = "Visa", expiresOn = new DateOnly(2020, 1, 15), taskIds = new[] { foreignTask } });
        var hidden = await SendAsync<JsonElement>(
            otherClient,
            other.Token,
            HttpMethod.Put,
            $"{path}/{documentId}",
            new { category = "Other", expiresOn = (DateOnly?)null, taskIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.Status);
        Assert.Equal(HttpStatusCode.BadRequest, foreignLink.Status);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);

        var unchanged = await SendAsync<JsonElement>(ownerClient, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Equal([visaTask], unchanged.Body[0].GetProperty("taskIds").EnumerateArray().Select(id => id.GetGuid()));
        Assert.Equal(VisaPdf, await File.ReadAllBytesAsync(stored));

        var replaced = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"{path}/{documentId}",
            new { category = (string?)null, expiresOn = (DateOnly?)null, taskIds = new[] { leaseTask } });
        Assert.Equal(HttpStatusCode.OK, replaced.Status);
        Assert.Equal(JsonValueKind.Null, replaced.Body.GetProperty("category").ValueKind);
        Assert.Equal(JsonValueKind.Null, replaced.Body.GetProperty("expiresOn").ValueKind);
        Assert.Equal([leaseTask], replaced.Body.GetProperty("taskIds").EnumerateArray().Select(id => id.GetGuid()));

        var deletedTask = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"{tasksPath}/{leaseTask}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deletedTask.Status);
        var afterTask = await SendAsync<JsonElement>(ownerClient, owner.Token, HttpMethod.Get, path, body: null);
        Assert.Empty(afterTask.Body[0].GetProperty("taskIds").EnumerateArray());
        Assert.True(File.Exists(stored));

        var deleted = await SendAsync<JsonElement>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"{path}/{documentId}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);
        Assert.False(File.Exists(stored));
        var tasks = await SendAsync<RelocationTasksResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            tasksPath,
            body: null);
        Assert.Empty(tasks.Body!.Tasks.Single(task => task.Id == visaTask).DocumentIds);
    }

    private static async Task<Guid> AddTaskAsync(ClientScope scope, string token, string path, string title)
    {
        var created = await SendAsync<RelocationTaskResponse>(
            scope,
            token,
            HttpMethod.Post,
            path,
            new { title, dueDate = (DateOnly?)null, phase = "Administration", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!.Id;
    }

    private async Task<ApiResult<T>> UploadAsync<T>(
        ClientScope scope,
        string token,
        string path,
        byte[] content,
        string fileName,
        string mediaType)
    {
        scope.Cookies.Add(SessionAuthenticationDefaults.CookieName, token);
        await EnsureAntiforgeryAsync(scope);

        using var body = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        body.Add(file, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = body };
        var xsrf = scope.Cookies.Read(AntiforgeryCookies.RequestTokenCookieName);
        if (xsrf is not null)
        {
            request.Headers.TryAddWithoutValidation(AntiforgeryCookies.HeaderName, xsrf);
        }

        var response = await scope.Client.SendAsync(request);
        if (!response.IsSuccessStatusCode && typeof(T) != typeof(JsonElement))
        {
            return new ApiResult<T>(response, response.StatusCode, default);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(Json);
        return new ApiResult<T>(response, response.StatusCode, payload);
    }

    private static async Task<DownloadResult> DownloadAsync(ClientScope scope, string token, string path)
    {
        scope.Cookies.Add(SessionAuthenticationDefaults.CookieName, token);
        var response = await scope.Client.GetAsync(path);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new DownloadResult(response, response.StatusCode, bytes);
    }

    private sealed record DownloadResult(HttpResponseMessage Response, HttpStatusCode Status, byte[] Bytes);
}
