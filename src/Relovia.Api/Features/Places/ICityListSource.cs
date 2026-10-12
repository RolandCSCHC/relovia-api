namespace Relovia.Api.Features.Places;

public interface ICityListSource
{
    Task<CityListFiles> OpenAsync(CancellationToken cancellationToken);
}
