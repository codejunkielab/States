namespace CodeJunkie.StateChart.DiagramGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CodeJunkie.StateChart.DiagramGenerator.Models;
using CodeJunkie.StateChart.DiagramGenerator.Services;
using CodeJunkie.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Generates state chart diagrams based on annotated classes.
/// </summary>
[Generator]
public class Diagrammer : CodeJunkieGenerator, IIncrementalGenerator {
  /// <summary>
  /// Determines if a given syntax node is a candidate for state chart generation.
  /// </summary>
  public static Log Log { get; } = new Log();

  /// <summary>
  /// The name of the attribute used to mark classes as state charts.
  /// </summary>
  public ICodeService CodeService { get; } = new CodeService();

  /// <summary>
  /// Discovers and processes state chart implementations from the provided syntax tree.
  /// </summary>
  public void Initialize(IncrementalGeneratorInitializationContext context) {
    var options = context.AnalyzerConfigOptionsProvider
      .Select(
          (options, _) => {
            var disabled = options.GlobalOptions.TryGetValue(
                $"build_property.{Constants.DisableCsprojProp}",
                out var value) && value.ToLower() is "true";

            return new GenerationOptions(StateChartsDiagramGeneratorDisabled: disabled);
          });

    var stateChartCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (SyntaxNode node, CancellationToken _) =>
        IsStateChartCandidate(node),
      transform: (GeneratorSyntaxContext context, CancellationToken token) =>
        GetStateGraph(
          (ClassDeclarationSyntax)context.Node, context.SemanticModel, token))
    .Where(stateChartImplementation => stateChartImplementation is not null)
    .Combine(options)
    .Select(
      (value, token) => new GenerationData(
        Options: value.Right,
        Result: ConvertStateGraphToUml(value.Right, value.Left!, token)));

    context.RegisterImplementationSourceOutput(
      source: stateChartCandidates,
      action: (SourceProductionContext context, GenerationData data) => {
        var disabled = data.Options.StateChartsDiagramGeneratorDisabled;
        if (disabled) { return; }

        var possibleResult = data.Result;

        if (possibleResult is not StateChartOutputResult result) { return; }

        var destFile = result.FilePath;
        var content = result.Content;

        try {
          File.WriteAllText(destFile, content);
        }
        catch (Exception) {
          context.AddSource(
            hintName: $"{result.Name}.puml.g.cs",
            source: string.Join("\n", result.Content.Split('\n').Select(line => $"// {line}"))
          );
        }
      }
    );

