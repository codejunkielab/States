namespace CodeJunkie.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Linq;

public abstract class CodeJunkieGenerator {
  /// <summary>
  /// Generates a string containing whitespace equivalent to the specified number of tabs.
  /// </summary>
  /// <param name="numTabs">The number of tab levels to convert into spaces.</param>
  /// <returns>A string containing spaces equivalent to <paramref name="numTabs" /> multiplied by <see cref="SPACES_PER_TAB"/>.</returns>
  public static string Tab(int numTabs) => new(' ', numTabs * Constants.SpacesPerIndent);

  /// <summary>
  /// Generates a string containing whitespace equivalent to the specified number of tabs,
  /// </summary>
  /// <param name="numTabs">Number of tabs to indent.</param>
  /// <param name="text">Text to indent.</param>
  /// <returns>A string containing the specified number of tabs followed by the provided text.</returns>
  public static string Tab(int numTabs, string text) => Tab(numTabs) + text;

  /// <summary>
  /// Formats the provided code by normalizing new lines and applying a custom indentation-aware interpolation handler.
  /// This ensures proper indentation for string enumerable expressions within the interpolation.
  /// </summary>
  /// <param name="code">The code to be formatted.</param>
  /// <returns>The formatted code as a string.</returns>
  public static string Format(IndentationAwareInterpolationHandler code) =>
    code.GetFormattedText().Clean();

  /// <summary>
  /// Conditionally returns the specified lines of code if the provided condition evaluates to true.
  /// Otherwise, returns an empty enumerable.
  /// </summary>
  /// <param name="condition">The condition to evaluate.</param>
  /// <param name="lines">The lines of code to return if the condition is true.</param>
  /// <returns>An enumerable containing the specified lines of code, or an empty enumerable if the condition is false.</returns>
  public static IEnumerable<string> If(bool condition, IEnumerable<string> lines) =>
    condition ? lines : [];

  /// <summary>
  /// Conditionally returns the specified lines of code if the provided condition evaluates to true.
  /// <paramref name="condition" /> is true, otherwise returns
  /// an empty enumerable.
  /// </summary>
  /// <param name="condition">Condition to check.</param>
  /// <param name="lines">Lines of code to return if condition is true.</param>
  /// <returns>Enumerable lines of code.</returns>
  public static IEnumerable<string> If(bool condition, params string[] lines) =>
    condition ? lines : Enumerable.Empty<string>();
}
