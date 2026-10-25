using System.Globalization;

namespace Relovia.Api.Features.Export;

public static class ExportText
{
    public static TimeZoneInfo Zone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var zone)
            && zone is not null)
        {
            return zone;
        }

        return TimeZoneInfo.Utc;
    }

    public static string FileName(string name)
    {
        var characters = name.Trim().Select(character =>
            char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' ? character : ' ');
        var cleaned = string.Join(
            ' ',
            new string(characters.ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (cleaned.Length == 0)
        {
            cleaned = "Relocation";
        }

        if (cleaned.Length > 80)
        {
            cleaned = cleaned[..80].TrimEnd();
        }

        return cleaned + ".pdf";
    }

    public static string SummaryFileName(string name)
    {
        var stem = FileName(name)[..^4] + " summary";
        if (stem.Length > 80)
        {
            stem = stem[..80].TrimEnd();
        }

        return (stem.Length == 0 ? "Relocation summary" : stem) + ".pdf";
    }

    public static string Date(DateOnly date)
    {
        return date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string Appointment(DateTimeOffset start, DateTimeOffset? end, TimeZoneInfo zone)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, zone);
        var text = localStart.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        if (end is DateTimeOffset finishes)
        {
            var localEnd = TimeZoneInfo.ConvertTime(finishes, zone);
            var endText = localEnd.Date == localStart.Date
                ? localEnd.ToString("HH:mm", CultureInfo.InvariantCulture)
                : localEnd.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
            text = $"{text} – {endText}";
        }

        return zone.Equals(TimeZoneInfo.Utc) ? $"{text} UTC" : text;
    }

    public static string Clock(DateTimeOffset start, DateTimeOffset? end, TimeZoneInfo zone)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, zone);
        var text = localStart.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (end is DateTimeOffset finishes)
        {
            var localEnd = TimeZoneInfo.ConvertTime(finishes, zone);
            text = $"{text} – {localEnd.ToString("HH:mm", CultureInfo.InvariantCulture)}";
        }

        return text;
    }

    public static DateOnly LocalDate(DateTimeOffset instant, TimeZoneInfo zone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);
    }

    /// <summary>
    /// Currency code, a dollar sign, and the amount. Totals use the currency's own decimals.
    /// An exact amount keeps up to four decimals, which is what an estimate or expense stores.
    /// </summary>
    public static string Money(decimal amount, string currencyCode, bool exact = false)
    {
        var code = currencyCode.Trim().ToUpperInvariant();
        var minimum = MinorUnits(code);
        var maximum = exact ? Math.Max(4, minimum) : minimum;
        var rounded = decimal.Round(amount, maximum, MidpointRounding.AwayFromZero);
        var negative = rounded < 0;
        var text = Math.Abs(rounded).ToString($"N{maximum}", CultureInfo.InvariantCulture);
        if (exact && maximum > minimum)
        {
            var dot = text.LastIndexOf('.');
            if (dot >= 0)
            {
                var end = text.Length;
                while (end > dot + 1 + minimum && text[end - 1] == '0')
                {
                    end--;
                }

                if (end == dot + 1)
                {
                    end = dot;
                }

                text = text[..end];
            }
        }

        return $"{(negative ? "-" : "")}{code} ${text}";
    }

    public static string Size(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes.ToString(CultureInfo.InvariantCulture)} B";
        }

        if (bytes < 1024 * 1024)
        {
            var kilobytes = Math.Round(bytes / 1024d, MidpointRounding.AwayFromZero);
            return $"{kilobytes.ToString("0", CultureInfo.InvariantCulture)} KB";
        }

        var megabytes = bytes / (1024d * 1024d);
        return $"{megabytes.ToString("0.0", CultureInfo.InvariantCulture)} MB";
    }

    public static string Remaining(decimal amount, string currencyCode)
    {
        return amount < 0
            ? $"{Money(-amount, currencyCode)} over"
            : $"{Money(amount, currencyCode)} left";
    }

    private static int MinorUnits(string currencyCode)
    {
        return currencyCode switch
        {
            "BIF" or "CLP" or "DJF" or "GNF" or "ISK" or "JPY" or "KMF" or "KRW" or "PYG" or "RWF"
                or "UGX" or "UYI" or "VND" or "VUV" or "XAF" or "XOF" or "XPF" => 0,
            "BHD" or "IQD" or "JOD" or "KWD" or "LYD" or "OMR" or "TND" => 3,
            _ => 2,
        };
    }
}
