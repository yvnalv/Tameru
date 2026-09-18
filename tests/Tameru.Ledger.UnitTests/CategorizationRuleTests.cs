using FluentAssertions;
using Tameru.Ledger.Domain;
using Xunit;

namespace Tameru.Ledger.UnitTests;

public class CategorizationRuleTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Theory]
    [InlineData("Starbucks Reserve", "coffee", true)]
    [InlineData("Janji Jiwa", "lunch", false)]
    [InlineData("STARBUCKS", "", true)]
    public void Matches_contains_operator_on_payee(string payee, string desc, bool expected)
    {
        var rule = CategorizationRule.Create("Starbucks Rule", "starbucks", targetCategoryId: CategoryId, matchField: RuleMatchField.Payee, matchOperator: RuleMatchOperator.Contains);
        rule.Matches(payee, desc).Should().Be(expected);
    }

    [Theory]
    [InlineData("Netflix", true)]
    [InlineData("netflix", true)]
    [InlineData("Netflix Subscription", false)]
    public void Matches_equals_operator_on_payee(string payee, bool expected)
    {
        var rule = CategorizationRule.Create("Exact Netflix", "netflix", targetCategoryId: CategoryId, matchField: RuleMatchField.Payee, matchOperator: RuleMatchOperator.Equals);
        rule.Matches(payee, null).Should().Be(expected);
    }

    [Theory]
    [InlineData("Indomaret Point", true)]
    [InlineData("indomaret fresh", true)]
    [InlineData("Toko Indomaret", false)]
    public void Matches_startsWith_operator_on_payee(string payee, bool expected)
    {
        var rule = CategorizationRule.Create("Indomaret prefix", "indomaret", targetCategoryId: CategoryId, matchField: RuleMatchField.Payee, matchOperator: RuleMatchOperator.StartsWith);
        rule.Matches(payee, null).Should().Be(expected);
    }

    [Theory]
    [InlineData("Grab Food order #123", true)]
    [InlineData("Grab Bike trip", true)]
    [InlineData("Gojek Ride", false)]
    public void Matches_regex_operator_on_description(string desc, bool expected)
    {
        var rule = CategorizationRule.Create("Grab services", @"grab\s+(food|bike)", targetCategoryId: CategoryId, matchField: RuleMatchField.Description, matchOperator: RuleMatchOperator.Regex);
        rule.Matches("Unknown", desc).Should().Be(expected);
    }

    [Fact]
    public void Inactive_rule_never_matches()
    {
        var rule = CategorizationRule.Create("Inactive Rule", "kopi", targetCategoryId: CategoryId, matchField: RuleMatchField.Payee, matchOperator: RuleMatchOperator.Contains);
        rule.Update("Inactive Rule", "kopi", targetCategoryId: CategoryId, targetBudgetCategoryId: null, targetSubCategoryId: null, matchField: RuleMatchField.Payee, matchOperator: RuleMatchOperator.Contains, priority: 0, isActive: false, targetStatus: null);

        rule.Matches("kopi kenangan", "").Should().BeFalse();
    }

    [Theory]
    [InlineData(50000, false)]
    [InlineData(150000, true)]
    [InlineData(1000000, true)]
    [InlineData(1500000, false)]
    public void Matches_respects_amount_thresholds(decimal amount, bool expected)
    {
        var rule = CategorizationRule.Create("Tokopedia Electronics", "tokopedia", targetCategoryId: CategoryId, minAmount: 100000m, maxAmount: 1000000m);
        rule.Matches("Tokopedia", null, amount: amount).Should().Be(expected);
    }

    [Fact]
    public void Matches_respects_specific_account_filter()
    {
        var accountGopay = Guid.NewGuid();
        var accountBca = Guid.NewGuid();
        var rule = CategorizationRule.Create("Gopay Only", "gojek", targetCategoryId: CategoryId, accountId: accountGopay);

        rule.Matches("Gojek", null, accountId: accountGopay).Should().BeTrue();
        rule.Matches("Gojek", null, accountId: accountBca).Should().BeFalse();
    }

    [Theory]
    [InlineData("Expense", true)]
    [InlineData("Income", false)]
    [InlineData("Transfer", false)]
    public void Matches_respects_transaction_type_constraint(string type, bool expected)
    {
        var rule = CategorizationRule.Create("Expense Only", "reimburse", targetCategoryId: CategoryId, transactionType: "Expense");
        rule.Matches("Reimburse", null, type: type).Should().Be(expected);
    }
}
