namespace Relovia.Api.Features.Relocations;

public static class TaskDependencyRules
{
    public const string SelfReferenceMessage = "A task cannot depend on itself.";

    public const string CrossRelocationMessage = "A task dependency cannot cross relocations.";

    public const string CycleMessage = "Task dependencies cannot contain a cycle.";

    public static string? Validate(IReadOnlyList<Link> links)
    {
        foreach (var link in links)
        {
            if (link.RelocationId != link.DependsOnRelocationId)
            {
                return CrossRelocationMessage;
            }

            if (link.TaskId == link.DependsOnTaskId)
            {
                return SelfReferenceMessage;
            }
        }

        return HasCycle(links) ? CycleMessage : null;
    }

    private static bool HasCycle(IReadOnlyList<Link> links)
    {
        var outgoing = new Dictionary<Guid, List<Guid>>();
        var nodes = new HashSet<Guid>();
        foreach (var link in links)
        {
            nodes.Add(link.TaskId);
            nodes.Add(link.DependsOnTaskId);
            if (!outgoing.TryGetValue(link.TaskId, out var next))
            {
                next = [];
                outgoing[link.TaskId] = next;
            }

            next.Add(link.DependsOnTaskId);
        }

        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        foreach (var node in nodes)
        {
            if (Visit(node))
            {
                return true;
            }
        }

        return false;

        bool Visit(Guid node)
        {
            if (visited.Contains(node))
            {
                return false;
            }

            if (!visiting.Add(node))
            {
                return true;
            }

            if (outgoing.TryGetValue(node, out var next))
            {
                foreach (var dependency in next)
                {
                    if (Visit(dependency))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    public readonly record struct Link(
        Guid RelocationId,
        Guid TaskId,
        Guid DependsOnRelocationId,
        Guid DependsOnTaskId);
}
