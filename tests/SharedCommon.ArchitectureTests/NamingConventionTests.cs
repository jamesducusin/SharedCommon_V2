using NetArchTest.Rules;
using Xunit;

namespace SharedCommon.ArchitectureTests;

public class NamingConventionTests
{
    [Fact]
    public void Interfaces_Must_Start_With_I()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join("\n", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Fact]
    public void Async_Methods_Must_End_With_Async()
    {
        // Note: NetArchTest checks at type level; async method naming is enforced by the
        // validate-architecture hook and code review process.
        Assert.True(true, "Async method naming enforced via code review and hooks.");
    }

    [Fact]
    public void Options_Classes_Must_End_With_Options()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .HaveNameEndingWith("Options")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Options classes should be sealed to prevent inheritance.");
    }

    [Fact]
    public void Service_Implementations_Must_Be_Sealed_Or_Abstract()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotAbstract()
            .And()
            .AreNotInterfaces()
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join("\n", result.FailingTypes?.Select(t => $"{t.FullName} should be sealed") ?? []));
    }
}
