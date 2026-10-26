using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class RelocationTaskRulesTests
{
    [Fact]
    public void A_blank_description_and_link_stay_empty()
    {
        var errors = RelocationTaskRules.Validate(new CreateRelocationTaskRequest
        {
            Title = "Call the landlord",
            Phase = TaskPhase.Housing,
            Priority = TaskPriority.Normal,
            Description = "  ",
            Url = "   ",
        });

        Assert.Empty(errors);
        Assert.Null(RelocationTaskRules.NormalizeOptional("  "));
        Assert.Equal("Tell them in writing.", RelocationTaskRules.NormalizeOptional(" Tell them in writing. "));
        Assert.Equal("https://example.com/notice", RelocationTaskRules.NormalizeOptional(" https://example.com/notice "));
    }

    [Fact]
    public void A_link_must_be_an_absolute_http_address()
    {
        Assert.Empty(ValidateLink("https://example.com/notice"));
        Assert.Empty(ValidateLink("http://localhost/notice"));
        Assert.Empty(ValidateLink(" HTTPS://example.com/Path "));

        Assert.Equal([RelocationTaskRules.UrlMessage], ValidateLink("example.com")["url"]);
        Assert.Equal([RelocationTaskRules.UrlMessage], ValidateLink("javascript:alert(1)")["url"]);
        Assert.Equal([RelocationTaskRules.UrlMessage], ValidateLink("ftp://example.com/file")["url"]);
        Assert.Equal([RelocationTaskRules.UrlMessage], ValidateLink("https://example.com/a b")["url"]);
        Assert.Equal([RelocationTaskRules.UrlMessage], ValidateLink("https://")["url"]);
    }

    [Fact]
    public void A_description_and_link_have_a_maximum_length()
    {
        var errors = RelocationTaskRules.Validate(new UpdateRelocationTaskRequest
        {
            Title = "Call the landlord",
            Phase = TaskPhase.Housing,
            Priority = TaskPriority.Normal,
            Status = RelocationTaskStatus.Todo,
            Description = new string('a', RelocationTaskRules.DescriptionMaxLength + 1),
            Url = "https://example.com/" + new string('a', RelocationTaskRules.UrlMaxLength),
        });

        Assert.Equal(
            [$"Description must be {RelocationTaskRules.DescriptionMaxLength} characters or fewer."],
            errors["description"]);
        Assert.Equal(
            [$"Link must be {RelocationTaskRules.UrlMaxLength} characters or fewer."],
            errors["url"]);
    }

    private static Dictionary<string, string[]> ValidateLink(string url)
    {
        return RelocationTaskRules.Validate(new CreateRelocationTaskRequest
        {
            Title = "Call the landlord",
            Phase = TaskPhase.Housing,
            Priority = TaskPriority.Normal,
            Url = url,
        });
    }
}
