namespace CodeJunkie.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Collections.Immutable;

public class Constants {
  /// <summary>Spaces per tab. Adjust to your generator's liking.</summary>
  public static int SPACES_PER_INDENT = 2;

  public const string DISABLE_CSPROJ_PROP = "StateChartsDiagramGeneratorDisabled";
  public const string STATE_CHART_GET_INITIAL_STATE = "GetInitialState";
  public const string STATE_CHART_STATE_OUTPUT = "Output";
  public const string STATE_CHART_STATE_LOGIC_ON_ENTER = "OnEnter";
  public const string STATE_CHART_STATE_LOGIC_ON_EXIT = "OnExit";
  public const string STATE_CHART_INPUT_INTERFACE_ID = "global::CodeJunkie.StateChart.StateChart.IGet";
  public const string STATE_CHART_ATTRIBUTE_NAME = "StateChart";
  public const string STATE_CHART_ATTRIBUTE_NAME_FULL = "StateChartAttribute";

  /// <summary>
  /// A dictionary of source code that must be injected into the compilation
  /// regardless of whether or not the user has taken advantage of any of the
  /// other features of this source generator.
  /// </summary>
  public static readonly ImmutableDictionary<string, string>
    PostInitializationSources = new Dictionary<string, string>() { }.ToImmutableDictionary();
}
