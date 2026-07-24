using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Ledger module. Domain and Application must not depend on
/// Infrastructure, the web layer, or persistence frameworks (docs/ARCHITECTURE.md, MODULES.md).
/// </summary>
public class LedgerBoundaryTests
{
    private static readonly Assembly Domain = typeof(Ledger.Domain.Transaction).Assembly;
    private static readonly Assembly Application = typeof(Ledger.Application.LedgerService).Assembly;

    [Fact]
    public void Ledger_Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Ledger.Application",
                "Tameru.Ledger.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Ledger.Domain must stay pure: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ledger_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Ledger.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Ledger.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
