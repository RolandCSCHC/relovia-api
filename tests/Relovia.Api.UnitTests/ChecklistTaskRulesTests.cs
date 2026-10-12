using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class ChecklistTaskRulesTests
{
    private static readonly DateOnly Target = new(2026, 12, 1);

    [Fact]
    public void Due_date_is_the_target_date_minus_the_days_before_the_move()
    {
        Assert.Null(ChecklistDueDate.On(null, 60));
        Assert.Equal(new DateOnly(2026, 10, 2), ChecklistDueDate.On(Target, 60));
        Assert.Equal(Target, ChecklistDueDate.On(Target, 0));
        Assert.Equal(new DateOnly(2026, 12, 15), ChecklistDueDate.On(Target, -14));
    }

    [Fact]
    public void Progress_counts_done_tasks_and_leaves_skipped_tasks_out()
    {
        var progress = TaskProgress.Calculate(
        [
            RelocationTaskStatus.Done,
            RelocationTaskStatus.Todo,
            RelocationTaskStatus.InProgress,
            RelocationTaskStatus.Skipped,
        ]);

        Assert.Equal(1, progress.Done);
        Assert.Equal(3, progress.Counted);
        Assert.Equal(1m / 3m, progress.Ratio);
    }

    [Fact]
    public void Progress_is_zero_when_every_task_is_skipped_or_there_are_no_tasks()
    {
        var skipped = TaskProgress.Calculate([RelocationTaskStatus.Skipped, RelocationTaskStatus.Skipped]);
        var empty = TaskProgress.Calculate([]);

        Assert.Equal(0, skipped.Counted);
        Assert.Equal(0m, skipped.Ratio);
        Assert.Equal(0m, empty.Ratio);
    }

    [Theory]
    [InlineData(RelocationTaskStatus.Done, true)]
    [InlineData(RelocationTaskStatus.Skipped, true)]
    [InlineData(RelocationTaskStatus.Todo, false)]
    [InlineData(RelocationTaskStatus.InProgress, false)]
    public void A_dependency_is_satisfied_when_the_prerequisite_is_done_or_skipped(
        RelocationTaskStatus status,
        bool satisfied)
    {
        Assert.Equal(satisfied, TaskProgress.IsDependencySatisfied(status));
    }

    [Fact]
    public void A_task_cannot_depend_on_itself()
    {
        var taskId = Guid.CreateVersion7();
        var relocationId = Guid.CreateVersion7();

        var error = TaskDependencyRules.Validate(
        [
            new TaskDependencyRules.Link(relocationId, taskId, relocationId, taskId),
        ]);

        Assert.Equal(TaskDependencyRules.SelfReferenceMessage, error);
    }

    [Fact]
    public void A_dependency_cannot_cross_relocations()
    {
        var error = TaskDependencyRules.Validate(
        [
            new TaskDependencyRules.Link(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
        ]);

        Assert.Equal(TaskDependencyRules.CrossRelocationMessage, error);
    }

    [Fact]
    public void A_dependency_cycle_is_rejected()
    {
        var relocationId = Guid.CreateVersion7();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var third = Guid.CreateVersion7();

        var error = TaskDependencyRules.Validate(
        [
            Link(relocationId, first, second),
            Link(relocationId, second, third),
            Link(relocationId, third, first),
        ]);

        Assert.Equal(TaskDependencyRules.CycleMessage, error);
    }

    [Fact]
    public void A_chain_of_dependencies_is_allowed()
    {
        var relocationId = Guid.CreateVersion7();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var third = Guid.CreateVersion7();

        var error = TaskDependencyRules.Validate(
        [
            Link(relocationId, first, second),
            Link(relocationId, second, third),
        ]);

        Assert.Null(error);
    }

    private static TaskDependencyRules.Link Link(Guid relocationId, Guid taskId, Guid dependsOnTaskId)
    {
        return new TaskDependencyRules.Link(relocationId, taskId, relocationId, dependsOnTaskId);
    }
}
