using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SharedCommon.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObservabilityAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ConsoleWriteRule = new(
        id: "SC0003",
        title: "Console.Write* detected",
        messageFormat: "Console.Write* is forbidden in '{0}'. Use ILogger<T> instead.",
        category: "Observability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Use structured logging via ILogger. See docs/standards/logging-guidelines.md.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ConsoleWriteRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var objectName = memberAccess.Expression.ToString();
            var methodName = memberAccess.Name.Identifier.Text;

            if (objectName == "Console" && (methodName is "Write" or "WriteLine" or "Error"))
            {
                var containingMethod = invocation.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault()?.Identifier.Text ?? "unknown";

                context.ReportDiagnostic(
                    Diagnostic.Create(ConsoleWriteRule, invocation.GetLocation(), containingMethod));
            }
        }
    }
}
