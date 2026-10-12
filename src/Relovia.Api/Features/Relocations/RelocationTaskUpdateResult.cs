namespace Relovia.Api.Features.Relocations;

public abstract record RelocationTaskUpdateResult
{
    private RelocationTaskUpdateResult()
    {
    }

    public sealed record Updated(RelocationTaskResponse Task) : RelocationTaskUpdateResult;

    public sealed record RelocationNotFound : RelocationTaskUpdateResult;

    public sealed record TaskNotFound : RelocationTaskUpdateResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : RelocationTaskUpdateResult;
}
