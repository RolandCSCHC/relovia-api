using System.IO.Compression;

namespace Relovia.Api.Features.Places;

public sealed class GeoNamesCityListSource(HttpClient http, IConfiguration configuration) : ICityListSource
{
    public const string CitiesFileKey = "GeoNames:CitiesFile";

    public const string Admin1FileKey = "GeoNames:Admin1File";

    public async Task<CityListFiles> OpenAsync(CancellationToken cancellationToken)
    {
        var citiesFile = configuration[CitiesFileKey];
        var admin1File = configuration[Admin1FileKey];
        if (!string.IsNullOrWhiteSpace(citiesFile) && !string.IsNullOrWhiteSpace(admin1File))
        {
            return new CityListFiles(File.OpenRead(citiesFile), File.OpenRead(admin1File));
        }

        var regions = await DownloadAsync("export/dump/admin1CodesASCII.txt", cancellationToken);
        try
        {
            var zipStream = await DownloadAsync("export/dump/cities1000.zip", cancellationToken);
            var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
            var entry = archive.GetEntry("cities1000.txt");
            if (entry is null)
            {
                archive.Dispose();
                throw new InvalidDataException("cities1000.txt is missing.");
            }

            return new CityListFiles(entry.Open(), regions, archive);
        }
        catch
        {
            await regions.DisposeAsync();
            throw;
        }
    }

    private async Task<MemoryStream> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var buffer = new MemoryStream();
        try
        {
            await response.Content.CopyToAsync(buffer, cancellationToken);
        }
        catch
        {
            await buffer.DisposeAsync();
            throw;
        }

        buffer.Position = 0;
        return buffer;
    }
}
