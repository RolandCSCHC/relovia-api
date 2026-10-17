using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Relovia.Api.Common.Persistence;

namespace Relovia.Api.Features.Access;

public static class AccessPolicies
{
    public const string Approved = "approved";
}

public sealed class ApprovedUserRequirement : IAuthorizationRequirement;

public sealed class ApprovedUserHandler(ReloviaDbContext db, AccessPolicy access)
    : AuthorizationHandler<ApprovedUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApprovedUserRequirement requirement)
    {
        if (!access.Restricted)
        {
            context.Succeed(requirement);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var email = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync();
        if (email is not null && access.HasAccess(email))
        {
            context.Succeed(requirement);
        }
    }
}
