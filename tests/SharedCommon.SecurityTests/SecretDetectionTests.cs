using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SharedCommon.SecurityTests;

public partial class SecretDetectionTests
{
    [GeneratedRegex(@"(password|secret|apikey|api_key)\s*=\s*""[^""]+""|bearer\s+[a-zA-Z0-9\-_\.]{20,}", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPattern();

    [Fact]
    public void No_Hardcoded_Secrets_In_Source_Assemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("SharedCommon") == true)
            .ToList();

        var violations = new List<string>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (field.IsLiteral && field.FieldType == typeof(string))
                    {
                        var value = field.GetRawConstantValue()?.ToString() ?? "";
                        if (SecretPattern().IsMatch(value))
                        {
                            violations.Add($"{type.FullName}.{field.Name}: potential secret in constant");
                        }
                    }
                }
            }
        }

        Assert.Empty(violations);
    }
}
