namespace Relovia.Api.Features.Relocations;

public abstract record RelocationTaskMoveResult
{
    private RelocationTaskMoveResult()
    {
    }

    public sealed record Moved(RelocationTasksResponse Tasks) : RelocationTaskMoveResult;

    public sealed record RelocationNotFound : RelocationTaskMoveResult;

    public sealed record TaskNotFound : RelocationTaskMoveResult;

    public sealed record NotAllowed(string Message) : RelocationTaskMoveResult;
}
