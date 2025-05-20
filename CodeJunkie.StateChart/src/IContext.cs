namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Represents the context provided to each states state for managing inputs, outputs, and errors.
/// </summary>
public interface IContext {
  /// <summary>
  /// Adds an input value to the states's internal queue for processing.
  /// </summary>
  /// <param name="input">The input value to process.</param>
  /// <typeparam name="TInputType">The type of the input value.</typeparam>
  void Input<TInputType>(in TInputType input) where TInputType : struct;

  /// <summary>
  /// Produces an output value from the states.
  /// </summary>
  /// <typeparam name="TOutputType">The type of the output value.</typeparam>
  /// <param name="output">The output value to produce.</param>
  void Output<TOutputType>(in TOutputType output) where TOutputType : struct;

  /// <summary>
  /// Retrieves a value from the states's shared blackboard.
  /// </summary>
  /// <typeparam name="TDataType">The type of the value to retrieve.</typeparam>
  /// <returns>The retrieved value.</returns>
  TDataType Get<TDataType>() where TDataType : class;

  /// <summary>
  /// Reports an error to the states for immediate handling.
  /// </summary>
  /// <param name="e">The exception to report.</param>
  void AddError(Exception e);
}
