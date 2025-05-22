namespace CodeJunkie.SourceGeneratorUtils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// An interpolated string handler that handles indentation for formatted strings.
/// </summary>
[InterpolatedStringHandler]
public readonly ref struct IndentationAwareInterpolationHandler {
  private class State {
    public bool EndedOnWhitespace { get; set; }
    public int Indent { get; set; }
    public string Prefix => new(' ', Indent * Constants.SpacesPerIndent);
  }

  private readonly StringBuilder _builder;
  private readonly State _state = new();

  /// <summary>
  /// Initializes a new instance of the <see cref="IndentationAwareInterpolationHandler"/> class.
  /// </summary>
  /// <param name="literalLength">The length of the literal string.</param>
  /// <param name="formattedCount">The number of formatted strings.</param>
  public IndentationAwareInterpolationHandler(int literalLength, int formattedCount) {
    _builder = new StringBuilder(literalLength);
  }

  /// <summary>
  /// Appends a formatted string to the builder.
  /// </summary>
  /// <param name="s">The literal string.</param>
  public void AppendLiteral(string s) => AddString(s);

  /// <summary>
  /// Appends a formatted string to the builder.
  /// </summary>
  /// <typeparam name="T">The type of the formatted string.</typeparam>
  public void AppendFormatted<T>(T? t) {
    if (t is not T item) {
      return;
    }
    else if (item is IEnumerable<string> lines) {
      AddLines(lines);
      return;
    }
    else if (item is string str) {
      AddString(str);
      return;
    }

    _builder.Append(item.ToString());
  }

  /// <summary>
  /// Returns the formatted text.
  /// </summary>
  internal string GetFormattedText() => _builder.ToString();

  private void AddString(string s) {
    var value = s.NormalizeLineEndings();
    var lastNewLineIndex = value.LastIndexOf('\n');
    var remainingString = value.Substring(lastNewLineIndex + 1);
    var remainingNonWs = remainingString.TrimEnd();
    _state.EndedOnWhitespace = remainingNonWs.Length == 0;
    _state.Indent = _state.EndedOnWhitespace
      ? remainingString.Length / Constants.SpacesPerIndent
      : 0;
    _builder.Append(value);
  }

  private void AddLines(IEnumerable<string> lines) {
    var prefix = _state.Prefix;
    var value = string.Join(
        Environment.NewLine,
        lines.Take(1).Concat(lines.Skip(1).Select((line) => prefix + line)));
    if (string.IsNullOrEmpty(value)) {
      return;
    }
    _builder.Append(value);
  }
}