    Log.Print("Done finding candidates");
  }

  /// <summary>
  /// Checks if the provided syntax node is a candidate for state chart generation.
  /// </summary>
  public static bool IsStateChartCandidate(SyntaxNode node) =>
    node is ClassDeclarationSyntax classDeclaration &&
    classDeclaration.AttributeLists.SelectMany(list => list.Attributes)
      .Any(attribute =>
        attribute.Name.ToString() == Constants.StateChartAttributeName &&
        attribute.ArgumentList is AttributeArgumentListSyntax argumentList &&
        argumentList.Arguments.Any(
          arg =>
            arg.NameEquals is NameEqualsSyntax nameEquals &&
            nameEquals.Name.ToString() == "Diagram" &&
            arg.Expression is LiteralExpressionSyntax literalExpression &&
            literalExpression.Token.ValueText == "true"));

  /// <summary>
  /// Retrieves the state chart implementation from the provided class declaration.
  /// </summary>
  public StateChartImplementation? GetStateGraph(ClassDeclarationSyntax stateChartClassDecl,
                                                 SemanticModel model,
                                                 CancellationToken token) {
    try {
      return DiscoverStateGraph(stateChartClassDecl, model, token);
    }
    catch (Exception e) {
      Log.Print($"Exception occurred: {e}");
      return null;
    }
  }

  /// <summary>
  /// Discovers the state graph from the provided class declaration.
  /// </summary>
  public StateChartImplementation? DiscoverStateGraph(ClassDeclarationSyntax stateChartClassDecl,
                                                      SemanticModel model,
                                                      CancellationToken token) {
    var filePath = stateChartClassDecl.SyntaxTree.FilePath;
    var destFile = Path.ChangeExtension(filePath, ".g.puml");

    Log.Print($"File path: {filePath}");
    Log.Print($"Dest file: {destFile}");

    var semanticSymbol = model.GetDeclaredSymbol(stateChartClassDecl, token);

    if (semanticSymbol is not INamedTypeSymbol symbol) {
      return null;
    }

    var concreteState = (INamedTypeSymbol)symbol
      .GetAttributes()
      .FirstOrDefault(
        attr => attr.AttributeClass?.Name ==
          Constants.StateChartAttributeNameFull
      )
      ?.ConstructorArguments[0]
      .Value!;

    var stateSubtypes = CodeService.GetAllNestedTypesRecursively(
      symbol,
      (type) => CodeService.GetAllBaseTypes(type).Any(
        (baseType) =>
        SymbolEqualityComparer.Default.Equals(baseType, concreteState) ||
        (concreteState.IsGenericType &&
         SymbolEqualityComparer.Default.Equals(baseType, concreteState.OriginalDefinition))));

    var root = new StateChartGraph(
      id: CodeService.GetNameFullyQualifiedWithoutGenerics(concreteState, concreteState.Name),
      name: concreteState.Name,
      baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(concreteState, concreteState.Name));

    var stateTypesById = new Dictionary<string, INamedTypeSymbol> {
      [root.Id] = concreteState
    };

    var stateGraphsById = new Dictionary<string, StateChartGraph> {
      [root.Id] = root
    };

    var subtypesByBaseType = new Dictionary<string, IList<INamedTypeSymbol>>();

    foreach (var subtype in stateSubtypes) {
      if (token.IsCancellationRequested) {
        return null;
      }

      var baseType = subtype.BaseType;

      if (baseType is not INamedTypeSymbol namedBaseType) {
        continue;
      }

      var baseTypeId = CodeService.GetNameFullyQualifiedWithoutGenerics(
        namedBaseType, namedBaseType.Name
      );

      if (!subtypesByBaseType.ContainsKey(baseTypeId)) {
        subtypesByBaseType[baseTypeId] = new List<INamedTypeSymbol>();
      }

      subtypesByBaseType[baseTypeId].Add(subtype);
    }

    var getInitialStateMethod = symbol.GetMembers()
      .FirstOrDefault(
          member => member is IMethodSymbol method &&
          member.Name == Constants.StateChartGetInitialState);

    HashSet<string> initialStateIds = new();

    if (getInitialStateMethod is IMethodSymbol initialStateMethod &&
        initialStateMethod.DeclaringSyntaxReferences.Select(
          (syntaxRef) =>
          syntaxRef.GetSyntax(token)).OfType<MethodDeclarationSyntax>() is
        IEnumerable<MethodDeclarationSyntax> initialStateMethodSyntaxes) {
      foreach (var initialStateMethodSyntax in initialStateMethodSyntaxes) {
        var initialStateVisitor = new ReturnTypeVisitor(
          model, token, CodeService, concreteState, symbol
        );
        initialStateVisitor.Visit(initialStateMethodSyntax);
        initialStateIds.UnionWith(initialStateVisitor.ReturnTypes);
      }
    }

    StateChartGraph buildGraph(INamedTypeSymbol type, INamedTypeSymbol baseType) {
      var typeId = CodeService.GetNameFullyQualifiedWithoutGenerics(type, type.Name);

      var graph = new StateChartGraph(
        id: typeId,
        name: type.Name,
        baseId: CodeService.GetNameFullyQualifiedWithoutGenerics(baseType, baseType.Name));

      stateTypesById[typeId] = type;
      stateGraphsById[typeId] = graph;

      var subtypes = subtypesByBaseType.ContainsKey(typeId)
        ? subtypesByBaseType[typeId]
        : new List<INamedTypeSymbol>();

      foreach (var subtype in subtypes) {
        graph.Children.Add(buildGraph(subtype, type));
      }

      return graph;
    }

    if (subtypesByBaseType.ContainsKey(root.BaseId)) {
      root.Children.AddRange(subtypesByBaseType[root.BaseId]
        .Select((stateType) => buildGraph(stateType, concreteState)));
    }

    foreach (var state in stateGraphsById.Values) {
      state.Data = GetStateGraphData(stateTypesById[state.Id], model, token, concreteState);
    }

    var implementation = new StateChartImplementation(
      FilePath: destFile,
      Id: CodeService.GetNameFullyQualified(symbol, symbol.Name),
      Name: symbol.Name,
      InitialStateIds: [.. initialStateIds],
      Graph: root,
      StatesById: stateGraphsById.ToImmutableDictionary()
    );

    Log.Print("Graph: " + implementation.Graph);

    return implementation;
  }

  /// <summary>
  /// Converts the state graph to UML format.
  /// </summary>
  public IStateChartResult ConvertStateGraphToUml(GenerationOptions options,
                                                  StateChartImplementation implementation,
                                                  CancellationToken token) {
    var sb = new StringBuilder();

    var graph = implementation.Graph;

    var transitions = new List<string>();
    foreach (
      var stateId in implementation.StatesById.OrderBy(id => id.Key)
    ) {
      var state = stateId.Value;
      foreach (
        var inputToStates in state.Data.InputToStates.OrderBy(id => id.Key)
      ) {
        var inputId = inputToStates.Key;
        foreach (var destStateId in inputToStates.Value.OrderBy(id => id)) {
          var dest = implementation.StatesById[destStateId];
          transitions.Add(
            $"{state.UmlId} --> " +
            $"{dest.UmlId} : {state.Data.Inputs[inputId].Name}"
          );
        }
      }
    }

    transitions.Sort();

    var initialStates = new List<string>();
    // State descriptions are added at the end of the document outside
    // of the state declaration. Mermaid doesn't support state descriptions
    // when they are nested inside the state, so we just flatten it out.
    //
    // In our case, we use state descriptions to show what outputs are produced
    // by the state, and when.
    var stateDescriptions = new List<string>();

    foreach (var initialStateId in implementation.InitialStateIds.OrderBy(id => id)) {
      initialStates.Add(
        "[*] --> " + implementation.StatesById[initialStateId].UmlId
      );
    }

    var states = WriteGraph(implementation.Graph, implementation, stateDescriptions, 0);

    stateDescriptions.Sort();

    var text = Format(
        $"""
        @startuml {implementation.Name}
        {states}

        {transitions}

        {stateDescriptions}

        {initialStates}
        @enduml
        """);

    return new StateChartOutputResult(
      FilePath: implementation.FilePath,
      Name: implementation.Name,
      Content: text);
  }

  /// <summary>
  /// Retrieves the state graph data for a given type.
  /// </summary>
  public StateChartGraphData GetStateGraphData(INamedTypeSymbol type,
                                               SemanticModel model,
                                               CancellationToken token,
                                               INamedTypeSymbol stateBaseType) {
    var inputsBuilder = ImmutableDictionary.CreateBuilder<string, StateChartInput>();
    var inputToStatesBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>();
    var outputsBuilder = ImmutableDictionary.CreateBuilder<IOutputContext, ImmutableHashSet<StateChartOutput>>();

    var handledInputInterfaces = type.AllInterfaces.Where(
      (interfaceType) => CodeService.GetNameFullyQualifiedWithoutGenerics(
        interfaceType, interfaceType.Name) is
        Constants.StateChartInputInterfaceId &&
        interfaceType.TypeArguments.Length == 1);

    var interfaces = new HashSet<INamedTypeSymbol>(type.Interfaces, SymbolEqualityComparer.Default);

    var syntaxNodes = type.DeclaringSyntaxReferences
      .Select(syntaxRef => syntaxRef.GetSyntax(token));

    var constructorNodes = syntaxNodes
      .SelectMany(syntaxNode => syntaxNode.ChildNodes())
      .OfType<ConstructorDeclarationSyntax>().ToList();

    var inputHandlerMethods = new List<MethodDeclarationSyntax>();

    var outputVisitor = new OutputVisitor(model, token, CodeService, OutputContexts.None);
    foreach (var constructor in constructorNodes) {
      outputVisitor.Visit(constructor);
    }
    outputsBuilder.AddRange(outputVisitor.OutputTypes);

    foreach (var handledInputInterface in handledInputInterfaces) {
      var interfaceMembers = handledInputInterface.GetMembers();
      var inputTypeSymbol = handledInputInterface.TypeArguments[0];
      if (inputTypeSymbol is not INamedTypeSymbol inputType) {
        continue;
      }
      if (interfaceMembers.Length == 0) { continue; }
      var implementation = type.FindImplementationForInterfaceMember(
        interfaceMembers[0]
      );
      if (implementation is not IMethodSymbol methodSymbol) {
        continue;
      }

      var onTypeItself = interfaces.Contains(handledInputInterface);

      if (!onTypeItself) {
        methodSymbol = type.GetMembers()
          .OfType<IMethodSymbol>()
          .FirstOrDefault(
              member => SymbolEqualityComparer.Default.Equals(
                member.OverriddenMethod, methodSymbol));

        if (methodSymbol is null) {
          continue;
        }
      }

      var handlerMethodSyntaxes = methodSymbol
        .DeclaringSyntaxReferences
        .Select(syntaxRef => syntaxRef.GetSyntax(token))
        .OfType<MethodDeclarationSyntax>()
        .ToImmutableArray();

      foreach (var methodSyntax in handlerMethodSyntaxes) {
        inputHandlerMethods.Add(methodSyntax);
        var inputId = CodeService.GetNameFullyQualifiedWithoutGenerics(
          inputType, inputType.Name
        );
        var outputContext = OutputContexts.OnInput(inputType.Name);
        var modelForSyntax = model.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
        var returnTypeVisitor = new ReturnTypeVisitor(
          modelForSyntax, token, CodeService, stateBaseType, type);
        outputVisitor = new OutputVisitor(
          modelForSyntax, token, CodeService, outputContext
        );

        returnTypeVisitor.Visit(methodSyntax);
        outputVisitor.Visit(methodSyntax);

        if (outputVisitor.OutputTypes.ContainsKey(outputContext)) {
          outputsBuilder.Add(outputContext, outputVisitor.OutputTypes[outputContext]);
        }

        inputsBuilder.Add(
            inputId,
            new StateChartInput(Id: inputId, Name: inputType.Name));

        inputToStatesBuilder.Add(inputId, returnTypeVisitor.ReturnTypes);
      }
    }

    var allOtherMethods = syntaxNodes
      .SelectMany(syntaxNode => syntaxNode.ChildNodes())
      .OfType<MethodDeclarationSyntax>()
      .Where(methodSyntax => !inputHandlerMethods.Contains(methodSyntax));

    foreach (var otherMethod in allOtherMethods) {
      Log.Print("Examining method: " + otherMethod.Identifier.Text);
      var outputContext = OutputContexts.Method(otherMethod.Identifier.Text);

      var modelForSyntax = model.Compilation.GetSemanticModel(otherMethod.SyntaxTree);

      outputVisitor = new OutputVisitor(modelForSyntax, token, CodeService, outputContext);
      outputVisitor.Visit(otherMethod);

      if (outputVisitor.OutputTypes.ContainsKey(outputContext)) {
        outputsBuilder.Add(outputContext, outputVisitor.OutputTypes[outputContext]);
      }
    }

    var inputs = inputsBuilder.ToImmutable();

    var inputToStates = inputToStatesBuilder.ToImmutable();

    foreach (var input in inputToStates.Keys) {
      Log.Print(
        $"{type.Name} + {input.Split('.').Last()} -> " +
        $"{string.Join(", ", inputToStates[input].Select(s => s.Split('.').Last()))}");
    }

    var outputs = outputsBuilder.ToImmutable();

    return new StateChartGraphData(
      Inputs: inputs,
      InputToStates: inputToStates,
      Outputs: outputs);
  }

  private IEnumerable<string> WriteGraph(StateChartGraph graph,
                                         StateChartImplementation impl,
                                         List<string> stateDescriptions,
                                         int t) {
    var lines = new List<string>();

    var isMultilineState = graph.Children.Count > 0;

    var isRoot = graph == impl.Graph;

    if (isMultilineState) {
      if (isRoot) {
        lines.Add($"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId} {{");
      }
      else {
        lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId} {{");
      }
    }
    else if (isRoot) {
      lines.Add($"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId}");
    }
    else {
      lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId}");
    }

    foreach (var child in graph.Children.OrderBy(child => child.Name)) {
      lines.AddRange(WriteGraph(child, impl, stateDescriptions, t + 1));
    }

    foreach (var outputContext in graph.Data.Outputs.Keys.OrderBy(key => key.DisplayName)) {
      var outputs = graph.Data.Outputs[outputContext]
        .Select(output => output.Name)
        .OrderBy(output => output);
      var line = string.Join(", ", outputs);
      stateDescriptions.Add($"{graph.UmlId} : {outputContext.DisplayName} → {line}");
    }

    if (isMultilineState) { lines.Add($"{Tab(t)}}}"); }
    return lines;
  }
}
