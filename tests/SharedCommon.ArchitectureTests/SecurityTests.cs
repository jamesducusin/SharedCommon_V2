using NetArchTest.Rules;
using Xunit;

namespace SharedCommon.ArchitectureTests;

public class SecurityTests
{
    [Fact]
    public void No_Static_Mutable_State_In_Services()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .HaveNameEndingWith("Service")
            .Should()
            .NotHaveDependencyOn("System.Collections.Concurrent.ConcurrentDictionary`2")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Services must not use static mutable state. Use DI-scoped or singleton services instead.");
    }

    [Fact]
    public void Auth_Package_Must_Not_Depend_On_Caching()
    {
        var result = Types
            .InNamespace("SharedCommon.Auth")
            .Should()
            .NotHaveDependencyOn("SharedCommon.Caching")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "SharedCommon.Auth must not directly depend on SharedCommon.Caching.");
    }

    [Fact]
    public void Security_Package_Must_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InNamespace("SharedCommon.Security")
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "SharedCommon.Security must not reference EF Core.");
    }
}
