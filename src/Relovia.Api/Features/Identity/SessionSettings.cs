namespace Relovia.Api.Features.Identity;

public sealed class SessionSettings
{
    public const string SectionName = "Session";

    public TimeSpan SlidingLifetime { get; set; } = TimeSpan.FromDays(14);

    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromDays(30);
}
