using System.Reflection;

namespace LaBlanca.UnitTests.Architecture;

public class DependencyRulesTests
{
    private const string SolutionPrefix = "LaBlanca.";

    [Theory]
    [InlineData("LaBlanca.Domain")]
    [InlineData("LaBlanca.Shared")]
    public void Assembly_does_not_reference_other_solution_projects(string assemblyName)
    {
        var references = Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith(SolutionPrefix, StringComparison.Ordinal));

        references.Should().BeEmpty();
    }

    [Fact]
    public void Domain_does_not_reference_infrastructure_packages()
    {
        var references = Assembly.Load("LaBlanca.Domain")
            .GetReferencedAssemblies()
            .Select(a => a.Name!);

        references.Should().NotContain(n =>
            n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || n.StartsWith("Npgsql", StringComparison.Ordinal)
            || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var references = Assembly.Load("LaBlanca.Application")
            .GetReferencedAssemblies()
            .Select(a => a.Name!);

        references.Should().NotContain(["LaBlanca.Infrastructure", "LaBlanca.Api", "LaBlanca.Migrations"]);
    }
}
