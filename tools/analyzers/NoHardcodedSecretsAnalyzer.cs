using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace SharedCommon.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoHardcodedSecretsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SC0001",
        title: "Hardcoded secret detected",
        messageFormat: "Potential hardcoded secret in '{0}'. Use User Secrets or environment variables.",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Secrets must not be hardcoded. See docs/standards/security-guidelines.md.");

    private static readonly Regex SecretPattern = new(
        @"(password|secret|apikey|api_key|connectionstring|bearer)\s*=\s*""[^""]{4,}""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (SecretPattern.IsMatch(value))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rule, literal.GetLocation(), value[..Math.Min(20, value.Length)]));
        }
    }
}
