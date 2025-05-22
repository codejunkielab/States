namespace CodeJunkie.StateChart.DiagramGenerator.Models;

/// <summary>
/// Represents the data generated during the state chart generation process.
/// </summary>
public record GenerationData(IStateChartResult Result, GenerationOptions Options);
