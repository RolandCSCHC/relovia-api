namespace Relovia.Api.Features.Calendar;

public abstract record CalendarResult<T>
{
    private CalendarResult()
    {
    }

    public sealed record Success(T Value) : CalendarResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CalendarResult<T>;

    /// <summary>The relocation, or an event inside it, does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : CalendarResult<T>;
}
