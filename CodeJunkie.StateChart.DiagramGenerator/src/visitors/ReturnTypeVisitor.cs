namespace CodeJunkie.StateChart.DiagramGenerator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using CodeJunkie.StateChart.DiagramGenerator.Services;
using CodeJunkie.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Visitor for analyzing return types in state chart classes.
/// </summary>
public class ReturnTypeVisitor : CSharpSyntaxWalker {
  /// <summary>
  /// The semantic model for the current syntax tree.
  /// </summary>
  public SemanticModel Model { get; }

  /// <summary>
  /// The cancellation token for the current operation.
  /// </summary>
  public CancellationToken Token { get; }

  /// <summary>
  /// The code service for generating code.
  /// </summary>
  public ICodeService CodeService { get; }

  /// <summary>
  /// The base type of the state.
  /// </summary>
  public INamedTypeSymbol StateBaseType { get; }

  /// <summary>
  /// Type of the current state.
  /// </summary>
  public INamedTypeSymbol StateType { get; }

  private readonly HashSet<string> _returnTypes = new();

  /// <summary>
  /// Gets the return types for the current state chart.
  /// </summary>
  public ImmutableHashSet<string> ReturnTypes => [.. _returnTypes];

  /// <summary>
  /// Initializes a new instance of the <see cref="ReturnTypeVisitor"/> class.
  /// </summary>
  public ReturnTypeVisitor(SemanticModel model,
                           CancellationToken token,
                           ICodeService codeService,
                           INamedTypeSymbol stateBaseType,
                           INamedTypeSymbol stateType) {
    Model = model;
    Token = token;
    CodeService = codeService;
    StateBaseType = stateBaseType;
    StateType = stateType;
  }

  /// <summary>
  /// Visits a method declaration and analyzes its return type.
  /// </summary>
  public override void VisitReturnStatement(ReturnStatementSyntax node) =>
    AddExpressionToReturnTypes(node.Expression);

  /// <summary>
  /// Visits an arrow expression clause and analyzes its return type.
  /// </summary>
  public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node) =>
    AddExpressionToReturnTypes(node.Expression);

  private void AddExpressionToReturnTypes(ExpressionSyntax? expression) {
    if (expression is not ExpressionSyntax expressionSyntax) {
      return;
    }

    if (expression is ConditionalExpressionSyntax conditional) {
      AddExpressionToReturnTypes(conditional.WhenTrue);
      AddExpressionToReturnTypes(conditional.WhenFalse);
      return;
    }

    if (expression is SwitchExpressionSyntax @switch) {
      foreach (var arm in @switch.Arms) {
        AddExpressionToReturnTypes(arm.Expression);
      }

      return;
    }

    if (expression is BinaryExpressionSyntax binary) {
      AddExpressionToReturnTypes(binary.Left);
      AddExpressionToReturnTypes(binary.Right);
      return;
    }

    if (expression is MemberAccessExpressionSyntax memberAccess) {
      AddExpressionToReturnTypes(memberAccess.Expression);
      return;
    }

    ITypeSymbol? type = default;

    if (expression is InvocationExpressionSyntax invocation) {
      if (invocation.Expression is GenericNameSyntax generic &&
          generic.Identifier.Text == "To" &&
          generic.TypeArgumentList.Arguments.Count == 1) {
        var genericType = generic.TypeArgumentList.Arguments[0];
        type = GetModel(genericType).GetTypeInfo(genericType, Token).Type;
      }
      else if (invocation.Expression is IdentifierNameSyntax id &&
               id.Identifier.Text == "ToSelf") {
        type = StateType;
      }
      else {
        AddExpressionToReturnTypes(invocation.Expression);
        return;
      }
    }

    if (type is not ITypeSymbol typeSymbol) {
      return;
    }

    if (!type.InheritsFromOrEquals(StateBaseType)) {
      return;
    }

    var returnTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        typeSymbol, typeSymbol.Name);

    _returnTypes.Add(returnTypeId);
  }

  private SemanticModel GetModel(SyntaxNode node) =>
    Model.Compilation.GetSemanticModel(node.SyntaxTree);
}
