namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Represents an exception specific to state charts. This exception is thrown
/// when an error occurs within the state chart framework.
/// </summary>
public class StateChartException : Exception {
  /// <summary>
  /// Initializes a new instance of the <see cref="StateChartException"/> class
  /// with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  public StateChartException(string message) : base(message) { }
}
