namespace CodeJunkie.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Collections.Immutable;

/// <summary>
/// Constants for the StateChart diagram generator.
/// </summary>
public class Constants {
  /// <summary>
  /// The name of the attribute used to mark classes as state charts.
  /// </summary>
  public static int SpacesPerIndent = 2;

  public const string DisableCsprojProp = "StateChartsDiagramGeneratorDisabled";
  public const string StateChartGetInitialState = "GetInitialState";
  public const string StateChartStateOutput = "Output";
  public const string StateChartStateLogicOnEnter = "OnEnter";
  public const string StateChartStateLogicOnExit = "OnExit";
  public const string StateChartInputInterfaceId = "global::CodeJunkie.StateChart.StateChart.IGet";
  public const string StateChartAttributeName = "StateChart";
  public const string StateChartAttributeNameFull = "StateChartAttribute";

  /// <summary>
  /// The name of the attribute used to mark classes as state charts.
  /// </summary>
  public static readonly ImmutableDictionary<string, string> PostInitializationSources =
    new Dictionary<string, string>() { }.ToImmutableDictionary();
}
