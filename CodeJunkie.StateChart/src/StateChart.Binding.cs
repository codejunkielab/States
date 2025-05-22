namespace CodeJunkie.StateChart;

using System;
using System.Collections.Generic;

/// <summary>
/// <para>Provides fluent bindings for states in the state chart.</para>
/// </summary>
public abstract partial class StateChart<TState> {
  /// <summary>
  /// Represents an action that processes a specific type of input.
  /// </summary>
  /// <typeparam name="TInputType">The type of input to be processed.</typeparam>
  /// <param name="input">The input object to be handled.</param>
  public delegate void InputAction<TInputType>(in TInputType input)
    where TInputType : struct;

  /// <summary>
  /// Represents an action that processes a specific type of output.
  /// </summary>
  /// <typeparam name="TOutputType">The type of output to be processed.</typeparam>
  /// <param name="output">The output object to be handled.</param>
  public delegate void OutputAction<TOutputType>(in TOutputType output)
    where TOutputType : struct;

  /// <summary>
  /// <para>Defines bindings for states in the state chart.</para>
  /// <para>
  /// Bindings enable the selection of data from a state, invocation of methods
  /// when specific states occur, and handling of outputs. They promote a declarative
  /// coding style and help avoid redundant updates when irrelevant data changes.
  /// </para>
  /// </summary>
  public interface IBinding : IDisposable {
    /// <summary>
    /// Registers a callback to be executed whenever an input of type
    /// <typeparamref name="TInputType" /> is received.
    /// </summary>
    /// <param name="handler">The callback handler for the input.</param>
    /// <typeparam name="TInputType">The type of input to associate with the handler.</typeparam>
    /// <returns>The current binding instance for chaining.</returns>
    IBinding Watch<TInputType>(InputAction<TInputType> handler)
      where TInputType : struct;

    /// <summary>
    /// Creates a binding group for a specific type of state.
    /// The registered callback is executed only when the specified state type
    /// <typeparamref name="TStateType" /> is encountered.
    /// </summary>
    /// <typeparam name="TStateType">The type of state to bind the handler to.</typeparam>
    /// <param name="handler">The callback to invoke when the state changes.</param>
    /// <returns>The current binding instance for chaining.</returns>
    public IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState;

    /// <summary>
    /// Registers a callback to be executed whenever an output of type
    /// <typeparamref name="TOutputType" /> is produced.
    /// </summary>
    /// <param name="handler">The callback handler for the output.</param>
    /// <typeparam name="TOutputType">The type of output to associate with the handler.</typeparam>
    /// <returns>The current binding instance for chaining.</returns>
    IBinding Handle<TOutputType>(OutputAction<TOutputType> handler)
      where TOutputType : struct;

    /// <summary>
    /// Registers a callback to handle exceptions of type
    /// <typeparamref name="TException" /> when they occur.
    /// </summary>
    /// <param name="handler">The callback handler for the exception.</param>
    /// <typeparam name="TException">The type of exception to handle.</typeparam>
    /// <returns>The current binding instance for chaining.</returns>
    IBinding Catch<TException>(Action<TException> handler)
      where TException : Exception;
  }

  /// <summary>
  /// <para>Provides fluent bindings for states in the state chart.</para>
  /// <para>
  /// Bindings enable the selection of data from a state, invocation of methods
  /// when specific states occur, and handling of outputs. They promote a declarative
  /// coding style and help avoid redundant updates when irrelevant data changes.
  /// </para>
  /// <para>
  /// Ensure to dispose of the binding properly after use to release resources.
  /// </para>
  /// </summary>
  internal abstract class BindingBase : StateChartListenerBase<TState>, IBinding {
    // Functions that check if a binding should run for a given TInputType.
    // These are stored as a dictionary of type to list of handlers.
    internal readonly Dictionary<Type, List<object>> _inputRunners;
    internal readonly Dictionary<Type, List<object>> _outputRunners;

    // Functions that determine if a binding should execute for a given TState.
    internal readonly List<Func<TState, bool>> _stateCheckers;
    // Functions that execute bindings for specific TState types.
    internal readonly List<Action<TState>> _stateRunners;

