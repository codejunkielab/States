namespace CodeJunkie.StateChart.Analyzers.Utils;

using Microsoft.CodeAnalysis;

public static class Diagnostics {
  public const string ERR_PREFIX = "STATE_CHARTS";
  public const string ERR_CATEGORY = "CodeJunkie.StateChart.Analyzers";

  public static DiagnosticDescriptor MissingStateChartAttributeDescriptor {
    get;
  } = new(
    $"{ERR_PREFIX}_001",
    $"Missing [{Constants.STATE_CHART_ATTRIBUTE_NAME}]",
    messageFormat:
      $"Missing [{Constants.STATE_CHART_ATTRIBUTE_NAME}] on state chart " +
      "implementation `{0}`",
    ERR_CATEGORY,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  public static Diagnostic MissingStateChartAttribute(
    Location location, string name
  ) => Diagnostic.Create(MissingStateChartAttributeDescriptor, location, name);
}
