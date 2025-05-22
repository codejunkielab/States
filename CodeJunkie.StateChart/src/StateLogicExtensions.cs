namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Provides extension methods for state logic.
/// </summary>
public static class StateLogicExtensions {
  /// <summary>
  /// Registers a callback to be invoked when the state is entered.
  /// The callback does not receive any arguments.
  ///
  /// Callbacks are invoked in the order they are registered, starting from the base class
  /// and proceeding to the most derived class. This ensures that entrance callbacks
  /// follow the correct order in a statechart hierarchy.
  /// </summary>
  /// <typeparam name="TDerivedState">The type of the derived state being entered.</typeparam>
  /// <param name="state">The state to which the callback is added.</param>
  /// <param name="handler">The callback to invoke upon entering the state.</param>
  public static void OnEnter<TDerivedState>(this TDerivedState state, Action handler)
    where TDerivedState : StateBase =>
    state.OnEnter<TDerivedState>((_) => handler());

  /// <summary>
  /// Registers a callback to be invoked when the state is entered.
  /// The callback receives the previous state as an argument.
  ///
  /// Callbacks are invoked in the order they are registered, starting from the base class
  /// and proceeding to the most derived class. This ensures that entrance callbacks
  /// follow the correct order in a statechart hierarchy.
  /// </summary>
  /// <typeparam name="TBaseState">The base type of the state.</typeparam>
  /// <typeparam name="TDerivedState">The type of the derived state being entered.</typeparam>
  /// <param name="state">The state to which the callback is added.</param>
  /// <param name="handler">The callback to invoke upon entering the state, receiving the previous state as an argument.</param>
  public static void OnEnter<TBaseState, TDerivedState>(this TDerivedState state,
                                                        Action<TBaseState?> handler)
    where TBaseState : StateBase
    where TDerivedState : StateBase, TBaseState =>
    state.OnEnter<TDerivedState>((previous) => handler(previous as TBaseState));

  /// <summary>
  /// Registers a callback to be invoked when the state is exited.
  /// The callback does not receive any arguments.
  ///
  /// Callbacks are invoked in reverse order of their registration, starting from the most derived class
  /// and proceeding to the base class. This ensures that exit callbacks
  /// follow the correct order in a statechart hierarchy.
  /// </summary>
  /// <typeparam name="TDerivedState">The type of the derived state being exited.</typeparam>
  /// <param name="state">The state to which the callback is added.</param>
  /// <param name="handler">The callback to invoke upon exiting the state.</param>
  public static void OnExit<TDerivedState>(this TDerivedState state, Action handler)
    where TDerivedState : StateBase =>
    state.OnExit<TDerivedState>((_) => handler());

  /// <summary>
  /// Registers a callback to be invoked when the state is exited.
  /// The callback receives the next state as an argument.
  ///
  /// Callbacks are invoked in reverse order of their registration, starting from the most derived class
  /// and proceeding to the base class. This ensures that exit callbacks
  /// follow the correct order in a statechart hierarchy.
  /// </summary>
  /// <typeparam name="TBaseState">The base type of the state.</typeparam>
  /// <typeparam name="TDerivedState">The type of the derived state being exited.</typeparam>
  /// <param name="state">The state to which the callback is added.</param>
  /// <param name="handler">The callback to invoke upon exiting the state, receiving the next state as an argument.</param>
  public static void OnExit<TBaseState, TDerivedState>(this TDerivedState state,
                                                       Action<TBaseState?> handler)
    where TBaseState : StateBase
    where TDerivedState : StateBase, TBaseState =>
    state.OnExit<TDerivedState>((next) => handler(next as TBaseState));
}