    // Functions that check if a binding should handle a given Exception.
    internal readonly List<Func<Exception, bool>> _exceptionCheckers;
    // Functions that execute bindings for specific Exception types.
    internal readonly List<Action<Exception>> _exceptionRunners;

    internal BindingBase() {
      _inputRunners = new();
      _outputRunners = new();
      _stateCheckers = new();
      _stateRunners = new();
      _exceptionCheckers = new();
      _exceptionRunners = new();
    }

    /// <inheritdoc />
    public IBinding Watch<TInputType>(InputAction<TInputType> handler)
      where TInputType : struct {
        if (_inputRunners.TryGetValue(typeof(TInputType), out var runners)) {
          runners.Add(handler);
        }
        else {
          _inputRunners[typeof(TInputType)] = new List<object> { handler };
        }

        return this;
      }

    /// <inheritdoc />
    public IBinding When<TStateType>(Action<TStateType> handler)
      where TStateType : TState {
        // Only run the callback if the incoming state is the expected type of
        // state. All incoming states are guaranteed to be non-equivalent to the
        // previous state.
        _stateCheckers.Add((state) => state is TStateType);
        _stateRunners.Add((state) => handler((TStateType)state));

        return this;
      }

    /// <inheritdoc />
    public IBinding Handle<TOutputType>(OutputAction<TOutputType> handler)
      where TOutputType : struct {
        if (_outputRunners.TryGetValue(typeof(TOutputType), out var runners)) {
          runners.Add(handler);
        }
        else {
          _outputRunners[typeof(TOutputType)] = new List<object> { handler };
        }

        return this;
      }

    /// <inheritdoc />
    public IBinding Catch<TException>(
        Action<TException> handler
        ) where TException : Exception {
      _exceptionCheckers.Add((error) => error is TException);
      _exceptionRunners.Add((error) => handler((TException)error));

      return this;
    }

    protected override void ReceiveInput<TInputType>(in TInputType input) where TInputType : struct {
      if (!_inputRunners.TryGetValue(typeof(TInputType), out var runners)) {
        return;
      }

      // Execute all applicable input bindings.
      foreach (var runner in runners) {
        // Invoke the handler for this input type.
        (runner as InputAction<TInputType>)!(in input);
      }
    }

    protected override void ReceiveState(TState state) {
      // Execute all applicable state bindings.
      for (var i = 0; i < _stateCheckers.Count; i++) {
        var checker = _stateCheckers[i];
        var runner = _stateRunners[i];
        if (checker(state)) {
          // Invoke the handler for this state type.
          runner(state);
        }
      }
    }

    protected override void ReceiveOutput<TOutputType>(in TOutputType output) where TOutputType : struct {
      if (!_outputRunners.TryGetValue(typeof(TOutputType), out var runners)) {
        return;
      }

      // Execute all applicable output bindings.
      foreach (var runner in runners) {
        // Invoke the handler for this output type.
        (runner as OutputAction<TOutputType>)!(in output);
      }
    }

    protected override void ReceiveException(Exception e) {
      // Execute all applicable error bindings.
      for (var i = 0; i < _exceptionCheckers.Count; i++) {
        var checker = _exceptionCheckers[i];
        var runner = _exceptionRunners[i];
        if (checker(e)) {
          // Invoke the handler for this error type.
          runner(e);
        }
      }
    }

    protected override void Cleanup() {
      _inputRunners.Clear();
      _outputRunners.Clear();
      _stateCheckers.Clear();
      _stateRunners.Clear();
      _exceptionCheckers.Clear();
      _exceptionRunners.Clear();
    }
  }

  internal class Binding : BindingBase {
    /// <summary>State chart being listened to.</summary>
    public StateChart<TState> StateChart { get; }

    internal Binding(StateChart<TState> stateChart) {
      StateChart = stateChart;
      stateChart.AddBinding(this);
    }

    protected override void Cleanup() {
      StateChart.RemoveBinding(this);
      base.Cleanup();
    }
  }
}
