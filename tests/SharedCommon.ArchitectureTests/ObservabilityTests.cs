using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace SharedCommon.ArchitectureTests;

public class ObservabilityTests
{
    [Fact]
    public void All_Services_Must_Have_ILogger_Dependency()
    {
        var serviceTypes = Types
            .InNamespace("SharedCommon")
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotAbstract()
            .And()
            .AreNotInterfaces()
            .GetTypes();

        var missingLogger = serviceTypes
            .Where(t =>
            {
                var constructors = t.GetConstructors();
                return !constructors.Any(c =>
                    c.GetParameters().Any(p =>
                        p.ParameterType.Name.Contains("ILogger")));
            })
            .ToList();

        Assert.Empty(missingLogger);
    }

    [Fact]
    public void Static_Logger_Usage_Is_Forbidden()
    {
        var result = Types
            .InNamespace("SharedCommon")
            .Should()
            .NotHaveDependencyOn("Serilog.Log")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Static Serilog.Log usage is forbidden. Use ILogger<T> via DI.");
    }
}
