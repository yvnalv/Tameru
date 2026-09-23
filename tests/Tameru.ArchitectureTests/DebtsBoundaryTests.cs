using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Debts module. Domain and Application must not depend on
/// Infrastructure, the web layer, or persistence frameworks (docs/ARCHITECTURE.md, MODULES.md).
/// </summary>
public class DebtsBoundaryTests
{
    private static readonly Assembly Domain = typeof(Debts.Domain.Liability).Assembly;
    private static readonly Assembly Application = typeof(Debts.Application.DebtsService).Assembly;

    [Fact]
    public void Debts_Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Debts.Application",
                "Tameru.Debts.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Debts.Domain must stay pure: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Debts_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Debts.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Debts.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
