namespace Relovia.Api.Features.Applications;

public abstract record JobApplicationResult<T>
{
    private JobApplicationResult()
    {
    }

    public sealed record Success(T Value) : JobApplicationResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : JobApplicationResult<T>;

    /// <summary>The relocation, or an application inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : JobApplicationResult<T>;
}
