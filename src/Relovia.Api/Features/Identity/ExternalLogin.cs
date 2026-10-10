using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Identity;

public sealed class ExternalLogin : ICreationTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderSubject { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
