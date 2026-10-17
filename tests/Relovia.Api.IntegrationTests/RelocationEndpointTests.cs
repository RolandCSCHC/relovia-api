using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relovia.Api.Common.Persistence;
using Relovia.Api.Common.Security;
using Relovia.Api.Features.Access;
using Relovia.Api.Features.ExchangeRates;
using Relovia.Api.Features.Identity;
using Relovia.Api.Features.Places;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.IntegrationTests;

public sealed partial class RelocationEndpointTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PostgresFixture _postgres;
    private ReloviaApiFactory _factory = null!;
    private string? _citiesDirectory;

    public RelocationEndpointTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public Task InitializeAsync()
    {
        _citiesDirectory = Path.Combine(Path.GetTempPath(), "relovia-cities-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_citiesDirectory);
        var citiesFile = Path.Combine(_citiesDirectory, "cities.txt");
        var admin1File = Path.Combine(_citiesDirectory, "admin1.txt");
        File.WriteAllText(
            citiesFile,
            "1\tBerlin\tBerlin\t\t0\t0\tP\tPPL\tDE\t\tBE\n");
        File.WriteAllText(admin1File, "DE.BE\tBerlin\n");
        _factory = new ReloviaApiFactory(_postgres.ConnectionString, citiesFile, admin1File);
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        Assert.Equal(_postgres.ConnectionString, configuration.GetConnectionString("Relovia"));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        if (_citiesDirectory is not null)
        {
            Directory.Delete(_citiesDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Anonymous_request_is_unauthorized()
    {
        using var client = CreateClient().Client;

        var response = await client.GetAsync("/api/relocations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_user_manages_only_their_relocations()
    {
        var owner = await SignInAsync("owner-crud@example.com");
        var other = await SignInAsync("other-crud@example.com");
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();

        var created = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Later", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "DE"));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.EndsWith($"/api/relocations/{created.Body!.Id}", created.Response.Headers.Location?.AbsolutePath);
        Assert.Equal("Later", created.Body.Name);
        Assert.Equal("US", created.Body.OriginCountryCode);
        Assert.Equal("DE", created.Body.DestinationCountryCode);
        Assert.True(created.Body.IsInternational);
        Assert.Equal(RelocationPurpose.Work, created.Body.Purpose);
        Assert.Equal(RelocationStatus.Active, created.Body.Status);

        var earlier = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Earlier", targetDate: new DateOnly(2026, 3, 1), destinationCountry: "US"));
        Assert.Equal(HttpStatusCode.Created, earlier.Status);
        Assert.False(earlier.Body!.IsInternational);

        var listed = await SendAsync<List<RelocationResponse>>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            "/api/relocations",
            body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal([earlier.Body.Id, created.Body.Id], listed.Body!.Select(relocation => relocation.Id));

        var hidden = await SendAsync<RelocationResponse>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            $"/api/relocations/{created.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);

        var otherList = await SendAsync<List<RelocationResponse>>(
            otherClient,
            other.Token,
            HttpMethod.Get,
            "/api/relocations",
            body: null);
        Assert.Equal(HttpStatusCode.OK, otherList.Status);
        Assert.Empty(otherList.Body!);

        var updated = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Put,
            $"/api/relocations/{created.Body.Id}",
            Request(name: "Updated", targetDate: new DateOnly(2026, 12, 1), destinationCountry: "US", status: "Draft"));
        Assert.Equal(HttpStatusCode.OK, updated.Status);
        Assert.Equal("Updated", updated.Body!.Name);
        Assert.Equal(RelocationStatus.Draft, updated.Body.Status);
        Assert.False(updated.Body.IsInternational);

        var rejectedDelete = await SendAsync<RelocationResponse>(
            otherClient,
            other.Token,
            HttpMethod.Delete,
            $"/api/relocations/{created.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, rejectedDelete.Status);

        var deleted = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Delete,
            $"/api/relocations/{created.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Status);

        var missing = await SendAsync<RelocationResponse>(
            ownerClient,
            owner.Token,
            HttpMethod.Get,
            $"/api/relocations/{created.Body.Id}",
            body: null);
        Assert.Equal(HttpStatusCode.NotFound, missing.Status);
    }

    [Fact]
    public async Task A_move_can_leave_the_destination_city_and_target_date_unset()
    {
        var owner = await SignInAsync("unknown-date@example.com");
        using var client = CreateClient();

        var created = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Offer pending", targetDate: null, destinationCountry: "DE", destinationCity: "  "));

        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.Equal(string.Empty, created.Body!.DestinationCity);
        Assert.Null(created.Body.TargetDate);
    }

    [Fact]
    public async Task Invalid_purpose_status_and_blank_name_are_rejected()
    {
        var owner = await SignInAsync("owner-invalid@example.com");
        using var client = CreateClient();

        var badPurpose = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "Move", targetDate: new DateOnly(2026, 8, 1), destinationCountry: "DE", purpose: "Nope"));
        Assert.Equal(HttpStatusCode.BadRequest, badPurpose.Status);

        var numericStatus = await SendAsync<RelocationResponse>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            new
            {
                name = "Move",
                originCountryCode = "US",
                originCity = "Austin",
                destinationCountryCode = "DE",
                destinationCity = "Berlin",
                targetDate = "2026-08-01",
                purpose = "Work",
                status = 1,
            });
        Assert.Equal(HttpStatusCode.BadRequest, numericStatus.Status);

        var blankName = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Post,
            "/api/relocations",
            Request(name: "   ", targetDate: new DateOnly(2026, 8, 1), destinationCountry: "DE"));
        Assert.Equal(HttpStatusCode.BadRequest, blankName.Status);
        Assert.Equal("Name is required.", blankName.Body!.GetProperty("errors").GetProperty("name")[0].GetString());

        await using var db = _postgres.CreateContext();
        var stored = await db.Relocations.CountAsync(relocation => relocation.UserId == owner.UserId);
        Assert.Equal(0, stored);
    }

    [Fact]
    public async Task City_search_requires_a_session_and_a_country_code()
    {
        var anonymous = await CreateClient().Client.GetAsync("/api/places/cities?country=DE&q=Ber");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var owner = await SignInAsync("cities@example.com");
        using var client = CreateClient();
        var missing = await SendAsync<JsonElement>(
            client,
            owner.Token,
            HttpMethod.Get,
            "/api/places/cities?country=D&q=Ber",
            body: null);
        Assert.Equal(HttpStatusCode.BadRequest, missing.Status);

        var listed = await SendAsync<CitySearchResponse>(
            client,
            owner.Token,
            HttpMethod.Get,
            "/api/places/cities?country=DE",
            body: null);
        Assert.Equal(HttpStatusCode.OK, listed.Status);
        Assert.Equal(["Berlin"], listed.Body!.Cities);
    }

    private ClientScope CreateClient()
    {
        var cookies = new CookieJar();
        return new ClientScope(_factory.CreateDefaultClient(cookies), cookies);
    }

    private async Task<(Guid UserId, string Token)> SignInAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReloviaDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<IAuthSessionStore>();
        var user = new User
        {
            Email = email,
            DisplayName = email,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var issued = await sessions.IssueAsync(user.Id, CancellationToken.None);
        return (user.Id, issued.Token);
    }

    private static async Task<ApiResult<T>> SendAsync<T>(
        ClientScope scope,
        string token,
        HttpMethod method,
        string path,
        object? body)
    {
        scope.Cookies.Add(SessionAuthenticationDefaults.CookieName, token);
        if (method != HttpMethod.Get && method != HttpMethod.Head)
        {
            await EnsureAntiforgeryAsync(scope);
        }

        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var xsrf = scope.Cookies.Read(AntiforgeryCookies.RequestTokenCookieName);
        if (xsrf is not null)
        {
            request.Headers.TryAddWithoutValidation(AntiforgeryCookies.HeaderName, xsrf);
        }

        var response = await scope.Client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new ApiResult<T>(response, response.StatusCode, default);
        }

        if (!response.IsSuccessStatusCode && typeof(T) != typeof(JsonElement))
        {
            return new ApiResult<T>(response, response.StatusCode, default);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(Json);
        return new ApiResult<T>(response, response.StatusCode, payload);
    }

    private static async Task EnsureAntiforgeryAsync(ClientScope scope)
    {
        if (scope.Cookies.Read(AntiforgeryCookies.RequestTokenCookieName) is not null)
        {
            return;
        }

        var warmup = await scope.Client.GetAsync("/api/relocations");
        warmup.EnsureSuccessStatusCode();
    }

    private static object Request(
        string name,
        DateOnly? targetDate,
        string destinationCountry,
        string destinationCity = "Berlin",
        string purpose = "Work",
        string status = "Active")
    {
        return new
        {
            name,
            originCountryCode = "us",
            originCity = " Austin ",
            destinationCountryCode = destinationCountry,
            destinationCity,
            targetDate,
            purpose,
            status,
        };
    }

    private sealed class ReloviaApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly string _citiesFile;
        private readonly string _admin1File;

        private readonly bool _restricted;

        public ReloviaApiFactory(
            string connectionString,
            string citiesFile,
            string admin1File,
            bool restricted = false)
        {
            _connectionString = connectionString;
            _citiesFile = citiesFile;
            _admin1File = admin1File;
            _restricted = restricted;
            // Environment variables override appsettings, including the local Development database.
            Environment.SetEnvironmentVariable("ConnectionStrings__Relovia", connectionString);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Relovia", _connectionString);
            builder.UseSetting(GeoNamesCityListSource.CitiesFileKey, _citiesFile);
            builder.UseSetting(GeoNamesCityListSource.Admin1FileKey, _admin1File);
            // Most tests sign in as arbitrary example.com users, so they run with the app open.
            builder.UseSetting("Access:Restricted", _restricted ? "true" : "false");
            builder.UseSetting("Access:AllowedEmails:0", AllowedEmail);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IExchangeRateProvider>();
                services.AddScoped<IExchangeRateProvider, FakeRateProvider>();
                services.RemoveAll<IInterestNotifier>();
                services.AddSingleton<RecordingInterestNotifier>();
                services.AddSingleton<IInterestNotifier>(provider => provider.GetRequiredService<RecordingInterestNotifier>());
            });
        }
    }

    private sealed class ClientScope(HttpClient client, CookieJar cookies) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public CookieJar Cookies { get; } = cookies;

        public void Dispose()
        {
            Client.Dispose();
        }
    }

    private sealed record ApiResult<T>(HttpResponseMessage Response, HttpStatusCode Status, T? Body);

    private sealed class CookieJar : DelegatingHandler
    {
        private readonly CookieContainer _cookies = new();

        public void Add(string name, string value)
        {
            var uri = new Uri("http://localhost");
            var existing = _cookies.GetCookies(uri)[name];
            if (existing is not null)
            {
                existing.Value = value;
                return;
            }

            _cookies.Add(uri, new Cookie(name, value, "/"));
        }

        public string? Read(string name)
        {
            return _cookies.GetCookies(new Uri("http://localhost"))[name]?.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? new Uri("http://localhost");
            var header = _cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(header))
            {
                request.Headers.Remove("Cookie");
                request.Headers.TryAddWithoutValidation("Cookie", header);
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var setCookie in setCookies)
                {
                    Store(setCookie);
                }
            }

            return response;
        }

        private void Store(string setCookie)
        {
            var pair = setCookie.Split(';', 2)[0];
            var split = pair.Split('=', 2);
            if (split.Length != 2)
            {
                return;
            }

            var raw = split[1].Trim();
            var value = raw.Contains('%', StringComparison.Ordinal) ? Uri.UnescapeDataString(raw) : raw;
            Add(split[0].Trim(), value);
        }
    }
}
