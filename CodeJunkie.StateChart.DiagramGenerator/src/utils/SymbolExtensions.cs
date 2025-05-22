namespace CodeJunkie.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Provides utility extension methods for <see cref="ITypeSymbol"/>.
/// </summary>
public static class SymbolExtensions {
  /// <summary>
  /// Checks if the specified type inherits from or is equal to the specified base type.
  /// </summary>
  public static bool InheritsFromOrEquals(this ITypeSymbol type, INamedTypeSymbol baseType) =>
    type
    .GetBaseTypesAndThis()
    .Any(t => SymbolEqualityComparer.Default.Equals(t, baseType)) ||
    (baseType.IsGenericType &&
     type.GetBaseTypesAndThis().Any(t =>
       SymbolEqualityComparer.Default.Equals(
         t.OriginalDefinition,
         baseType.OriginalDefinition)));

  private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol? type) {
    var current = type;
    while (current != null) {
      yield return current;
      current = current.BaseType;
    }
  }
}
