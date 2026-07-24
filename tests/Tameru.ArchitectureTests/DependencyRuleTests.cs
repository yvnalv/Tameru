using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.ArchitectureTests;

/// <summary>
/// Architecture-fitness tests that enforce the Clean-Architecture dependency direction
/// (docs/ARCHITECTURE.md). These grow as modules are added; for M0 they guard the BuildingBlocks.
/// </summary>
public class DependencyRuleTests
{
    private static readonly Assembly SharedKernel = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAbstractions =
        typeof(Application.Abstractions.ICurrentUser).Assembly;

    [Fact]
    public void SharedKernel_should_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(SharedKernel)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Application.Abstractions",
                "Tameru.Infrastructure.Common",
                "Tameru.Web.Common",
                "Tameru.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "the SharedKernel must have no outward dependencies: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ApplicationAbstractions_should_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(ApplicationAbstractions)
            .Should()
            .NotHaveDependencyOnAny(
                "Tameru.Infrastructure.Common",
                "Tameru.Web.Common",
                "Tameru.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application.Abstractions must not reference Infrastructure or Web: {0}",
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
