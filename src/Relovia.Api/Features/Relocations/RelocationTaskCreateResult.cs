namespace Relovia.Api.Features.Relocations;

public abstract record RelocationTaskCreateResult
{
    private RelocationTaskCreateResult()
    {
    }

    public sealed record Created(RelocationTaskResponse Task) : RelocationTaskCreateResult;

    public sealed record RelocationNotFound : RelocationTaskCreateResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : RelocationTaskCreateResult;
}
