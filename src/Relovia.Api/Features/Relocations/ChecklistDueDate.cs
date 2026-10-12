namespace Relovia.Api.Features.Relocations;

public static class ChecklistDueDate
{
    public static DateOnly On(DateOnly targetDate, int daysBeforeMove)
    {
        return targetDate.AddDays(-daysBeforeMove);
    }
}
