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
/// Local copy of DiagramFormat enum for source generator independence.
/// Must match the definition in CodeJunkie.StateChart.
/// </summary>
[Flags]
internal enum DiagramFormat {
  None = 0,
  PlantUML = 1,
  Mermaid = 2,
  Markdown = 4,
  All = PlantUML | Mermaid | Markdown
}

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
      transform: (GeneratorSyntaxContext context, CancellationToken token) => {
        var implementation = GetStateGraph(
          (ClassDeclarationSyntax)context.Node, context.SemanticModel, token);
        if (implementation == null) return (implementation: (StateChartImplementation?)null, formats: DiagramFormat.None);
        
        var symbol = context.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)context.Node, token);
        if (symbol is not INamedTypeSymbol namedSymbol) return (implementation: (StateChartImplementation?)null, formats: DiagramFormat.None);
        
        var formats = GetDiagramFormats(namedSymbol);
        return (implementation, formats);
      })
    .Where(result => result.implementation is not null)
    .Combine(options)
    .Select(
      (value, token) => {
        var (implementation, formats) = value.Left;
        var results = GenerateDiagrams(value.Right, implementation!, formats, token);
        return new GenerationData(Results: results, Options: value.Right);
      });

    context.RegisterImplementationSourceOutput(
      source: stateChartCandidates,
      action: (SourceProductionContext context, GenerationData data) => {
        var disabled = data.Options.StateChartsDiagramGeneratorDisabled;
        if (disabled) { return; }

        foreach (var result in data.Results) {
          var (filePath, name, content, extension) = result switch {
            StateChartOutputResult r => (r.FilePath, r.Name, r.Content, ".puml"),
            MermaidOutputResult r => (r.FilePath, r.Name, r.Content, ".mermaid"),
            MarkdownOutputResult r => (r.FilePath, r.Name, r.Content, ".md"),
            _ => (null, null, null, null)
          };

          if (filePath == null || content == null) continue;

          try {
            File.WriteAllText(filePath, content);
          }
          catch (Exception) {
            context.AddSource(
              hintName: $"{name}{extension}.g.cs",
              source: string.Join("\n", content.Split('\n').Select(line => $"// {line}"))
            );
          }
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
        (argumentList.Arguments.Any(
          arg =>
            arg.NameEquals is NameEqualsSyntax nameEquals &&
            nameEquals.Name.ToString() == "Diagram" &&
            arg.Expression is LiteralExpressionSyntax literalExpression &&
            literalExpression.Token.ValueText == "true") ||
         argumentList.Arguments.Any(
          arg =>
            arg.NameEquals is NameEqualsSyntax nameEquals &&
            nameEquals.Name.ToString() == "DiagramFormats")));

  /// <summary>
  /// Gets the requested diagram formats from the StateChart attribute.
  /// </summary>
  private static DiagramFormat GetDiagramFormats(INamedTypeSymbol symbol) {
    var attribute = symbol.GetAttributes()
      .FirstOrDefault(attr => attr.AttributeClass?.Name == Constants.StateChartAttributeNameFull);
    
    if (attribute == null) {
      return DiagramFormat.None;
    }
    
    // Check for DiagramFormats property
    var diagramFormatsArg = attribute.NamedArguments
      .FirstOrDefault(arg => arg.Key == "DiagramFormats");
    
    if (diagramFormatsArg.Value.Value != null) {
      return (DiagramFormat)(int)diagramFormatsArg.Value.Value;
    }
    
    // Fall back to Diagram property for backward compatibility
    var diagramArg = attribute.NamedArguments
      .FirstOrDefault(arg => arg.Key == "Diagram");
    
    if (diagramArg.Value.Value is bool diagram && diagram) {
      return DiagramFormat.PlantUML;
    }
    
    return DiagramFormat.None;
  }

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
    var baseFilePath = Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar + 
                       Path.GetFileNameWithoutExtension(filePath);

    Log.Print($"File path: {filePath}");
    Log.Print($"Base file path: {baseFilePath}");

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
      FilePath: baseFilePath,
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
  /// Generates all requested diagram formats based on the StateChart implementation.
  /// </summary>
  private IReadOnlyList<IStateChartResult> GenerateDiagrams(GenerationOptions options,
                                                           StateChartImplementation implementation,
                                                           DiagramFormat formats,
                                                           CancellationToken token) {
    var results = new List<IStateChartResult>();
    
    if (formats.HasFlag(DiagramFormat.PlantUML)) {
      results.Add(ConvertStateGraphToUml(options, implementation, token));
    }
    
    if (formats.HasFlag(DiagramFormat.Mermaid)) {
      results.Add(ConvertStateGraphToMermaid(options, implementation, token));
    }
    
    if (formats.HasFlag(DiagramFormat.Markdown)) {
      results.Add(ConvertStateGraphToMarkdown(options, implementation, token));
    }
    
    return results;
  }

  /// <summary>
  /// Computes a deterministic hash code for a string using DJB2 algorithm.
  /// This ensures consistent hash values across different .NET runtime versions.
  /// </summary>
  private static int GetDeterministicHashCode(string str) {
    unchecked {
      int hash = 5381;
      foreach (char c in str) {
        hash = ((hash << 5) + hash) + c; // hash * 33 + c
      }
      return hash;
    }
  }

  /// <summary>
  /// Generates a color palette for states based on their IDs.
  /// Uses deterministic hash-based generation for consistency across regenerations.
  /// </summary>
  private Dictionary<string, string> GenerateStateColors(StateChartImplementation implementation) {
    var colors = new Dictionary<string, string>();
    
    // Predefined color palette with good contrast and visibility
    var colorPalette = new[] {
      "#FF6B6B", // Red
      "#4ECDC4", // Teal
      "#45B7D1", // Blue
      "#96CEB4", // Green
      "#FFEAA7", // Yellow
      "#DDA0DD", // Plum
      "#98D8C8", // Mint
      "#FFB6C1", // Pink
      "#87CEEB", // Sky Blue
      "#F4A460", // Sandy Brown
      "#B19CD9", // Purple
      "#90EE90", // Light Green
      "#FFE4B5", // Moccasin
      "#E6B0AA", // Rose
      "#85C1E2", // Light Blue
      "#F9E79F", // Pale Yellow
      "#D5A6BD", // Dusty Rose
      "#A9DFBF", // Pale Green
      "#F8C471", // Light Orange
      "#AED6F1"  // Powder Blue
    };
    
    var stateIds = implementation.StatesById.Keys.OrderBy(id => id).ToList();
    
    for (int i = 0; i < stateIds.Count; i++) {
      var stateId = stateIds[i];
      // Use deterministic hash to ensure consistent colors across different runtime versions
      var colorIndex = Math.Abs(GetDeterministicHashCode(stateId)) % colorPalette.Length;
      colors[stateId] = colorPalette[colorIndex];
    }
    
    return colors;
  }

  /// <summary>
  /// Converts the state graph to UML format.
  /// </summary>
  public IStateChartResult ConvertStateGraphToUml(GenerationOptions options,
                                                  StateChartImplementation implementation,
                                                  CancellationToken token) {
    var stateColors = GenerateStateColors(implementation);
    var transitions = CollectTransitions(implementation);
    var stateOutputs = CollectStateOutputs(implementation.Graph, stateColors);
    
    var transitionLines = transitions
      .Select(t => {
        // Find the color for the source state
        var fromStateEntry = implementation.StatesById.FirstOrDefault(s => s.Value.UmlId == t.FromStateId);
        if (fromStateEntry.Value != null && stateColors.TryGetValue(fromStateEntry.Key, out var color)) {
          return $"{t.FromStateId} -[{color}]-> {t.ToStateId} : {t.InputName}";
        }
        return $"{t.FromStateId} --> {t.ToStateId} : {t.InputName}";
      })
      .OrderBy(t => t)
      .ToList();

    var initialStates = implementation.InitialStateIds
      .OrderBy(id => id)
      .Select(id => "[*] --> " + implementation.StatesById[id].UmlId)
      .ToList();

    var stateDescriptions = new List<string>();
    var states = WriteGraphWithColors(implementation.Graph, implementation, stateDescriptions, stateColors, 0);

    var outputDescriptions = stateOutputs
      .Select(o => {
        if (!string.IsNullOrEmpty(o.Color)) {
          return $"{o.StateId} : <color:{o.Color}>{o.Context} → {o.Outputs}</color>";
        }
        return $"{o.StateId} : {o.Context} → {o.Outputs}";
      })
      .OrderBy(o => o)
      .ToList();

    var text = Format(
        $"""
        @startuml {implementation.Name}
        {states}

        {transitionLines}

        {outputDescriptions}

        {initialStates}
        @enduml
        """);

    var filePath = implementation.FilePath + ".g.puml";
    return new StateChartOutputResult(
      FilePath: filePath,
      Name: implementation.Name,
      Content: text);
  }

  /// <summary>
  /// Converts the state graph to Mermaid format.
  /// </summary>
  public IStateChartResult ConvertStateGraphToMermaid(GenerationOptions options,
                                                      StateChartImplementation implementation,
                                                      CancellationToken token) {
    var transitions = CollectTransitions(implementation);
    var stateOutputs = CollectStateOutputs(implementation.Graph, null);
    
    var transitionLines = transitions
      .Select(t => $"    {t.FromStateId} --> {t.ToStateId} : {t.InputName}")
      .OrderBy(t => t)
      .ToList();

    var initialStates = implementation.InitialStateIds
      .OrderBy(id => id)
      .Select(id => $"    [*] --> {implementation.StatesById[id].UmlId}")
      .ToList();

    var mermaidStates = WriteMermaidGraph(implementation.Graph, implementation, 1);

    // Mermaid supports state descriptions
    var outputDescriptions = stateOutputs
      .Select(o => $"    {o.StateId} : {o.Context} → {o.Outputs}")
      .OrderBy(o => o)
      .ToList();

    var text = Format(
        $"""
        ```mermaid
        stateDiagram-v2
        {mermaidStates}
        {initialStates}
        {transitionLines}
        {outputDescriptions}
        ```
        """);

    var filePath = implementation.FilePath + ".g.mermaid";
    return new MermaidOutputResult(
      FilePath: filePath,
      Name: implementation.Name,
      Content: text);
  }

  /// <summary>
  /// Writes the state graph in Mermaid format.
  /// </summary>
  private IEnumerable<string> WriteMermaidGraph(StateChartGraph graph,
                                                StateChartImplementation impl,
                                                int indentLevel) {
    var lines = new List<string>();
    var indent = new string(' ', indentLevel * 4);
    
    var isRoot = graph == impl.Graph;
    var hasChildren = graph.Children.Count > 0;
    
    if (hasChildren) {
      if (isRoot) {
        lines.Add($"{indent}state \"{impl.Name} State\" as {graph.UmlId} {{");
      } else {
        lines.Add($"{indent}state \"{graph.Name}\" as {graph.UmlId} {{");
      }
      
      foreach (var child in graph.Children.OrderBy(child => child.Name)) {
        lines.AddRange(WriteMermaidGraph(child, impl, indentLevel + 1));
      }
      
      lines.Add($"{indent}}}");
    } else {
      if (isRoot) {
        lines.Add($"{indent}state \"{impl.Name} State\" as {graph.UmlId}");
      } else {
        lines.Add($"{indent}state \"{graph.Name}\" as {graph.UmlId}");
      }
    }
    
    return lines;
  }

  /// <summary>
  /// Converts the state graph to Markdown documentation format.
  /// </summary>
  public IStateChartResult ConvertStateGraphToMarkdown(GenerationOptions options,
                                                       StateChartImplementation implementation,
                                                       CancellationToken token) {
    var transitions = CollectTransitions(implementation);
    var stateOutputs = CollectStateOutputs(implementation.Graph, null);
    
    var sb = new StringBuilder();
    
    // Header
    sb.AppendLine($"# {implementation.Name} State Chart");
    sb.AppendLine();
    sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine();
    
    // State Hierarchy
    sb.AppendLine("## State Hierarchy");
    sb.AppendLine();
    WriteMarkdownStateHierarchy(implementation.Graph, implementation, sb, 0);
    sb.AppendLine();
    
    // Initial States
    if (implementation.InitialStateIds.Any()) {
      sb.AppendLine("## Initial States");
      sb.AppendLine();
      foreach (var initialStateId in implementation.InitialStateIds.OrderBy(id => id)) {
        var state = implementation.StatesById[initialStateId];
        sb.AppendLine($"- **{state.Name}**");
      }
      sb.AppendLine();
    }
    
    // Transitions Table
    if (transitions.Any()) {
      sb.AppendLine("## State Transitions");
      sb.AppendLine();
      sb.AppendLine("| From State | Input | To State |");
      sb.AppendLine("|------------|-------|----------|");
      foreach (var transition in transitions.OrderBy(t => t.FromStateId).ThenBy(t => t.InputName)) {
        var fromState = implementation.StatesById.First(s => s.Value.UmlId == transition.FromStateId).Value;
        var toState = implementation.StatesById.First(s => s.Value.UmlId == transition.ToStateId).Value;
        sb.AppendLine($"| {fromState.Name} | {transition.InputName} | {toState.Name} |");
      }
      sb.AppendLine();
    }
    
    // Outputs Table
    if (stateOutputs.Any()) {
      sb.AppendLine("## State Outputs");
      sb.AppendLine();
      sb.AppendLine("| State | Context | Outputs |");
      sb.AppendLine("|-------|---------|---------|");
      foreach (var output in stateOutputs.OrderBy(o => o.StateId).ThenBy(o => o.Context)) {
        var state = implementation.StatesById.First(s => s.Value.UmlId == output.StateId).Value;
        sb.AppendLine($"| {state.Name} | {output.Context} | {output.Outputs} |");
      }
      sb.AppendLine();
    }
    
    // Mermaid Diagram
    sb.AppendLine("## State Diagram");
    sb.AppendLine();
    
    // Generate Mermaid content without the markdown code fence
    var mermaidResult = ConvertStateGraphToMermaid(options, implementation, token) as MermaidOutputResult;
    if (mermaidResult != null) {
      // The mermaid content already has the code fence, so just append it
      sb.Append(mermaidResult.Content);
    }
    
    var filePath = implementation.FilePath + ".g.md";
    return new MarkdownOutputResult(
      FilePath: filePath,
      Name: implementation.Name,
      Content: sb.ToString());
  }

  /// <summary>
  /// Writes the state hierarchy in Markdown format.
  /// </summary>
  private void WriteMarkdownStateHierarchy(StateChartGraph graph,
                                          StateChartImplementation impl,
                                          StringBuilder sb,
                                          int level) {
    var indent = new string(' ', level * 2);
    var bullet = level == 0 ? "-" : "-";
    
    var isRoot = graph == impl.Graph;
    var displayName = isRoot ? $"{impl.Name} State" : graph.Name;
    
    sb.AppendLine($"{indent}{bullet} **{displayName}**");
    
    // Add outputs if any
    if (graph.Data.Outputs.Any()) {
      foreach (var outputContext in graph.Data.Outputs.Keys.OrderBy(key => key.DisplayName)) {
        var outputs = graph.Data.Outputs[outputContext]
          .Select(output => output.Name)
          .OrderBy(output => output);
        var outputList = string.Join(", ", outputs);
        sb.AppendLine($"{indent}  - {outputContext.DisplayName}: {outputList}");
      }
    }
    
    // Recursively write children
    foreach (var child in graph.Children.OrderBy(child => child.Name)) {
      WriteMarkdownStateHierarchy(child, impl, sb, level + 1);
    }
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

  /// <summary>
  /// Common data structures for state chart diagram generation.
  /// </summary>
  private record StateTransition(string FromStateId, string ToStateId, string InputName);
  
  private record StateOutput(string StateId, string Context, string Outputs, string? Color = null);

  /// <summary>
  /// Collects all transitions from the state chart implementation.
  /// </summary>
  private List<StateTransition> CollectTransitions(StateChartImplementation implementation) {
    var transitions = new List<StateTransition>();
    
    foreach (var stateId in implementation.StatesById.OrderBy(id => id.Key)) {
      var state = stateId.Value;
      foreach (var inputToStates in state.Data.InputToStates.OrderBy(id => id.Key)) {
        var inputId = inputToStates.Key;
        foreach (var destStateId in inputToStates.Value.OrderBy(id => id)) {
          transitions.Add(new StateTransition(
            state.UmlId,
            implementation.StatesById[destStateId].UmlId,
            state.Data.Inputs[inputId].Name
          ));
        }
      }
    }
    
    return transitions;
  }

  /// <summary>
  /// Collects all state outputs from the state chart implementation.
  /// </summary>
  private List<StateOutput> CollectStateOutputs(StateChartGraph graph, Dictionary<string, string>? stateColors = null, List<StateOutput>? outputs = null) {
    outputs ??= new List<StateOutput>();
    
    foreach (var outputContext in graph.Data.Outputs.Keys.OrderBy(key => key.DisplayName)) {
      var outputNames = graph.Data.Outputs[outputContext]
        .Select(output => output.Name)
        .OrderBy(output => output);
      var line = string.Join(", ", outputNames);
      var color = stateColors?.TryGetValue(graph.Id, out var stateColor) == true ? stateColor : null;
      outputs.Add(new StateOutput(graph.UmlId, outputContext.DisplayName, line, color));
    }
    
    foreach (var child in graph.Children.OrderBy(child => child.Name)) {
      CollectStateOutputs(child, stateColors, outputs);
    }
    
    return outputs;
  }

  private IEnumerable<string> WriteGraphWithColors(StateChartGraph graph,
                                                   StateChartImplementation impl,
                                                   List<string> stateDescriptions,
                                                   Dictionary<string, string> stateColors,
                                                   int t) {
    var lines = new List<string>();

    var isMultilineState = graph.Children.Count > 0;
    var isRoot = graph == impl.Graph;
    
    // Get color for this state
    var color = stateColors.TryGetValue(graph.Id, out var stateColor) ? $" {stateColor}" : "";

    if (isMultilineState) {
      if (isRoot) {
        lines.Add($"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId}{color} {{");
      }
      else {
        lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId}{color} {{");
      }
    }
    else if (isRoot) {
      lines.Add($"{Tab(t)}state \"{impl.Name} State\" as {graph.UmlId}{color}");
    }
    else {
      lines.Add($"{Tab(t)}state \"{graph.Name}\" as {graph.UmlId}{color}");
    }

    foreach (var child in graph.Children.OrderBy(child => child.Name)) {
      lines.AddRange(WriteGraphWithColors(child, impl, stateDescriptions, stateColors, t + 1));
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
