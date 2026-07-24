using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Reporting module. It owns no data and composes other
/// modules only through the shared contracts (docs/MODULES.md → Reporting): its Application must not
/// depend on any other module's internals, on Infrastructure, the web layer, or persistence.
/// </summary>
public class ReportingBoundaryTests
{
    private static readonly Assembly Application = typeof(Reporting.Application.ReportingService).Assembly;

    [Fact]
    public void Reporting_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Reporting.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Reporting.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Reporting_Application_should_not_depend_on_other_modules_internals()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Accounts.Application",
                "Tameru.Accounts.Domain",
                "Tameru.Accounts.Infrastructure",
                "Tameru.Ledger.Application",
                "Tameru.Ledger.Domain",
                "Tameru.Ledger.Infrastructure",
                "Tameru.Budgeting.Application",
                "Tameru.Budgeting.Domain",
                "Tameru.Budgeting.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Reporting must consume other modules only via Modules.Contracts: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
