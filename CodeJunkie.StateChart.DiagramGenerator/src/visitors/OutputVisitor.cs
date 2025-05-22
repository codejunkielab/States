namespace CodeJunkie.StateChart.DiagramGenerator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using CodeJunkie.StateChart.DiagramGenerator.Models;
using CodeJunkie.StateChart.DiagramGenerator.Services;
using CodeJunkie.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Visitor for analyzing output types in state chart classes.
/// </summary>
public class OutputVisitor : CSharpSyntaxWalker {
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

  private readonly ImmutableDictionary<IOutputContext, HashSet<StateChartOutput>>.Builder _outputTypes =
    ImmutableDictionary.CreateBuilder<IOutputContext, HashSet<StateChartOutput>>();
  private readonly Stack<IOutputContext> _outputContexts = new();
  private IOutputContext OutputContext => _outputContexts.Peek();

  /// <summary>
  /// Gets the output types for the current state chart.
  /// </summary>
  public ImmutableDictionary<IOutputContext, ImmutableHashSet<StateChartOutput>> OutputTypes =>
    _outputTypes.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableHashSet());

  /// <summary>
  /// Initializes a new instance of the <see cref="OutputVisitor"/> class.
  /// </summary>
  /// <param name="model">The semantic model for the current syntax tree.</param>
  /// <param name="token">The cancellation token for the current operation.</param>
  /// <param name="service">The code service for generating code.</param>
  /// <param name="startContext">The initial output context.</param>
  public OutputVisitor(SemanticModel model,
                       CancellationToken token,
                       ICodeService service,
                       IOutputContext startContext) {
    Model = model;
    Token = token;
    CodeService = service;
    _outputContexts.Push(startContext);
  }

  /// <summary>
  /// Visits a method declaration and processes its body.
  /// </summary>
  public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
    void pushContext(IOutputContext context) {
      _outputContexts.Push(context);
      var pushedContext = true;

      base.VisitInvocationExpression(node);

      if (pushedContext) {
        _outputContexts.Pop();
      }
    }

    if (node.Expression is not MemberAccessExpressionSyntax memberAccess) {
      var methodName = "";

      var id = node.Expression;
      if (id is not IdentifierNameSyntax identifierName) {
        base.VisitInvocationExpression(node);
        return;
      }

      methodName = identifierName.Identifier.ValueText;

      if (methodName != Constants.StateChartStateOutput) {
        base.VisitInvocationExpression(node);
        return;
      }

      var args = node.ArgumentList.Arguments;

      if (args.Count != 1) {
        base.VisitInvocationExpression(node);
        return;
      }

      var rhs = node.ArgumentList.Arguments[0].Expression;
      var rhsType = GetModel(rhs).GetTypeInfo(rhs, Token).Type;

      if (rhsType is null) {
        base.VisitInvocationExpression(node);
        return;
      }

      var rhsTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        rhsType, rhsType.Name
      );

      AddOutput(rhsTypeId, rhsType.Name);

      return;
    }

    if (memberAccess.Expression is ThisExpressionSyntax) {
      if (
        memberAccess.Name.Identifier.ValueText is
          Constants.StateChartStateLogicOnEnter
      ) {
        pushContext(OutputContexts.OnEnter);
        return;
      }

      if (
        memberAccess.Name.Identifier.ValueText is
          Constants.StateChartStateLogicOnExit
      ) {
        pushContext(OutputContexts.OnExit);
      }
    }
  }

  /// <summary>
  /// Visits a class declaration and processes its members.
  /// </summary>
  public override void VisitClassDeclaration(ClassDeclarationSyntax node) { }

  /// <summary>
  /// Visits a struct declaration and processes its members.
  /// </summary>
  public override void VisitStructDeclaration(StructDeclarationSyntax node) { }

  private void AddOutput(string id, string name) {
    if (!_outputTypes.TryGetValue(OutputContext, out var outputs)) {
      outputs = new HashSet<StateChartOutput>();
      _outputTypes.Add(OutputContext, outputs);
    }

    outputs.Add(new StateChartOutput(id, name));
  }

  private SemanticModel GetModel(SyntaxNode node) =>
    Model.Compilation.GetSemanticModel(node.SyntaxTree);
}
