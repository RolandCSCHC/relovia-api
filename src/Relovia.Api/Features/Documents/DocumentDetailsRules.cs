namespace Relovia.Api.Features.Documents;

public static class DocumentDetailsRules
{
    public const string UnknownCategoryMessage = "Choose a category.";

    public const string TaskIdsRequiredMessage = "List the tasks this document supports.";

    public const string UnknownTaskMessage = "Choose a task from this relocation.";

    public static Dictionary<string, string[]> Validate(UpdateDocumentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request.Category is { } category && !Enum.IsDefined(category))
        {
            errors["category"] = [UnknownCategoryMessage];
        }

        if (request.TaskIds is null)
        {
            errors["taskIds"] = [TaskIdsRequiredMessage];
        }
        else if (request.TaskIds.Contains(Guid.Empty))
        {
            errors["taskIds"] = [UnknownTaskMessage];
        }

        return errors;
    }

    public static Guid[] DistinctTaskIds(IReadOnlyList<Guid> taskIds)
    {
        return taskIds.Distinct().ToArray();
    }
}
