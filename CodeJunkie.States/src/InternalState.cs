namespace CodeJunkie.States;

using System;
using System.Collections.Generic;

/// <summary>
/// Internal state stored in each states state. This is used to store
/// entrance and exit callbacks without tripping up equality checking.
/// </summary>
internal class InternalState {
  /// <summary>
  /// Callbacks to be invoked when the state is entered.
  /// </summary>
  internal Queue<UpdateCallback> EnterCallbacks { get; }

  /// <summary>
  /// Callbacks to be invoked when the state is exited.
  /// </summary>
  internal Stack<UpdateCallback> ExitCallbacks { get; }

  /// <summary>
  /// Callbacks to be invoked when the state is attached to the states.
  /// </summary>
  internal Queue<Action> AttachCallbacks { get; } = new();

  /// <summary>
  /// Callbacks to be invoked when the state is detached from the states.
  /// </summary>
  internal Stack<Action> DetachCallbacks { get; } = new();

  /// <summary>
  /// <para>
  /// Internal context adapter. If there's no underlying context in the
  /// adapter, the context has not been initialized yet. An uninitialized
  /// context implies the state has never been active in a states.
  /// </para>
  /// <para>
  /// If an underlying object exists, it is either the real states
  /// context or a fake one supplied to facilitate unit-testing.
  /// </para>
  /// </summary>
  internal IContextAdapter ContextAdapter { get; }

  /// <summary>Creates a new state logic internal state.</summary>
  /// <param name="contextAdapter">LogicBlock context adapter.</param>
  public InternalState(IContextAdapter contextAdapter) {
    EnterCallbacks = new();
    ExitCallbacks = new();
    ContextAdapter = contextAdapter;
  }

  // We don't want state logic states to be compared, so we make them
  // always equal to whatever other state logic state they are compared to.
  // This prevents issues where two seemingly equivalent states are not
  // deemed equivalent because their callbacks are different.

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
