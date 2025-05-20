namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Internal adapter interface for managing and adapting states contexts.
/// </summary>
internal interface IContextAdapter : IContext {
  /// <summary>
  /// Gets the current context instance.
  /// </summary>
  IContext? Context { get; }

  /// <summary>
  /// Adapts the specified context for use.
  /// </summary>
  /// <param name="context">The context to adapt.</param>
  void Adapt(IContext context);

  /// <summary>
  /// Clears the current context and resets the adapter.
  /// </summary>
  void Clear();

  /// <summary>
  /// Gets the error handling callback for the states.
  /// </summary>
  Action<Exception>? OnError { get; }
}
