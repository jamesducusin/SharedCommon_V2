using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace SharedCommon.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoStaticMutableStateAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SC0002",
        title: "Static mutable state detected",
        messageFormat: "Static mutable field '{0}' is forbidden. Use dependency injection instead.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Static mutable state causes threading issues and hinders testability. See CLAUDE.md.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;

        var isStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword);
        var isReadonly = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
        var isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);

        if (isStatic && !isReadonly && !isConst)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.Text));
            }
        }
    }
}
