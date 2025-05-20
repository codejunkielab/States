namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// State chart state. Inherit from this class to create a base state for a
/// states.
/// </summary>
/// <typeparam name="TState">State type inheriting from this record.</typeparam>
public abstract record StateLogic<TState> : StateBase
  where TState : StateLogic<TState> {
  /// <summary>
  /// State chart state. Inherit from this class to create a base state for a
  /// states.
  /// </summary>
  protected StateLogic() : base(new StateChart<TState>.ContextAdapter()) { }

  /// <inheritdoc />
  internal override void OnEnter<TDerivedState>(Action<object?> handler)
    => InternalState.EnterCallbacks.Enqueue(
      new((obj) => handler(obj as TState), typeof(TDerivedState))
    );

  /// <inheritdoc />
  internal override void OnExit<TDerivedState>(Action<object?> handler)
    => InternalState.ExitCallbacks.Push(
      new((obj) => handler(obj as TState), typeof(TDerivedState))
    );

  /// <summary>
  /// Runs all of the registered entrance callbacks for the state.
  /// </summary>
  /// <param name="previous">Previous state, if any.</param>
  public void Enter(TState? previous = default) =>
    CallOnEnterCallbacks(previous, this as TState);

  /// <summary>
  /// Runs all of the registered entrance callbacks for the state. To facilitate
  /// testing, only the type of the previous state needs to be specified.
  /// </summary>
  /// <typeparam name="TPreviousState">Type of the previous state, if any.
  /// </typeparam>
  public void Enter<TPreviousState>() where TPreviousState : TState =>
    CallOnEnterCallbacks(typeof(TPreviousState), this as TState);

  /// <summary>
  /// Runs all of the registered exit callbacks for the state.
  /// </summary>
  /// <param name="next">Next state, if any.</param>
  public void Exit(TState? next = default) =>
    CallOnExitCallbacks(this as TState, next);

  /// <summary>
  /// Runs all of the registered exit callbacks for the state. To facilitate
  /// testing, only the type of the next state needs to be specified.
  /// </summary>
  /// <typeparam name="TNextState">Type of the next state, if any.</typeparam>
  public void Exit<TNextState>() where TNextState : TState =>
    CallOnExitCallbacks(this as TState, typeof(TNextState));

  /// <summary>
  /// Defines a transition to a state stored on the states's blackboard.
  /// </summary>
  /// <typeparam name="TStateType">Type of state to transition to.</typeparam>
  protected StateChart<TState>.Transition To<TStateType>()
    where TStateType : TState => new(Context.Get<TStateType>());

  /// <summary>Defines a self-transition.</summary>
  protected StateChart<TState>.Transition ToSelf() => new((this as TState)!);

  /// <summary>
  /// Adds an input value to the states's internal input queue and
  /// returns the current state.
  /// </summary>
  /// <param name="input">Input to process.</param>
  /// <typeparam name="TInputType">Type of the input.</typeparam>
  protected void Input<TInputType>(in TInputType input)
    where TInputType : struct => Context.Input(input);

  /// <summary>
  /// Produces a states output value.
  /// </summary>
  /// <typeparam name="TOutputType">Type of output to produce.</typeparam>
  /// <param name="output">Output value.</param>
  protected void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct => Context.Output(output);

  /// <summary>
  /// Gets data from the blackboard.
  /// </summary>
  /// <typeparam name="TData">The type of data to retrieve.</typeparam>
  /// <exception cref="System.Collections.Generic.KeyNotFoundException" />
  protected TData Get<TData>() where TData : class => Context.Get<TData>();

  /// <summary>
  /// Adds an error to a states. Errors are immediately processed by the
  /// states's <see cref="StateChart{TState}.HandleError(Exception)"/>
  /// callback.
  /// </summary>
  /// <param name="e">Exception to add.</param>
  protected void AddError(Exception e) => Context.AddError(e);

  private void CallOnEnterCallbacks(object? previous, TState? next) {
    if (next is StateLogic<TState> nextLogic) {
      foreach (var onEnter in nextLogic.InternalState.EnterCallbacks) {
        if (onEnter.IsType(previous)) {
          // Already entered this state type.
          continue;
        }
        RunSafe(onEnter.Callback, previous);
      }
    }
  }

  private void CallOnExitCallbacks(TState? previous, object? next) {
    if (previous is StateLogic<TState> previousLogic) {
      foreach (var onExit in previousLogic.InternalState.ExitCallbacks) {
        if (onExit.IsType(next)) {
          // Not actually leaving this state type.
          continue;
        }
        RunSafe(onExit.Callback, next);
      }
    }
  }

  private void RunSafe(
    Action<object?> callback, object? stateArg
  ) {
    try { callback(stateArg); }
    catch (Exception e) {
      if (InternalState.ContextAdapter.OnError is { } onError) {
        onError(e);
        return;
      }
      throw;
    }
  }
}
