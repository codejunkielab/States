namespace CodeJunkie.StateChart.Extensions;

using System.Collections.Generic;

/// <summary>
/// Provides utility extension methods for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
internal static class DictionaryExtensions {
  /// <summary>
  /// Adds a value to the dictionary only if the specified key does not already exist.
  /// </summary>
  /// <param name="dictionary">The dictionary to be updated.</param>
  /// <param name="key">The key to check for existence in the dictionary.</param>
  /// <param name="value">The value to add if the key is not already present.</param>
  /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
  /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
  public static void AddIfNotPresent<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
                                                   TKey key,
                                                   TValue value) where TKey : notnull {
    if (!dictionary.ContainsKey(key)) {
      dictionary[key] = value;
    }
  }
}
