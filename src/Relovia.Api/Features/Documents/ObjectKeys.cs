namespace Relovia.Api.Features.Documents;

/// <summary>A storage key is the document id in hex, so it cannot name a directory.</summary>
public static class ObjectKeys
{
    public static bool IsValid(string? key)
    {
        if (key is not { Length: 32 })
        {
            return false;
        }

        foreach (var character in key)
        {
            if (character is not (>= '0' and <= '9' or >= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return true;
    }

    public static void Require(string key)
    {
        if (!IsValid(key))
        {
            throw new ArgumentException("The storage key is not a document id.", nameof(key));
        }
    }
}
