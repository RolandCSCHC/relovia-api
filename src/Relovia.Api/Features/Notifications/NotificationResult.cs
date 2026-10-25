namespace Relovia.Api.Features.Notifications;

public abstract record NotificationResult<T>
{
    private NotificationResult()
    {
    }

    public sealed record Success(T Value) : NotificationResult<T>;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : NotificationResult<T>;

    /// <summary>The reminder does not belong to the signed-in user or does not exist.</summary>
    public sealed record NotFound(string Title) : NotificationResult<T>;
}
