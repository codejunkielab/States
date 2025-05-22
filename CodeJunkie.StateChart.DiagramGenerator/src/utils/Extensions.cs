namespace CodeJunkie.SourceGeneratorUtils;

using System;
using System.Text.RegularExpressions;

public static class Extensions {
  /// <summary>
  /// Normalizes line endings in the input text to a specified newline character.
  /// </summary>
  /// <param name="text">The input text to normalize. Cannot be null.</param>
  /// <param name="newLine">The newline character to use. Defaults to the system's environment newline character if not specified.</param>
  /// <returns>A string with normalized line endings.</returns>
  public static string NormalizeLineEndings(this string text, string? newLine = null) {
    newLine ??= Environment.NewLine;
    return text
      .Replace("\r\n", "\n")
      .Replace("\r", "\n")
      .Replace("\n", newLine);
  }

  /// <summary>
  /// Cleans the input text by replacing lines containing only white spaces with empty lines.
  /// Additionally, it reduces consecutive empty lines (three or more) to a single empty line.
  /// </summary>
  /// <param name="text">The input text to clean. Cannot be null.</param>
  /// <param name="newLine">The newline character to use. Defaults to the system's environment newline character if not specified.</param>
  /// <returns>A cleaned string with normalized and reduced empty lines.</returns>
  public static string Clean(this string text, string? newLine = null) {
    newLine ??= Environment.NewLine;
    var value = text.NormalizeLineEndings();

    var lines = value.Split(new[] { newLine }, StringSplitOptions.None);
    for (var i = 0; i < lines.Length; i++) {
      lines[i] = string.IsNullOrWhiteSpace(lines[i]) ? "" : lines[i];
    }

    var escaped = Regex.Escape(newLine);
    var regex = new Regex($$"""({{escaped}}){3,}""");
    return regex.Replace(string.Join(newLine, lines), newLine);
  }
}
