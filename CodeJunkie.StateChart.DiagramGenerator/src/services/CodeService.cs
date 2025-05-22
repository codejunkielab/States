namespace CodeJunkie.StateChart.DiagramGenerator.Services;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService {
  /// <summary>
  /// Retrieves the fully qualified name of a named type symbol.
  /// </summary>
  /// <param name="symbol">The named type symbol to process.</param>
  /// <param name="fallbackName">The default name to return if the symbol is null or unnamed.</param>
  /// <returns>The fully qualified name of the symbol, or the fallback name if unavailable.</returns>
  string GetNameFullyQualified(INamedTypeSymbol? symbol, string fallbackName);

  /// <summary>
  /// Retrieves the fully qualified name of a named type symbol, excluding generic arguments.
  /// </summary>
  /// <param name="symbol">The type symbol to process.</param>
  /// <param name="fallbackName">The default name to return if the symbol is null or unnamed.</param>
  /// <returns>The fully qualified name of the symbol without generic arguments, or the fallback name if unavailable.</returns>
  string GetNameFullyQualifiedWithoutGenerics(ITypeSymbol? symbol, string fallbackName);

  /// <summary>
  /// Retrieves all nested types within a given type, including those nested recursively.
  /// </summary>
  /// <param name="symbol">The type symbol to search for nested types.</param>
  /// <param name="predicate">An optional filter predicate to match specific nested types.</param>
  /// <returns>An enumerable collection of nested types that match the predicate.</returns>
  IEnumerable<INamedTypeSymbol> GetAllNestedTypesRecursively(INamedTypeSymbol symbol,
                                                             Func<INamedTypeSymbol, bool> predicate);

  /// <summary>
  /// Retrieves all base types of a given type, traversing the inheritance hierarchy.
  /// </summary>
  /// <param name="type">The type symbol to examine for base types.</param>
  /// <returns>An enumerable sequence of base types, starting from the immediate base type.</returns>
  IEnumerable<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol type);
}

/// <summary>
/// Implementation of ICodeService for common code operations.
/// </summary>
public class CodeService : ICodeService {
  public string GetNameFullyQualified(INamedTypeSymbol? symbol, string fallbackName) =>
    symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? fallbackName;

  public string GetNameFullyQualifiedWithoutGenerics(ITypeSymbol? symbol, string fallbackName) =>
    symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None)) ?? fallbackName;

  public IEnumerable<INamedTypeSymbol> GetAllNestedTypesRecursively(INamedTypeSymbol symbol,
                                                                    Func<INamedTypeSymbol, bool>? predicate = null) {
    predicate ??= (_) => true;
    foreach (var type in @symbol.GetTypeMembers()) {
      if (predicate(type)) { yield return type; }
      foreach (var nestedType in GetAllNestedTypesRecursively(type, predicate)) {
        if (predicate(nestedType)) {
          yield return nestedType;
        }
      }
    }
  }

  public IEnumerable<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol type) {
    var current = type;
    while (current.BaseType != null) {
      yield return current.BaseType;
      current = current.BaseType;
    }
  }
}
