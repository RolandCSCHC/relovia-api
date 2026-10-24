namespace Relovia.Api.Features.Documents;

public abstract record DocumentResult<T>
{
    private DocumentResult()
    {
    }

    public sealed record Success(T Value) : DocumentResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : DocumentResult<T>;

    /// <summary>The relocation, or a document inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : DocumentResult<T>;
}
