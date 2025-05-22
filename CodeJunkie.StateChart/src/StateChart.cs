namespace CodeJunkie.StateChart;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeJunkie.Collections;
using CodeJunkie.Metadata;
using CodeJunkie.Serialization;

#if WITH_SERIALIZATION
/// A state chart. State charts are machines that receive input, maintain a
/// single state, and produce outputs. They can be used as simple
/// input-to-state reducers or extended to create hierarchical state
/// machines.
public interface IStateChart<TState> : IStateChartBase, ISerializableBlackboard where TState : StateLogic<TState> {
#else
/// <summary>
/// State charts are similar to state machines and enable the state pattern to
/// be easily utilized using traditional object-oriented programming in C#.
/// Each state is a self-contained record.
/// </summary>
/// <typeparam name="TState">The type of the state logic used in the state chart.</typeparam>
public interface IStateChart<TState> : IStateChartBase, IBlackboard where TState : StateLogic<TState> {
#endif
  /// <summary>
  /// Gets the context associated with the state chart, providing access to shared resources and state management.
  /// </summary>
  IContext Context { get; }

  /// <summary>
  /// Gets the current state of the state chart.
  /// </summary>
  TState Value { get; }

  /// <summary>
  /// Indicates whether the state chart is currently processing inputs or transitioning between states.
  /// </summary>
  bool IsProcessing { get; }

  /// <summary>
  /// Indicates whether the state chart has been started and is in an active state.
  /// </summary>
  bool IsStarted { get; }

  /// <summary>
  /// Retrieves the initial state of the state chart.
  /// </summary>
  /// <returns>A <see cref="StateChart{TState}.Transition"/> representing the initial state.</returns>
  StateChart<TState>.Transition GetInitialState();

  /// <summary>
  /// Processes an input and transitions the state chart to the appropriate state based on the input.
  /// </summary>
  /// <typeparam name="TInputType">The type of the input being processed.</typeparam>
  /// <param name="input">The input to process.</param>
  /// <returns>The current state after processing the input.</returns>
  TState Input<TInputType>(in TInputType input) where TInputType : struct;

  /// <summary>
  /// Creates a binding for the state chart, allowing external components to monitor or interact with its states and transitions.
  /// </summary>
  /// <returns>An <see cref="StateChart{TState}.IBinding"/> instance for managing bindings.</returns>
  StateChart<TState>.IBinding Bind();

  /// <summary>
  /// Starts the state chart, initializing its state and enabling state transitions.
  /// </summary>
  void Start();

  /// <summary>
  /// Stops the state chart, clearing its state and disabling further transitions.
  /// </summary>
  void Stop();

  /// <summary>
  /// Forces the state chart to reset to a specified state, bypassing normal transition rules.
  /// </summary>
  /// <param name="state">The state to reset to.</param>
  /// <returns>The new current state after the reset.</returns>
  TState ForceReset(TState state);

  /// <summary>
  /// Restores the state chart from another state chart instance, optionally calling the OnEnter method of the restored state.
  /// </summary>
  /// <param name="logic">The state chart instance to restore from.</param>
  /// <param name="shouldCallOnEnter">Indicates whether to call the OnEnter method of the restored state.</param>
  void RestoreFrom(IStateChart<TState> logic, bool shouldCallOnEnter = true);

  /// <summary>
  /// Adds a binding to the state chart, allowing external components to monitor or interact with its states and transitions.
  /// </summary>
  /// <param name="binding">The binding to add.</param>
  void AddBinding(IStateChartBinding<TState> binding);

  /// <summary>
  /// Removes a binding from the state chart.
  /// </summary>
  /// <param name="binding">The binding to remove.</param>
  void RemoveBinding(IStateChartBinding<TState> binding);
}

public abstract partial class StateChart<TState> : StateChartBase, IStateChart<TState>, IBoxlessValueHandler where TState : StateLogic<TState> {
  /// <summary>
  /// Creates a fake binding for testing purposes. This allows manual triggering of bindings
  /// to simulate state changes, inputs, outputs, and errors.
  /// </summary>
  public static IFakeBinding CreateFakeBinding() => new FakeBinding();

  private TState? _value;
  private int _isProcessing;
  private readonly BoxlessQueue _inputs;
  private readonly HashSet<IStateChartBinding<TState>> _bindings = new();

  private bool _shouldCallOnEnter = true;

  /// <inheritdoc />
  /// <summary>
  /// Gets the context associated with the state chart, which provides access to shared resources and state management.
  /// </summary>
  public IContext Context { get; }

  /// <inheritdoc />
  /// <summary>
  /// Indicates whether the state chart is currently processing inputs or transitioning between states.
  /// </summary>
  public bool IsProcessing => _isProcessing > 0;

  /// <inheritdoc />
  /// <summary>
  /// Indicates whether the state chart has been started and is in an active state.
  /// </summary>
  public bool IsStarted => _value is not null;

