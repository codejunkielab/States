namespace CodeJunkie.StateChart.Tests;

using Xunit;

/// <summary>
/// Defines xUnit test collections to control execution order and isolation.
/// </summary>

/// <summary>
/// Performance tests collection - runs in isolation to prevent GC/monitoring interference.
/// </summary>
[CollectionDefinition("PerformanceTests", DisableParallelization = true)]
public class PerformanceTestsCollection
{
}
