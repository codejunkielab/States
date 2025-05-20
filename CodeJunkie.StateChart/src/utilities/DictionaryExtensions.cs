namespace CodeJunkie.StateChart.Extensions;

using System.Collections.Generic;

/// <summary>
/// Provides extension methods for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
internal static class DictionaryExtensions {
  /// <summary>
  /// Adds a value to the dictionary if the specified key does not already exist.
  /// </summary>
  /// <param name="dictionary">The dictionary to modify.</param>
  /// <param name="key">The key to check for existence.</param>
  /// <param name="value">The value to add if the key is not present.</param>
  /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
  /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
  public static void AddIfNotPresent<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
                                                   TKey key,
                                                   TValue value) where TKey : notnull {
    if (!dictionary.ContainsKey(key)) {
      dictionary[key] = value;
    }
  }
}
