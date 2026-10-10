namespace Relovia.Api.Features.Identity;

public enum ExternalAccountDecision
{
    Create,
    Existing,
    RejectEmailInUse,
}

public static class ExternalAccountLink
{
    public static ExternalAccountDecision Decide(bool subjectAlreadyLinked, bool emailOwnedByAnotherUser)
    {
        if (subjectAlreadyLinked)
        {
            return ExternalAccountDecision.Existing;
        }

        if (emailOwnedByAnotherUser)
        {
            return ExternalAccountDecision.RejectEmailInUse;
        }

        return ExternalAccountDecision.Create;
    }
}
