namespace Relovia.Api.Features.Relocations;

public abstract record RelocationSaveResult
{
    private RelocationSaveResult()
    {
    }

    public sealed record Saved(RelocationResponse Relocation) : RelocationSaveResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : RelocationSaveResult;

    public sealed record NotFound : RelocationSaveResult;
}
