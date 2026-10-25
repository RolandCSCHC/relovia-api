using Relovia.Api.Features.Export;

namespace Relovia.Api.UnitTests;

public sealed class ExportTextTests
{
    [Fact]
    public void Money_uses_the_currency_decimals_and_keeps_an_exact_amount()
    {
        Assert.Equal("CLP $30,944,444", ExportText.Money(30944444.4445m, "CLP"));
        Assert.Equal("CLP $1,001", ExportText.Money(1000.6m, "CLP"));
        Assert.Equal("NZD $10.46", ExportText.Money(10.456m, "NZD"));
        Assert.Equal("KWD $12.345", ExportText.Money(12.345m, "KWD"));
        Assert.Equal("CLP $5", ExportText.Money(5m, "clp"));
        Assert.Equal("EUR $5.00", ExportText.Money(5m, "EUR"));
        Assert.Equal("-CLP $1,500", ExportText.Money(-1500m, "CLP"));
        Assert.Equal("CLP $900,000", ExportText.Money(900000m, "CLP", exact: true));
        Assert.Equal("USD $0.1235", ExportText.Money(0.1235m, "USD", exact: true));
        Assert.Equal("USD $750.50", ExportText.Money(750.5m, "USD", exact: true));
        Assert.Equal("EUR $0.00 left", ExportText.Remaining(0m, "EUR"));
        Assert.Equal("EUR $20.00 over", ExportText.Remaining(-20m, "EUR"));
    }

    [Fact]
    public void A_file_name_keeps_letters_and_an_unknown_zone_is_utc()
    {
        Assert.Equal("Berlin move.pdf", ExportText.FileName(" Berlin/move! "));
        Assert.Equal("Berlin move summary.pdf", ExportText.SummaryFileName("Berlin move"));
        Assert.Equal($"{new string('a', 80)}.pdf", ExportText.SummaryFileName(new string('a', 90)));
        Assert.Equal("Relocation.pdf", ExportText.FileName("   "));
        Assert.Equal(84, ExportText.FileName(new string('a', 90)).Length);
        Assert.EndsWith(".pdf", ExportText.FileName(new string('a', 90)));

        Assert.Equal(TimeZoneInfo.Utc, ExportText.Zone(null));
        Assert.Equal(TimeZoneInfo.Utc, ExportText.Zone("not-a-zone"));
        Assert.Equal("15 Nov 2026, 09:30 – 10:00", ExportText.Appointment(
            new DateTimeOffset(2026, 11, 15, 12, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 15, 13, 0, 0, TimeSpan.Zero),
            TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(-3), "Test", "Test")));
        Assert.Equal("15 Nov 2026, 12:30 UTC", ExportText.Appointment(
            new DateTimeOffset(2026, 11, 15, 12, 30, 0, TimeSpan.Zero),
            null,
            TimeZoneInfo.Utc));
        Assert.Equal("12 KB", ExportText.Size(12 * 1024));
    }
}
