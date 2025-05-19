namespace CodeJunkie.States;

using System;

/// <summary>Creates a new states exception.</summary>
public class LogicBlockException : Exception {
  /// <summary>
  /// Creates a new states exception with the specified message.
  /// </summary>
  /// <param name="message">Exception message.</param>
  public LogicBlockException(string message) : base(message) { }
}
