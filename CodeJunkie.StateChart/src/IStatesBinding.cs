namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Defines a binding interface for state charts.
/// </summary>
/// <typeparam name="TState">The type of the state logic.</typeparam>
public interface IStateChartBinding<TState> where TState : StateLogic<TState> {
  /// <summary>
  /// Invoked when the state chart receives an input.
  /// </summary>
  /// <param name="input">The input data received.</param>
  /// <typeparam name="TInput">The type of the input data.</typeparam>
  internal void MonitorInput<TInput>(in TInput input)
    where TInput : struct;

  /// <summary>
  /// Invoked when the state chart transitions to a new state.
  /// </summary>
  /// <param name="state">The new state of the state chart.</param>
  internal void MonitorState(TState state);

  /// <summary>
  /// Invoked when the state chart produces an output.
  /// </summary>
  /// <param name="output">The output data produced.</param>
  /// <typeparam name="TOutput">The type of the output data.</typeparam>
  internal void MonitorOutput<TOutput>(in TOutput output)
    where TOutput : struct;

  /// <summary>
  /// Invoked when the state chart encounters an exception.
  /// </summary>
  /// <param name="exception">The exception that occurred.</param>
  internal void MonitorException(Exception exception);
}
