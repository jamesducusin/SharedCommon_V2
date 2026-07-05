using NetArchTest.Rules;
using Xunit;

namespace SharedCommon.ArchitectureTests;

public class LayeringTests
{
    [Fact]
    public void Controllers_Must_Not_Access_Infrastructure_Directly()
    {
        var result = Types
            .InNamespace("SharedCommon.Api.Controllers")
            .Should()
            .NotHaveDependencyOn("SharedCommon.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Infrastructure_Cannot_Reference_Application_Layer()
    {
        var result = Types
            .InNamespace("SharedCommon.Infrastructure")
            .Should()
            .NotHaveDependencyOn("SharedCommon.Application")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Core_Must_Not_Reference_Other_SharedCommon_Packages()
    {
        var forbiddenPackages = new[]
        {
            "SharedCommon.Logging",
            "SharedCommon.Caching",
            "SharedCommon.Auth",
            "SharedCommon.Security",
            "SharedCommon.Messaging"
        };

        foreach (var package in forbiddenPackages)
        {
            var result = Types
                .InNamespace("SharedCommon.Core")
                .Should()
                .NotHaveDependencyOn(package)
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"SharedCommon.Core must not depend on {package}");
        }
    }

    [Fact]
    public void Middleware_Must_Not_Contain_Business_Logic()
    {
        var result = Types
            .InNamespace("SharedCommon.Middlewares")
            .That()
            .HaveNameEndingWith("Middleware")
            .Should()
            .NotHaveDependencyOn("SharedCommon.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }
}
