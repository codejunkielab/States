namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Indicates that a StateChart class should not be tracked by the StateChartRegistry.
/// Use this attribute on performance-critical StateChart implementations where monitoring overhead is undesirable.
/// </summary>
/// <example>
/// <code>
/// [NoMonitoring]
/// public class HighPerformanceStateChart : StateChart&lt;MyState&gt; {
///     // This StateChart instance will not be tracked by StateChartRegistry
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class NoMonitoringAttribute : Attribute {
}