using Relovia.Api.Features.Documents;

namespace Relovia.Api.UnitTests;

public sealed class DocumentDetailsRulesTests
{
    [Fact]
    public void An_empty_category_and_no_tasks_are_valid()
    {
        var errors = DocumentDetailsRules.Validate(new UpdateDocumentRequest
        {
            Category = null,
            ExpiresOn = new DateOnly(2020, 1, 15),
            TaskIds = [],
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void An_unknown_category_or_task_id_is_rejected()
    {
        var unknownCategory = DocumentDetailsRules.Validate(new UpdateDocumentRequest
        {
            Category = (DocumentCategory)99,
            TaskIds = [],
        });
        var missingTasks = DocumentDetailsRules.Validate(new UpdateDocumentRequest
        {
            TaskIds = null,
        });
        var emptyTask = DocumentDetailsRules.Validate(new UpdateDocumentRequest
        {
            TaskIds = [Guid.Empty],
        });

        Assert.Equal([DocumentDetailsRules.UnknownCategoryMessage], unknownCategory["category"]);
        Assert.Equal([DocumentDetailsRules.TaskIdsRequiredMessage], missingTasks["taskIds"]);
        Assert.Equal([DocumentDetailsRules.UnknownTaskMessage], emptyTask["taskIds"]);
    }

    [Fact]
    public void A_repeated_task_is_stored_once()
    {
        var taskId = Guid.CreateVersion7();

        var ids = DocumentDetailsRules.DistinctTaskIds([taskId, taskId]);

        Assert.Equal([taskId], ids);
    }
}
