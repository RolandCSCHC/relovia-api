namespace Relovia.Api.Features.Relocations;

public abstract record RelocationTaskDeleteResult
{
    private RelocationTaskDeleteResult()
    {
    }

    public sealed record Deleted : RelocationTaskDeleteResult;

    public sealed record RelocationNotFound : RelocationTaskDeleteResult;

    public sealed record TaskNotFound : RelocationTaskDeleteResult;
}
