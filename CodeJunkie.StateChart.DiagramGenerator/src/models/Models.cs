namespace CodeJunkie.StateChart.DiagramGenerator.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CodeJunkie.SourceGeneratorUtils;

/// <summary>
/// State chart graph data.
/// </summary>
/// <param name="Id">Fully qualified name of the type.</param>
/// <param name="Name">Declared name of the type.</param>
public sealed record StateChartGraph(string Id,
                                     string Name,
                                     string BaseId,
                                     List<StateChartGraph> Children) {
  /// <summary>
  /// State chart graph data (inputs, input to state mappings, and outputs).
  /// </summary>
  public StateChartGraphData Data { get; set; } = default!;

  /// <summary>
  /// Constructor for state chart graph.
  /// </summary>
  public StateChartGraph(string id,
                         string name,
                         string baseId) : this(id, name, baseId, new()) { }

  /// <summary>
  /// UML-friendly identifier for the state chart graph.
  /// </summary>
  public string UmlId =>
    Id
    .Replace("global::", "")
    .Replace(':', '_')
    .Replace('.', '_')
    .Replace('<', '_')
    .Replace('>', '_')
    .Replace(',', '_');

  /// <summary>
  /// Generates a string representation of the state chart graph.
  /// </summary>
  public override string ToString() => Describe(0);

  /// <summary>
  /// Generates a string representation of the state chart graph with indentation.
  /// </summary>
  public string Describe(int level) {
    var indent = CodeJunkieGenerator.Tab(level);

    return ($"{indent}StateChartGraph {{\n" +
      $"{indent}  Id: {Id},\n" +
      $"{indent}  Name: {Name},\n" +
      $"{indent}  BaseId: {BaseId},\n" +
      $"{indent}  Children: [\n" +
      string.Join(",\n", Children.Select(child => child.Describe(level + 2))) +
      $"\n{indent}  ]\n" +
      $"{indent}}}").Replace("global::CodeJunkie.StateChart.Example.", "");
  }

  /// <summary>
  /// Checks if two state chart graphs are equal.
  /// </summary>
  public bool Equals(StateChartGraph? other) =>
    other is not null &&
    Id == other.Id &&
    Name == other.Name &&
    BaseId == other.BaseId &&
    Children.SequenceEqual(other.Children);

  /// <summary>
  /// Generates a hash code for the state chart graph.
  /// </summary>
  public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// State chart implementation.
/// </summary>
public sealed record StateChartImplementation(string FilePath,
                                              string Id,
                                              string Name,
                                              ImmutableHashSet<string> InitialStateIds,
                                              StateChartGraph Graph,
                                              ImmutableDictionary<string, StateChartGraph> StatesById) {
  /// <summary>
  /// Checks if two state chart implementations are equal.
  /// </summary>
  public bool Equals(StateChartImplementation? other) =>
    other is not null &&
    Id == other.Id &&
    Name == other.Name &&
    Graph.Equals(other.Graph);

  /// <summary>
  /// Generates a hash code for the state chart implementation.
  /// </summary>
  public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// State chart subclass.
/// </summary>
public record StateChartSubclass(string Id, string Name, string BaseId);

/// <summary>
/// State chart input.
/// </summary>
public record StateChartInput(string Id, string Name);

/// <summary>
/// State chart output.
/// </summary>
public record StateChartOutput(string Id, string Name);

/// <summary>
/// State chart result.
/// </summary>
public interface IStateChartResult;

/// <summary>
/// Invalid state chart result.
/// </summary>
public record InvalidStateChartResult : IStateChartResult;

/// <summary>
/// State chart output result.
/// </summary>
public record StateChartOutputResult(string FilePath, string Name, string Content)
  : IStateChartResult;

/// <summary>
/// State chart graph data.
/// </summary>
public record StateChartGraphData(ImmutableDictionary<string, StateChartInput> Inputs,
                                  ImmutableDictionary<string, ImmutableHashSet<string>> InputToStates,
                                  ImmutableDictionary<IOutputContext, ImmutableHashSet<StateChartOutput>> Outputs);

/// <summary>
/// Interface for output context.
/// </summary>
public interface IOutputContext {
  /// <summary>
  /// Display name of the context in which the output is being produced:
  /// This is usually OnEnter, OnExit, or an input handler name.
  /// </summary>
  string DisplayName { get; }
}

/// <summary>
/// Static class for output contexts.
/// </summary>
public static class OutputContexts {
  private record OutputOnEnterContext : IOutputContext {
    public string DisplayName => "OnEnter";
  }

  private record OutputOnExitContext : IOutputContext {
    public string DisplayName => "OnExit";
  }

  private record OutputOnHandlerContext(string InputName) : IOutputContext {
    public string DisplayName => $"On{InputName}";
  }

  private record NoOutputContext : IOutputContext {
    public string DisplayName => "None";
  }

  private record OutputMethodContext(string MethodName) : IOutputContext {
    public string DisplayName => $"{MethodName}()";
  }

  /// <summary>
  /// Static instance representing no output context.
  /// </summary>
  public static readonly IOutputContext None = new NoOutputContext();

  /// <summary>
  /// Static instance representing the OnEnter output context.
  /// </summary>
  public static readonly IOutputContext OnEnter = new OutputOnEnterContext();

  /// <summary>
  /// Static instance representing the OnExit output context.
  /// </summary>
  public static readonly IOutputContext OnExit = new OutputOnExitContext();

  /// <summary>
  /// Creates an output context for a specific input name.
  /// </summary>
  public static IOutputContext OnInput(string inputName) => new OutputOnHandlerContext(inputName);

  /// <summary>
  /// Creates an output context for a specific method name.
  /// </summary>
  public static IOutputContext Method(string displayName) => new OutputMethodContext(displayName);
}