  /// <inheritdoc />
  /// <summary>
  /// Gets the current state of the state chart. If no state is set, it initializes the state chart.
  /// </summary>
  public TState Value => _value ?? Flush();

  /// <summary>
  /// The state to restore to when the state chart is initialized.
  /// </summary>
  protected StateChart() {
    _inputs = new(this);
    Context = new DefaultContext(this);
    PreallocateStates(this);
  }

  /// <inheritdoc />
  public abstract Transition GetInitialState();

  /// <inheritdoc />
  public virtual IBinding Bind() => new Binding(this);

  /// <inheritdoc />
  public virtual TState Input<TInputType>(in TInputType input) where TInputType : struct {
    if (IsProcessing) {
      _inputs.Enqueue(input);
      return Value;
    }
    return ProcessInputs<TInputType>(input);
  }

  /// <inheritdoc />
  public void Start() {
    if (IsProcessing || _value is not null) { return; }

    Flush();
  }

  /// <inheritdoc />
  public virtual void OnStart() { }

  /// <inheritdoc />
  public void Stop() {
    if (IsProcessing || _value is null) { return; }

    OnStop();

    ChangeState(null);

    _inputs.Clear();

    _value = null;
  }

  /// <inheritdoc />
  public virtual void OnStop() { }

  /// <inheritdoc />
  public TState ForceReset(TState state) {
    if (IsProcessing) {
      throw new StateChartException(
          "Force reset failed: The state chart is currently processing inputs. " +
          "Avoid calling ForceReset() from within the state's own processing logic.");
    }

    ChangeState(state);

    return Flush();
  }

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void AddBinding(IStateChartBinding<TState> binding) =>
    _bindings.Add(binding);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void RemoveBinding(IStateChartBinding<TState> binding) =>
    _bindings.Remove(binding);

  /// <summary>
  /// Determines whether the state chart can transition to the specified state.
  /// </summary>
  /// <param name="state">The target state to transition to.</param>
  /// <returns>True if the state can be changed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected virtual bool CanChangeState(TState? state) {
    return !SerializationUtilities.IsEquivalent(state, _value);
  }

  /// <summary>
  /// Handles errors encountered during state chart execution by logging or processing the exception.
  /// </summary>
  /// <param name="e">The exception to handle.</param>
  internal virtual void AddError(Exception e) {
    AnnounceException(e);
    HandleError(e);
  }

  /// <summary>
  /// Outputs a value from the state chart, notifying all bindings of the output.
  /// </summary>
  /// <typeparam name="TOutput">The type of the output value.</typeparam>
  /// <param name="output">The output value to announce.</param>
  internal virtual void OutputValue<TOutput>(in TOutput output)
    where TOutput : struct => AnnounceOutput(output);

  /// <summary>
  /// Handles errors encountered during state chart execution.
  /// </summary>
  protected virtual void HandleError(Exception e) { }

  /// <summary>
  /// Creates a transition to the specified state type.
  /// </summary>
  protected Transition To<TStateType>()
    where TStateType : TState => new(Context.Get<TStateType>());

  #region IReadOnlyBlackboard
  /// <inheritdoc />
  public IReadOnlySet<Type> Types => Blackboard.Types;

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public TData Get<TData>() where TData : class => Blackboard.Get<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public object GetObject(Type type) => Blackboard.GetObject(type);

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Has<TData>() where TData : class => Blackboard.Has<TData>();

  /// <inheritdoc />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool HasObject(Type type) => Blackboard.HasObject(type);
  #endregion IReadOnlyBlackboard

  #region IBlackboard
  /// <inheritdoc cref="IBlackboard.Set{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Set<TData>(TData data) where TData : class =>
    Blackboard.Set(data);

  /// <inheritdoc cref="IBlackboard.SetObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetObject(Type type, object data) =>
    Blackboard.SetObject(type, data);

  /// <inheritdoc cref="IBlackboard.Overwrite{TData}(TData)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Overwrite<TData>(TData data) where TData : class =>
    Blackboard.Overwrite(data);

  /// <inheritdoc cref="IBlackboard.OverwriteObject(Type, object)" />
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void OverwriteObject(Type type, object data) =>
    Blackboard.OverwriteObject(type, data);
  #endregion IBlackboard

#if WITH_SERIALIZATION
#region ISerializableBlackboard
  /// <inheritdoc cref="ISerializableBlackboard.SavedTypes" />
  public IEnumerable<Type> SavedTypes => Blackboard.SavedTypes;

  /// <inheritdoc cref="ISerializableBlackboard.TypesToSave" />
  public IEnumerable<Type> TypesToSave => Blackboard.TypesToSave;

  /// <inheritdoc cref="ISerializableBlackboard.Save{TData}(Func{TData})" />
  public void Save<TData>(Func<TData> factory)
    where TData : class, IIdentifiable => Blackboard.Save(factory);

