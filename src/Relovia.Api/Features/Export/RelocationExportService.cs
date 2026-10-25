using Relovia.Api.Features.Budgets;
using Relovia.Api.Features.Calendar;
using Relovia.Api.Features.Documents;
using Relovia.Api.Features.Inventory;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Export;

public sealed class RelocationExportService(
    RelocationService relocations,
    ChecklistService checklist,
    BudgetService budgets,
    ExpenseService expenses,
    InventoryService inventory,
    CalendarService calendar,
    DocumentService documents)
{
    public async Task<ExportResult> DownloadAsync(
        Guid userId,
        Guid relocationId,
        string? timeZoneId,
        CancellationToken cancellationToken)
    {
        var relocation = await relocations.FindAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new ExportResult.NotFound();
        }

        var tasks = await checklist.ListAsync(userId, relocationId, cancellationToken);
        var budget = await budgets.GetAsync(userId, relocationId, cancellationToken);
        var expenseList = await expenses.ListAsync(userId, relocationId, cancellationToken);
        var items = await inventory.ListAsync(userId, relocationId, cancellationToken);
        var schedule = await calendar.GetAsync(userId, relocationId, cancellationToken);
        var files = await documents.ListAsync(userId, relocationId, cancellationToken);
        if (tasks is null
            || budget is not BudgetResult<BudgetResponse>.Success budgetOk
            || expenseList is not BudgetResult<IReadOnlyList<ExpenseResponse>>.Success expenseOk
            || items is not InventoryResult<IReadOnlyList<InventoryItemResponse>>.Success itemOk
            || schedule is not CalendarResult<RelocationCalendarResponse>.Success scheduleOk
            || files is not DocumentResult<IReadOnlyList<DocumentResponse>>.Success fileOk)
        {
            return new ExportResult.NotFound();
        }

        var packet = RelocationPacketComposer.Compose(
            relocation,
            tasks,
            budgetOk.Value,
            expenseOk.Value,
            itemOk.Value,
            scheduleOk.Value.Events,
            fileOk.Value,
            ExportText.Zone(timeZoneId));
        return new ExportResult.Pdf(RelocationPdf.Render(packet), packet.FileName);
    }

    public async Task<ExportResult> DownloadSummaryAsync(
        Guid userId,
        Guid relocationId,
        string? timeZoneId,
        CancellationToken cancellationToken)
    {
        var relocation = await relocations.FindAsync(userId, relocationId, cancellationToken);
        if (relocation is null)
        {
            return new ExportResult.NotFound();
        }

        var tasks = await checklist.ListAsync(userId, relocationId, cancellationToken);
        var budget = await budgets.GetAsync(userId, relocationId, cancellationToken);
        var items = await inventory.ListAsync(userId, relocationId, cancellationToken);
        var schedule = await calendar.GetAsync(userId, relocationId, cancellationToken);
        var files = await documents.ListAsync(userId, relocationId, cancellationToken);
        if (tasks is null
            || budget is not BudgetResult<BudgetResponse>.Success budgetOk
            || items is not InventoryResult<IReadOnlyList<InventoryItemResponse>>.Success itemOk
            || schedule is not CalendarResult<RelocationCalendarResponse>.Success scheduleOk
            || files is not DocumentResult<IReadOnlyList<DocumentResponse>>.Success fileOk)
        {
            return new ExportResult.NotFound();
        }

        var packet = RelocationPacketComposer.ComposeSummary(
            relocation,
            tasks,
            budgetOk.Value,
            itemOk.Value,
            scheduleOk.Value.Events,
            fileOk.Value,
            ExportText.Zone(timeZoneId));
        return new ExportResult.Pdf(RelocationPdf.Render(packet), packet.FileName);
    }
}
