using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relovia.Api.Features.ExchangeRates;

namespace Relovia.Api.UnitTests;

public sealed class ExchangeRateTests
{
    private static readonly DateTimeOffset Updated = new(2026, 10, 17, 0, 2, 32, TimeSpan.Zero);

    [Fact]
    public void Converting_into_the_base_divides_by_the_rate()
    {
        var table = Table();

        Assert.True(table.TryConvert(100, "NZD", out var fromNzd));
        Assert.Equal(50_000m, fromNzd);
        Assert.True(table.TryConvert(1234.5m, "CLP", out var same));
        Assert.Equal(1234.5m, same);
    }

    [Fact]
    public void A_currency_without_a_rate_does_not_convert()
    {
        Assert.False(Table().TryConvert(10, "XTS", out var converted));
        Assert.Equal(0m, converted);
        Assert.False(new ExchangeRateTable(
            "CLP",
            new Dictionary<string, decimal> { ["NZD"] = 0m },
            Updated).TryConvert(10, "NZD", out _));
    }

    [Fact]
    public void A_converted_amount_is_rounded_to_four_places()
    {
        var table = new ExchangeRateTable("USD", new Dictionary<string, decimal> { ["EUR"] = 3m }, Updated);

        Assert.True(table.TryConvert(10, "EUR", out var converted));
        Assert.Equal(3.3333m, converted);
    }

    [Fact]
    public void The_open_er_api_response_becomes_a_rate_table()
    {
        using var document = JsonDocument.Parse(
            """
            {"result":"success","time_last_update_unix":1792195352,"base_code":"NZD",
             "rates":{"NZD":1,"CLP":555.559581,"BAD":0,"WORDS":"x"}}
            """);

        var table = OpenErApiRateProvider.Parse(document.RootElement, "nzd");

        Assert.NotNull(table);
        Assert.Equal("NZD", table.BaseCode);
        Assert.Equal(555.559581m, table.Rates["CLP"]);
        Assert.False(table.Rates.ContainsKey("BAD"));
        Assert.False(table.Rates.ContainsKey("WORDS"));
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1792195352), table.UpdatedAt);
    }

    [Fact]
    public void An_error_response_is_not_a_rate_table()
    {
        using var document = JsonDocument.Parse("""{"result":"error","error-type":"unsupported-code"}""");

        Assert.Null(OpenErApiRateProvider.Parse(document.RootElement, "ZZZ"));
    }

    [Fact]
    public async Task The_provider_asks_for_the_base_currency_and_reads_the_answer()
    {
        var handler = new StubHandler(_ => Json("""{"result":"success","rates":{"CLP":2}}"""));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://rates.example/") };

        var table = await new OpenErApiRateProvider(http).GetLatestAsync("NZD", CancellationToken.None);

        Assert.Equal("https://rates.example/v6/latest/NZD", handler.Requests.Single()?.ToString());
        Assert.Equal(2m, table!.Rates["CLP"]);
    }

    [Fact]
    public async Task Rates_are_fetched_once_while_they_are_fresh()
    {
        var provider = new CountingProvider(Table());
        var clock = new ManualClock();
        var service = Service(provider, clock);

        Assert.Same(provider.Table, await service.GetAsync("CLP", CancellationToken.None));
        clock.Advance(ExchangeRateService.FreshFor - TimeSpan.FromMinutes(1));
        Assert.Same(provider.Table, await service.GetAsync("CLP", CancellationToken.None));

        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Stale_rates_are_refreshed()
    {
        var provider = new CountingProvider(Table());
        var clock = new ManualClock();
        var service = Service(provider, clock);

        await service.GetAsync("CLP", CancellationToken.None);
        clock.Advance(ExchangeRateService.FreshFor + TimeSpan.FromMinutes(1));
        await service.GetAsync("CLP", CancellationToken.None);

        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task When_a_refresh_fails_the_last_rates_are_served()
    {
        var provider = new CountingProvider(Table());
        var clock = new ManualClock();
        var service = Service(provider, clock);
        await service.GetAsync("CLP", CancellationToken.None);

        provider.Failure = new HttpRequestException("down");
        clock.Advance(ExchangeRateService.FreshFor + TimeSpan.FromHours(1));
        var table = await service.GetAsync("CLP", CancellationToken.None);

        Assert.Same(provider.Table, table);
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task With_nothing_cached_a_failure_means_no_rates()
    {
        var provider = new CountingProvider(Table()) { Failure = new HttpRequestException("down") };
        var service = Service(provider, new ManualClock());

        Assert.Null(await service.GetAsync("CLP", CancellationToken.None));
    }

    [Fact]
    public async Task An_unsupported_currency_means_no_rates()
    {
        var provider = new CountingProvider(null);
        var service = Service(provider, new ManualClock());

        Assert.Null(await service.GetAsync("ZZZ", CancellationToken.None));
    }

    private static ExchangeRateTable Table()
    {
        return new ExchangeRateTable(
            "CLP",
            new Dictionary<string, decimal> { ["CLP"] = 1m, ["NZD"] = 0.002m },
            Updated);
    }

    private static ExchangeRateService Service(IExchangeRateProvider provider, TimeProvider clock)
    {
        return new ExchangeRateService(
            provider,
            new MemoryCache(new MemoryCacheOptions()),
            clock,
            NullLogger<ExchangeRateService>.Instance);
    }

    private static HttpResponseMessage Json(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private sealed class CountingProvider(ExchangeRateTable? table) : IExchangeRateProvider
    {
        public ExchangeRateTable? Table { get; } = table;

        public Exception? Failure { get; set; }

        public int Calls { get; private set; }

        public Task<ExchangeRateTable?> GetLatestAsync(string baseCode, CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is null ? Task.FromResult(Table) : Task.FromException<ExchangeRateTable?>(Failure);
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);
            return Task.FromResult(respond(request));
        }
    }
}
