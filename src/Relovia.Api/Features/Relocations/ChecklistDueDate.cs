namespace Relovia.Api.Features.Relocations;

public static class ChecklistDueDate
{
    public static DateOnly? On(DateOnly? targetDate, int daysBeforeMove)
    {
        return targetDate?.AddDays(-daysBeforeMove);
    }

    public static int DaysBefore(DateOnly targetDate, DateOnly dueDate)
    {
        return targetDate.DayNumber - dueDate.DayNumber;
    }
}
