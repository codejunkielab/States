namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Attribute to specify the state type and diagram generation for a state chart.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class StateChartAttribute : Attribute {
  /// <summary>
  /// Specifies the state type. If the state type is an interface, provide the base
  /// state record from which all other states derive, rather than the interface itself.
  /// </summary>
  public Type StateType { get; }

  /// <summary>
  /// Indicates whether a diagram should be generated for this state chart. Defaults to false.
  /// </summary>
  public bool Diagram { get; set; }

  /// <summary>
  /// Initializes a new instance of the <see cref="StateChartAttribute"/> class.
  /// Apply this to a class extending <see cref="StateChart{TState}" /> to enable
  /// serialization utilities and state diagram generation.
  /// </summary>
  /// <param name="stateType">
  /// The type of the state. If the state is an interface, specify the base state
  /// record from which all other states derive, not the interface itself.
  /// </param>
  public StateChartAttribute(Type stateType) {
    StateType = stateType;
  }
}
