namespace CodeJunkie.StateChart.DiagramGenerator.Models;

using System.Collections.Generic;

/// <summary>
/// Represents the data generated during the state chart generation process.
/// </summary>
public record GenerationData(IReadOnlyList<IStateChartResult> Results, GenerationOptions Options);
