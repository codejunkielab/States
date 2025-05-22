namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Represents an interface for adapting and managing a context instance.
/// </summary>
internal interface IContextAdapter : IContext {
  /// <summary>
  /// Retrieves the current context instance being managed by the adapter.
  /// </summary>
  IContext? Context { get; }

  /// <summary>
  /// Configures the adapter to use the specified context.
  /// </summary>
  /// <param name="context">The context instance to adapt and manage.</param>
  void Adapt(IContext context);

  /// <summary>
  /// Resets the adapter by clearing the current context.
  /// </summary>
  void Clear();

  /// <summary>
  /// Provides the callback function for handling errors within states.
  /// </summary>
  Action<Exception>? OnError { get; }
}
