namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Provides a shared context for states to manage inputs, outputs, shared data, and error handling.
/// </summary>
public interface IContext {
  /// <summary>
  /// Enqueues an input value for processing by the state.
  /// </summary>
  /// <param name="input">The input value to enqueue.</param>
  /// <typeparam name="TInputType">The type of the input value, which must be a value type.</typeparam>
  void Input<TInputType>(in TInputType input) where TInputType : struct;

  /// <summary>
  /// Sends an output value from the state to the external system or next process.
  /// </summary>
  /// <typeparam name="TOutputType">The type of the output value, which must be a value type.</typeparam>
  /// <param name="output">The output value to send.</param>
  void Output<TOutputType>(in TOutputType output) where TOutputType : struct;

  /// <summary>
  /// Fetches a value from the shared blackboard, which stores data accessible across states.
  /// </summary>
  /// <typeparam name="TDataType">The type of the value to fetch, which must be a reference type.</typeparam>
  /// <returns>The fetched value, or null if the value does not exist.</returns>
  TDataType Get<TDataType>() where TDataType : class;

  /// <summary>
  /// Logs an error for the state to handle immediately.
  /// </summary>
  /// <param name="e">The exception instance representing the error.</param>
  void AddError(Exception e);
}
