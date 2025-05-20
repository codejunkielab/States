namespace CodeJunkie.StateChart;

using System;

/// <summary>Creates a new states exception.</summary>
public class StateChartException : Exception {
  /// <summary>
  /// Creates a new states exception with the specified message.
  /// </summary>
  /// <param name="message">Exception message.</param>
  public StateChartException(string message) : base(message) { }
}
