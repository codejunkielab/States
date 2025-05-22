namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Base class for state chart listeners.
/// </summary>
public abstract class StateChartListenerBase<TState> : IStateChartBinding<TState>, IDisposable where TState : StateLogic<TState> {
  /// <summary>
  /// Initializes a new instance of the <see cref="StateChartListenerBase{TState}"/> class.
  /// </summary>
  /// <remarks>
  /// This listener provides callbacks for inputs, state transitions, outputs, and exceptions encountered by the state chart.
  /// </remarks>
  protected StateChartListenerBase() { }

  /// <summary>
  /// Invoked when the state chart receives an input.
  /// </summary>
  /// <typeparam name="TInputType">The type of the input.</typeparam>
  /// <param name="input">The input object.</param>
  protected virtual void ReceiveInput<TInputType>(in TInputType input)
    where TInputType : struct { }

  /// <summary>
  /// Invoked when the state chart transitions to a new state.
  /// </summary>
  /// <param name="state">The new state.</param>
  protected virtual void ReceiveState(TState state) { }

  /// <summary>
  /// Invoked when the state chart produces an output.
  /// </summary>
  /// <typeparam name="TOutputType">The type of the output.</typeparam>
  /// <param name="output">The output object.</param>
  protected virtual void ReceiveOutput<TOutputType>(in TOutputType output)
    where TOutputType : struct { }

  /// <summary>
  /// Invoked when the state chart encounters an exception.
  /// </summary>
  /// <param name="e">The exception object.</param>
  protected virtual void ReceiveException(Exception e) { }

  void IStateChartBinding<TState>.MonitorInput<TInputType>(in TInputType input) =>
    ReceiveInput(in input);

  void IStateChartBinding<TState>.MonitorState(TState state) =>
    ReceiveState(state);

  void IStateChartBinding<TState>.MonitorOutput<TOutputType>(in TOutputType output) =>
    ReceiveOutput(in output);

  void IStateChartBinding<TState>.MonitorException(Exception exception) =>
    ReceiveException(exception);

  /// <summary>
  /// Performs custom cleanup for the listener when it is disposed.
  /// </summary>
  /// <remarks>
  /// Override this method to implement custom cleanup logic. Ensure that the base method is called to properly unsubscribe from the state chart.
  /// </remarks>
  protected abstract void Cleanup();

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing) {
    if (disposing) {
      Cleanup();
    }
  }

  /// <summary>Binding finalizer.</summary>
  ~StateChartListenerBase() {
    Dispose(false);
  }
}

/// <inheritdoc />
public class StateChartListener<TState> : StateChartListenerBase<TState> where TState : StateLogic<TState> {
  /// <summary>
  /// Gets the state chart being monitored by this listener.
  /// </summary>
  public IStateChart<TState> StateChart { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="StateChartListener{TState}"/> class.
  /// </summary>
  /// <param name="stateChart">The state chart to monitor.</param>
  public StateChartListener(IStateChart<TState> stateChart) {
    StateChart = stateChart;
    StateChart.AddBinding(this);
  }

  /// <inheritdoc />
  protected override void Cleanup() => StateChart.RemoveBinding(this);
}
