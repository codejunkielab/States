namespace CodeJunkie.StateChart.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using CodeJunkie.StateChart.Analyzers.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StateChartAttributeAnalyzer : DiagnosticAnalyzer {
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
    get;
  } = ImmutableArray.Create(Diagnostics.MissingStateChartAttributeDescriptor);

  public override void Initialize(AnalysisContext context) {
    context.EnableConcurrentExecution();

    context.ConfigureGeneratedCodeAnalysis(
      GeneratedCodeAnalysisFlags.Analyze |
      GeneratedCodeAnalysisFlags.ReportDiagnostics
    );

    context.RegisterSyntaxNodeAction(
      AnalyzeClassDeclaration,
      SyntaxKind.ClassDeclaration
    );
  }

  private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
    var classDeclaration = (ClassDeclarationSyntax)context.Node;

    if (
      !(
        classDeclaration.BaseList?.Types.FirstOrDefault()?.Type
        is GenericNameSyntax genericName &&
        genericName.Identifier.ValueText.EndsWith("StateChart")
      )
    ) {
      // Only analyze types that appear to be state charts.
      return;
    }

    var attributes = classDeclaration.AttributeLists.SelectMany(
      list => list.Attributes
    ).Where(
      attribute => attribute.Name.ToString() == "StateChart"
    );

    if (!attributes.Any()) {
      context.ReportDiagnostic(
        Diagnostics.MissingStateChartAttribute(
          classDeclaration.GetLocation(),
          classDeclaration.Identifier.ValueText
        )
      );
    }
  }
}
