using Relovia.Api.Features.Budgets;

namespace Relovia.Api.UnitTests;

public sealed class BudgetRulesTests
{
    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" eur ", "EUR")]
    [InlineData("CLP", "CLP")]
    public void A_currency_code_is_trimmed_and_upper_cased(string value, string expected)
    {
        Assert.Equal(expected, BudgetRules.NormalizeCurrencyCode(value));
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData(" eur ", true)]
    [InlineData("US", false)]
    [InlineData("USDX", false)]
    [InlineData("U5D", false)]
    [InlineData("", false)]
    public void A_currency_code_is_three_letters(string value, bool valid)
    {
        Assert.Equal(valid, BudgetRules.IsCurrencyCode(value));
    }

    [Fact]
    public void The_budget_needs_a_currency()
    {
        var errors = BudgetRules.Validate(new SaveBudgetRequest { CurrencyCode = "dollars" });

        Assert.Equal("Enter a three-letter currency code.", errors["currencyCode"][0]);
        Assert.Empty(BudgetRules.Validate(new SaveBudgetRequest { CurrencyCode = "eur" }));
    }

    [Fact]
    public void Monthly_net_income_is_both_fields_or_neither()
    {
        Assert.Empty(BudgetRules.Validate(new SaveMonthlyNetIncomeRequest()));
        Assert.Empty(BudgetRules.Validate(new SaveMonthlyNetIncomeRequest
        {
            Amount = 4000m,
            CurrencyCode = " eur ",
        }));

        var amountOnly = BudgetRules.Validate(new SaveMonthlyNetIncomeRequest { Amount = 4000m });
        var currencyOnly = BudgetRules.Validate(new SaveMonthlyNetIncomeRequest { CurrencyCode = "EUR" });
        var zero = BudgetRules.Validate(new SaveMonthlyNetIncomeRequest { Amount = 0m, CurrencyCode = "EUR" });
        var tooFine = BudgetRules.Validate(new SaveMonthlyNetIncomeRequest
        {
            Amount = 10.12345m,
            CurrencyCode = "EUR",
        });

        Assert.Equal([BudgetRules.IncomePairMessage], amountOnly["amount"]);
        Assert.Equal([BudgetRules.IncomePairMessage], currencyOnly["amount"]);
        Assert.Equal("Amount must be greater than zero.", zero["amount"][0]);
        Assert.Equal("Amount can have at most 4 decimal places.", tooFine["amount"][0]);
    }

    [Fact]
    public void A_category_needs_a_name_within_the_limit()
    {
        Assert.Equal("Name is required.", BudgetRules.Validate(Category("   "))["name"][0]);
        Assert.Equal(
            "Name must be 100 characters or fewer.",
            BudgetRules.Validate(Category(new string('a', 101)))["name"][0]);
        Assert.Empty(BudgetRules.Validate(Category(new string('a', 100))));
    }

    [Theory]
    [InlineData(0, "Amount must be greater than zero.")]
    [InlineData(-5, "Amount must be greater than zero.")]
    [InlineData(1_000_000_000_001, "Amount must be 1,000,000,000,000 or less.")]
    public void An_estimate_amount_must_be_positive_and_bounded(long amount, string message)
    {
        var errors = BudgetRules.Validate(Estimate(amount));

        Assert.Equal(message, errors["amount"][0]);
    }

    [Fact]
    public void An_amount_keeps_at_most_four_decimal_places()
    {
        var tooPrecise = BudgetRules.Validate(Estimate(10.12345m));

        Assert.Equal("Amount can have at most 4 decimal places.", tooPrecise["amount"][0]);
        Assert.Empty(BudgetRules.Validate(Estimate(10.1234m)));
        Assert.Empty(BudgetRules.Validate(Estimate(1_000_000_000_000m)));
    }

    [Fact]
    public void An_estimate_needs_a_label()
    {
        var errors = BudgetRules.Validate(new SaveBudgetEstimateRequest
        {
            CategoryId = Guid.CreateVersion7(),
            Label = " ",
            Amount = 10,
            CurrencyCode = "EUR",
        });

        Assert.Equal("Label is required.", errors["label"][0]);
    }

    [Fact]
    public void An_estimate_needs_a_currency()
    {
        var errors = BudgetRules.Validate(new SaveBudgetEstimateRequest
        {
            CategoryId = Guid.CreateVersion7(),
            Label = "Sofa",
            Amount = 10,
            CurrencyCode = "NZ",
        });

        Assert.Equal("Enter a three-letter currency code.", errors["currencyCode"][0]);
    }

    [Fact]
    public void An_expense_needs_a_description_amount_and_currency()
    {
        var errors = BudgetRules.Validate(Expense(description: " ", amount: 0, currency: "12"));

        Assert.Equal("Description is required.", errors["description"][0]);
        Assert.Equal("Amount must be greater than zero.", errors["amount"][0]);
        Assert.Equal("Enter a three-letter currency code.", errors["currencyCode"][0]);
    }

    [Fact]
    public void Expense_notes_have_a_limit_and_are_optional()
    {
        Assert.Empty(BudgetRules.Validate(Expense(notes: null)));
        Assert.Empty(BudgetRules.Validate(Expense(notes: new string('n', 1000))));
        Assert.Equal(
            "Notes must be 1000 characters or fewer.",
            BudgetRules.Validate(Expense(notes: new string('n', 1001)))["notes"][0]);
    }

    private static SaveBudgetCategoryRequest Category(string name)
    {
        return new SaveBudgetCategoryRequest { Name = name };
    }

    private static SaveBudgetEstimateRequest Estimate(decimal amount)
    {
        return new SaveBudgetEstimateRequest
        {
            CategoryId = Guid.CreateVersion7(),
            Label = "Deposit",
            Amount = amount,
            CurrencyCode = "EUR",
        };
    }

    private static SaveExpenseRequest Expense(
        string description = "Deposit",
        decimal amount = 100,
        string currency = "EUR",
        string? notes = null)
    {
        return new SaveExpenseRequest
        {
            CategoryId = Guid.CreateVersion7(),
            Description = description,
            Amount = amount,
            CurrencyCode = currency,
            PaidOn = new DateOnly(2026, 11, 1),
            Notes = notes,
        };
    }
}
