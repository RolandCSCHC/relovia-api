namespace Relovia.Api.Features.Relocations;

public static class TaskProgress
{
    public static Snapshot Calculate(IEnumerable<RelocationTaskStatus> statuses)
    {
        var done = 0;
        var counted = 0;
        foreach (var status in statuses)
        {
            if (status is RelocationTaskStatus.Skipped)
            {
                continue;
            }

            counted++;
            if (status is RelocationTaskStatus.Done)
            {
                done++;
            }
        }

        var ratio = counted == 0 ? 0m : decimal.Divide(done, counted);
        return new Snapshot(done, counted, ratio);
    }

    public static bool IsDependencySatisfied(RelocationTaskStatus status)
    {
        return status is RelocationTaskStatus.Done or RelocationTaskStatus.Skipped;
    }

    public readonly record struct Snapshot(int Done, int Counted, decimal Ratio);
}
