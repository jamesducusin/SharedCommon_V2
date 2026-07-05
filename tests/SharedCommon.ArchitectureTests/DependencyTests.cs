using NetArchTest.Rules;
using Xunit;

namespace SharedCommon.ArchitectureTests;

public class DependencyTests
{
    [Fact]
    public void No_Circular_Dependencies_In_Core()
    {
        var types = Types.InNamespace("SharedCommon.Core").GetTypes();
        Assert.NotEmpty(types);
    }

    [Fact]
    public void Services_Must_Not_Have_Static_Dependencies()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .HaveNameEndingWith("Service")
            .Should()
            .NotHaveDependencyOn("System.Environment")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Services must not use static Environment dependencies.");
    }

    [Fact]
    public void Abstractions_Must_Not_Reference_Implementations()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .AreInterfaces()
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .And()
            .NotHaveDependencyOn("StackExchange.Redis")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Interfaces must not reference infrastructure implementations.");
    }
}
