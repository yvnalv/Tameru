using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Clean-Architecture boundaries for the Identity module: Domain and Application must not depend on
/// Infrastructure, the web layer, or persistence frameworks (docs/ARCHITECTURE.md, MODULES.md).
/// </summary>
public class IdentityBoundaryTests
{
    private static readonly Assembly Domain = typeof(Identity.Domain.User).Assembly;
    private static readonly Assembly Application = typeof(Identity.Application.AuthService).Assembly;

    [Fact]
    public void Identity_Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(Domain)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Identity.Application",
                "Tameru.Identity.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Identity.Domain must stay pure: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Identity_Application_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(Application)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Identity.Infrastructure",
                "Tameru.Web.Common",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Identity.Application must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
