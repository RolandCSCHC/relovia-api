using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Relovia.Api.Features.Identity;

namespace Relovia.Api.UnitTests;

public sealed class SessionTokenTests
{
    [Fact]
    public void Create_returns_a_token_whose_hash_is_not_the_token()
    {
        var token = SessionToken.Create();
        var raw = WebEncoders.Base64UrlDecode(token);
        var hash = SessionToken.TryHash(token);

        Assert.Equal(SessionToken.Size, raw.Length);
        Assert.NotNull(hash);
        Assert.Equal(SHA256.HashSizeInBytes, hash.Length);
        Assert.NotEqual(raw, hash);
        Assert.Equal(hash, SessionToken.TryHash(token));
    }

    [Fact]
    public void Two_tokens_have_different_hashes()
    {
        var first = SessionToken.TryHash(SessionToken.Create());
        var second = SessionToken.TryHash(SessionToken.Create());

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("YQ")]
    public void Invalid_tokens_are_not_hashed(string token)
    {
        Assert.Null(SessionToken.TryHash(token));
    }
}
