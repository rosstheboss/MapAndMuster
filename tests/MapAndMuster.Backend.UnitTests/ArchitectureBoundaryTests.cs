using System.Reflection;
using MapAndMuster.Application;
using MapAndMuster.Domain;

namespace MapAndMuster.Backend.UnitTests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void DomainDoesNotReferenceEntityFrameworkOrAspNetCore()
    {
        AssertNoInfrastructureFrameworkReferences(typeof(DomainAssembly).Assembly.GetReferencedAssemblies());
    }

    [Fact]
    public void ApplicationDoesNotReferenceEntityFrameworkOrAspNetCore()
    {
        AssertNoInfrastructureFrameworkReferences(typeof(ApplicationAssembly).Assembly.GetReferencedAssemblies());
    }

    private static void AssertNoInfrastructureFrameworkReferences(IEnumerable<AssemblyName> assemblies)
    {
        var referencedNames = assemblies
            .Select(assembly => assembly.Name)
            .OfType<string>()
            .ToArray();

        Assert.DoesNotContain(
            referencedNames,
            name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(
            referencedNames,
            name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(
            referencedNames,
            name => name.StartsWith("Npgsql", StringComparison.Ordinal));
    }
}
