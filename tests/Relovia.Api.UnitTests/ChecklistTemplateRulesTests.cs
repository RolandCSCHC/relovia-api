using Relovia.Api.Features.Relocations;

namespace Relovia.Api.UnitTests;

public sealed class ChecklistTemplateRulesTests
{
    [Fact]
    public void A_task_can_depend_on_another_code()
    {
        var errors = ChecklistTemplateRules.ValidateDependencies("book-movers", ["give-notice"]);

        Assert.Empty(errors);
    }

    [Fact]
    public void A_task_cannot_depend_on_its_own_code()
    {
        var task = new TaskTemplate
        {
            Code = "book-movers",
            Title = "Book movers",
        };

        var exception = Assert.Throws<InvalidOperationException>(() => task.ReplaceDependencies(["book-movers"]));

        Assert.Equal("A task template cannot depend on its own code.", exception.Message);
        Assert.Empty(task.Dependencies);
    }

    [Fact]
    public void A_task_cannot_list_the_same_dependency_twice()
    {
        var errors = ChecklistTemplateRules.ValidateDependencies("book-movers", ["give-notice", "give-notice"]);

        Assert.Equal("A task template cannot list the same dependency code twice.", Assert.Single(errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("bad/code")]
    public void A_code_is_a_short_token(string code)
    {
        Assert.False(ChecklistTemplateRules.IsCode(code));
    }
}
