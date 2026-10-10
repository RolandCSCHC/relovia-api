namespace Relovia.Api.Features.Identity;

public sealed record GoogleIdentity(string Subject, string Email, string DisplayName, string? AvatarUrl);
