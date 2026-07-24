using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Accounts module. Domain and Application must not depend on
/// Infrastructure, the web layer, or persistence frameworks (docs/ARCHITECTURE.md, MODULES.md).
/// </summary>
public class AccountsBoundaryTests
{
    private static readonly Assembly Domain = typeof(Accounts.Domain.Account).Assembly;
    private static readonly Assembly Application = typeof(Accounts.Application.AccountService).Assembly;

    [Fact]
    public void Accounts_Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Accounts.Application",
                "Tameru.Accounts.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Accounts.Domain must stay pure: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Accounts_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Accounts.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Accounts.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
