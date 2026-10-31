using Relovia.Api.Common.Persistence;
using Relovia.Api.Features.Relocations;

namespace Relovia.Api.Features.Identity;

public sealed class User : ICreationTimestamp, IUpdateTimestamp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string PreferredCurrency { get; set; } = "USD";

    public string PreferredLanguage { get; set; } = "en";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ExternalLogin> ExternalLogins { get; } = new List<ExternalLogin>();

    public ICollection<AuthSession> AuthSessions { get; } = new List<AuthSession>();

    public ICollection<Relocation> Relocations { get; } = new List<Relocation>();
}
