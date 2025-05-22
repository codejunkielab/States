namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Serves as the foundational class for states within a state chart.
/// This class is primarily intended for internal use by the StateChart framework.
/// For user-defined states, it is recommended to extend <see cref="StateLogic{TState}"/>
/// for better customization and functionality.
/// </summary>
public abstract record StateBase {
  /// <inheritdoc />
  internal IContext Context => InternalState.ContextAdapter;
  internal InternalState InternalState { get; }
  internal bool IsAttached => InternalState.ContextAdapter.Context is not null;

  internal StateBase(IContextAdapter contextAdapter) {
    InternalState = new(contextAdapter);
  }

  /// <summary>
  /// Generates and assigns a mock context to serve as the underlying context object for the state.
  /// This is particularly useful for testing logic states in isolation, enabling easier verification
  /// of interactions with the context during unit tests or debugging.
  /// </summary>
  /// <returns>
  /// An instance of <see cref="IFakeContext"/> representing the mock context for the state.
  /// </returns>
  public IFakeContext CreateFakeContext() {
    if (InternalState.ContextAdapter.Context is FakeContext fakeContext) {
      fakeContext.Reset();
      return fakeContext;
    }

    var context = new FakeContext();
    InternalState.ContextAdapter.Adapt(context);
    return context;
  }

  /// <summary>
  /// Adds a callback to be executed when the state is attached to a state chart.
  /// A state is considered "attached" when it becomes the active state.
  /// Unlike entrance callbacks, all registered attach callbacks are executed immediately upon attachment.
  /// </summary>
  /// <param name="handler">
  /// A delegate representing the callback to invoke when the state is attached.
  /// </param>
  public void OnAttach(Action handler) =>
    InternalState.AttachCallbacks.Enqueue(handler);

  /// <summary>
  /// Adds a callback to be executed when the state is detached from a state chart.
  /// A state is considered "detached" when it is no longer the active state.
  /// Unlike exit callbacks, all registered detach callbacks are executed immediately upon detachment.
  /// </summary>
  /// <param name="handler">
  /// A delegate representing the callback to invoke when the state is detached.
  /// </param>
  public void OnDetach(Action handler) =>
    InternalState.DetachCallbacks.Push(handler);

  /// <summary>
  /// Executes all callbacks that were registered for the state upon attachment.
  /// </summary>
  /// <param name="context">
  /// The context of the state chart to which the state is being attached.
  /// </param>
  public void Attach(IContext context) {
    InternalState.ContextAdapter.Adapt(context);
    CallAttachCallbacks();
  }

  /// <summary>
  /// Executes all callbacks that were registered for the state upon detachment.
  /// </summary>
  public void Detach() {
    if (!IsAttached) {
      return;
    }

    CallDetachCallbacks();
    InternalState.ContextAdapter.Clear();
  }

  private void CallAttachCallbacks() {
    foreach (var onAttach in InternalState.AttachCallbacks) {
      RunSafe(onAttach);
    }
  }

  private void CallDetachCallbacks() {
    foreach (var onDetach in InternalState.DetachCallbacks) {
      RunSafe(onDetach);
    }
  }

  private void RunSafe(Action callback) {
    try { callback(); }
    catch (Exception e) {
      if (InternalState.ContextAdapter.OnError is { } onError) {
        onError(e);
        return;
      }
      throw;
    }
  }

    /// <summary>
    /// Adds a callback to be executed when the state is entered.
    /// The callback receives the previous state as an argument, allowing for
    /// context-specific logic during the transition.
    /// <br />
    /// Callbacks are executed in the order they are registered, starting from the base class
    /// and proceeding to the most derived class, ensuring consistency with state chart behavior.
    /// </summary>
    /// <typeparam name="TDerivedState">
    /// The type of the derived state that is being entered.
    /// </typeparam>
    /// <param name="handler">
    /// A delegate representing the callback to invoke upon state entry.
    /// </param>
    internal abstract void OnEnter<TDerivedState>(Action<object?> handler);

    /// <summary>
    /// Adds a callback to be executed when the state is exited.
    /// The callback receives the next state as an argument, enabling logic to be executed
    /// during the transition to the next state.
    /// <br />
    /// Callbacks are executed in reverse order of registration, starting from the most derived class
    /// and proceeding to the base class, ensuring consistency with state chart behavior.
    /// </summary>
    /// <typeparam name="TDerivedState">
    /// The type of the derived state that is being exited.
    /// </typeparam>
    /// <param name="handler">
    /// A delegate representing the callback to invoke upon state exit.
    /// </param>
    internal abstract void OnExit<TDerivedState>(Action<object?> handler);
}
