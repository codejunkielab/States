namespace CodeJunkie.SourceGeneratorUtils;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple, but effective.
/// Inspired by https://dev.to/panoukos41/debugging-c-source-generators-1flm.
/// </summary>
public class Log {
  /// <summary>
  /// The list of logs.
  /// </summary>
  protected List<string> Logs { get; } = [];

  /// <summary>
  /// Adds a message to the log.
  /// </summary>
  public void Print(string msg) {
#if DEBUG
    var lines = msg.Split('\n').Select(line => "//\t" + line);
    Logs.AddRange(lines);
#endif
  }

  /// <summary>
  /// Adds a message to the log.
  /// </summary>
  public void Clear() => Logs.Clear();

  /// <summary>
  /// Adds a message to the log.
  /// </summary>
  public string Contents => string.Join(Environment.NewLine, Logs);
}
