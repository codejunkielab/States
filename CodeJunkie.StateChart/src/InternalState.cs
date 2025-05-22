namespace CodeJunkie.StateChart;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the internal state of a state chart.
/// </summary>
internal class InternalState {
  /// <summary>
  /// Queue of callbacks executed upon entering the state.
  /// </summary>
  internal Queue<UpdateCallback> EnterCallbacks { get; }

  /// <summary>
  /// Stack of callbacks executed upon exiting the state.
  /// </summary>
  internal Stack<UpdateCallback> ExitCallbacks { get; }

  /// <summary>
  /// Queue of callbacks executed when the state is attached to the state chart.
  /// </summary>
  internal Queue<Action> AttachCallbacks { get; } = new();

  /// <summary>
  /// Stack of callbacks executed when the state is detached from the state chart.
  /// </summary>
  internal Stack<Action> DetachCallbacks { get; } = new();

  /// <summary>
  /// Adapter for managing the state context. If uninitialized, the state has not been activated.
  /// Can represent the actual state context or a mock for testing purposes.
  /// </summary>
  internal IContextAdapter ContextAdapter { get; }

  /// <summary>
  /// Initializes a new instance of the internal state with the specified context adapter.
  /// </summary>
  /// <param name="contextAdapter">The context adapter for the state chart.</param>
  public InternalState(IContextAdapter contextAdapter) {
    EnterCallbacks = new();
    ExitCallbacks = new();
    ContextAdapter = contextAdapter;
  }

  /// <inheritdoc />
  public override bool Equals(object? obj) => true;

  /// <inheritdoc />
  public override int GetHashCode() => HashCode.Combine(
    EnterCallbacks,
    ExitCallbacks,
    AttachCallbacks,
    DetachCallbacks
  );
}
