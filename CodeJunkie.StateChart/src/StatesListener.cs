namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// <para>
/// State chart listener. Receives callbacks for inputs, states, outputs, and
/// exceptions that the states encounters.
/// </para>
/// <para>
/// For the sake of performance, StateCharts cannot be subscribed to with events
/// or observables. Instead, simply subclass this when you need to listen to
/// every input, state, output, and/or exception that a states encounters.
/// </para>
/// <para>
/// The generics are required on
/// <see cref="ReceiveInput{TInputType}(in TInputType)" />
/// and <see cref="ReceiveOutput{TOutputType}(in TOutputType)" /> to
/// prevent inputs and outputs from unnecessarily hitting the heap.
/// </para>
/// </summary>
/// <typeparam name="TState">State type.</typeparam>
public abstract class StateChartListenerBase<TState> :
IStateChartBinding<TState>, IDisposable
where TState : StateLogic<TState> {
  /// <summary>
  /// <para>
  /// Creates a new states listener.
  /// </para>
  /// <para>
  /// A states listener receives callbacks for inputs, states, outputs, and
  /// exceptions that the states encounters.
  /// </para>
  /// </summary>
  protected StateChartListenerBase() { }

  /// <summary>
  /// Called whenever the states receives an input.
  /// </summary>
  /// <typeparam name="TInputType">Input type.</typeparam>
  /// <param name="input">Input object.</param>
  protected virtual void ReceiveInput<TInputType>(in TInputType input)
    where TInputType : struct { }

  /// <summary>
  /// Called whenever the states transitions to a new state.
  /// </summary>
  /// <param name="state">New state.</param>
  protected virtual void ReceiveState(TState state) { }

  /// <summary>
  /// Called whenever the states produces an output.
  /// </summary>
  /// <typeparam name="TOutputType">Output type.</typeparam>
  /// <param name="output">Output.</param>
  protected virtual void ReceiveOutput<TOutputType>(in TOutputType output)
    where TOutputType : struct { }

  /// <summary>
  /// Called whenever the states encounters an exception.
  /// </summary>
  /// <param name="e">Exception object.</param>
  protected virtual void ReceiveException(Exception e) { }

  void IStateChartBinding<TState>.MonitorInput<TInputType>(
    in TInputType input
  ) => ReceiveInput(in input);

  void IStateChartBinding<TState>.MonitorState(TState state) =>
    ReceiveState(state);

  void IStateChartBinding<TState>.MonitorOutput<TOutputType>(
    in TOutputType output
  ) => ReceiveOutput(in output);

  void IStateChartBinding<TState>.MonitorException(Exception exception) =>
    ReceiveException(exception);

  /// <summary>
  /// Override this method to perform custom cleanup for your listener. This is
  /// called when the listener is disposed. Be sure to call the base method so
  /// this can unsubscribe from the states its listening to.
  /// </summary>
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
public class StateChartListener<TState> : StateChartListenerBase<TState>
where TState : StateLogic<TState> {
  /// <summary>State chart being listened to.</summary>
  public IStateChart<TState> StateChart { get; }

  /// <inheritdoc cref="StateChartListenerBase{TState}" />
  /// <param name="stateChart">State chart to listen to.</param>
  public StateChartListener(IStateChart<TState> stateChart) {
    StateChart = stateChart;
    StateChart.AddBinding(this);
  }

  /// <inheritdoc />
  protected override void Cleanup() => StateChart.RemoveBinding(this);
}
