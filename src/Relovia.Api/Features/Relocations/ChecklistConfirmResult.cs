namespace Relovia.Api.Features.Relocations;

public abstract record ChecklistConfirmResult
{
    private ChecklistConfirmResult()
    {
    }

    public sealed record Confirmed(RelocationTasksResponse Tasks) : ChecklistConfirmResult;

    public sealed record NotFound : ChecklistConfirmResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : ChecklistConfirmResult;

    public sealed record AlreadyConfirmed : ChecklistConfirmResult;
}
