namespace Relovia.Api.Features.Identity;

public static class SignInRedirect
{
    public static string Success(string appOrigin)
    {
        return $"{TrimOrigin(appOrigin)}/";
    }

    public static string Failure(string appOrigin, string errorCode)
    {
        return $"{TrimOrigin(appOrigin)}/sign-in?error={Uri.EscapeDataString(errorCode)}";
    }

    private static string TrimOrigin(string appOrigin)
    {
        return appOrigin.Trim().TrimEnd('/');
    }
}