  public void SaveObject(Type type,
                         Func<object> factory,
                         object? referenceValue) {
    Blackboard.SaveObject(type, factory, referenceValue);
  }
#endregion ISerializableBlackboard
#endif

  /// <summary>
  /// Processes the input queue and transitions the state chart to the appropriate state based on the inputs.
  /// </summary>
  /// <typeparam name="TInputType">The type of input being processed.</typeparam>
  /// <param name="input">An optional input to process immediately.</param>
  /// <returns>The current state after processing inputs.</returns>
  internal TState ProcessInputs<TInputType>(TInputType? input = null) where TInputType : struct {
    _isProcessing++;

    if (_value is null) {
#if WITH_SERIALIZATION
      Blackboard.InstantiateAnyMissingSavedData();
#endif
      ChangeState(RestoredState as TState ?? GetInitialState().State);
      RestoredState = null;
      OnStart();
    }

    if (input.HasValue) {
      (this as IBoxlessValueHandler).HandleValue(input.Value);
    }

    while (_inputs.HasValues) {
      _inputs.Dequeue();
    }

    _isProcessing--;

    _shouldCallOnEnter = true;

    return _value!;
  }

  void IBoxlessValueHandler.HandleValue<TInputType>(in TInputType input)
  where TInputType : struct {
    if (_value is not IGet<TInputType> stateWithInputHandler) {
      return;
    }

    var state = RunInputHandler(stateWithInputHandler, in input, _value);

    AnnounceInput(in input);

    if (!CanChangeState(state)) {
      return;
    }

    ChangeState(state);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ChangeState(TState? state) {
    _isProcessing++;
    var previous = _value;

    previous?.Exit(state);
    previous?.Detach();

    _value = state;

    var stateIsDifferent = CanChangeState(previous);

    if (state is not null) {
      state.Attach(Context);

      if (_shouldCallOnEnter) {
        state.Enter(previous);
      }

      if (stateIsDifferent) {
        AnnounceState(state);
      }
    }
    _isProcessing--;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceInput<TInputType>(in TInputType input)
  where TInputType : struct {
    foreach (var binding in _bindings) {
      binding.MonitorInput(in input);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceState(TState state) {
    foreach (var binding in _bindings) {
      binding.MonitorState(state);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceOutput<TOutputType>(TOutputType output)
  where TOutputType : struct {
    foreach (var binding in _bindings) {
      binding.MonitorOutput(in output);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AnnounceException(Exception exception) {
    foreach (var binding in _bindings) {
      binding.MonitorException(exception);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState RunInputHandler<TInputType>(
    IGet<TInputType> inputHandler,
    in TInputType input,
    TState fallback
  ) where TInputType : struct {
    try { return inputHandler.On(in input).State; }
    catch (Exception e) { AddError(e); }
    return fallback;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private TState Flush() => ProcessInputs<int>();

  /// <inheritdoc />
  public override bool Equals(object? obj) {
    if (ReferenceEquals(this, obj)) { return true; }

    if (obj is not StateChartBase logic) { return false; }

    if (GetType() != logic.GetType()) {
      return false;
    }

    if (
      !SerializationUtilities.IsEquivalent(ValueAsObject, logic.ValueAsObject)
    ) {
      return false;
    }

    var types = Blackboard.Types;
    var otherTypes = logic.Blackboard.Types;

    if (types.Count != otherTypes.Count) { return false; }

    foreach (var type in types) {
      if (!otherTypes.Contains(type)) { return false; }

      var obj1 = Blackboard.GetObject(type);
      var obj2 = logic.Blackboard.GetObject(type);

      if (SerializationUtilities.IsEquivalent(obj1, obj2)) {
        continue;
      }

      return false;
    }

    return true;
  }

  /// <inheritdoc />
  public override int GetHashCode() => base.GetHashCode();

  /// <inheritdoc />
  public void RestoreFrom(IStateChart<TState> logic, bool shouldCallOnEnter = true) {
    _shouldCallOnEnter = shouldCallOnEnter;

    if ((logic.ValueAsObject ?? logic.RestoredState) is not TState state) {
      throw new StateChartException(
          $"State restoration failed: The provided state chart ({logic}) is uninitialized. " +
          "Ensure that Start() has been called on the state chart before attempting to restore.");
    }

    Stop();

    foreach (var type in logic.Blackboard.Types) {
      Blackboard.OverwriteObject(type, logic.Blackboard.GetObject(type));
    }

    var stateType = state.GetType();
    OverwriteObject(stateType, state);
    RestoreState(state);
  }

  #region StateChartBase
  /// <inheritdoc />
  public override object? ValueAsObject => _value;

  /// <inheritdoc />
  public override void RestoreState(object state) {
    if (_value is not null) {
      throw new StateChartException(
          "State restoration failed: The state chart has already been initialized. " +
          "Ensure that the state chart is uninitialized before calling RestoreState().");
    }

    RestoredState = (TState)state;
  }
  #endregion StateChartBase
}
