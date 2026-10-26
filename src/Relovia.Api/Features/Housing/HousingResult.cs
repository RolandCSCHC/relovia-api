namespace Relovia.Api.Features.Housing;

public abstract record HousingResult<T>
{
    private HousingResult()
    {
    }

    public sealed record Success(T Value) : HousingResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : HousingResult<T>;

    /// <summary>The relocation, or a home inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : HousingResult<T>;
}
