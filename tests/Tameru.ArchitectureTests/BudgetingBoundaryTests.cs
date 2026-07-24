using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Budgeting module. Domain and Application must not depend on
/// Infrastructure, the web layer, or persistence frameworks (docs/ARCHITECTURE.md, MODULES.md).
/// </summary>
public class BudgetingBoundaryTests
{
    private static readonly Assembly Domain = typeof(Budgeting.Domain.Category).Assembly;
    private static readonly Assembly Application = typeof(Budgeting.Application.BudgetService).Assembly;

    [Fact]
    public void Budgeting_Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Budgeting.Application",
                "Tameru.Budgeting.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Budgeting.Domain must stay pure: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Budgeting_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Budgeting.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Budgeting.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
