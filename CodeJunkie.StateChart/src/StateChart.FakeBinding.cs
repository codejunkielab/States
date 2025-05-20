namespace CodeJunkie.StateChart;

using System;

public abstract partial class StateChart<TState> {
  /// <summary>
  /// Fake binding that allows bindings to be triggered manually. Makes testing
  /// objects that bind to statess easier.
  /// </summary>
  public interface IFakeBinding : IBinding {
    /// <summary>
    /// Triggers bindings to run in response to a state change.
    /// </summary>
    /// <param name="state">State.</param>
    void SetState(TState state);
    /// <summary>
    /// Triggers bindings to run in response to a new input.
    /// </summary>
    /// <typeparam name="TInputType">Input type.</typeparam>
    /// <param name="input">Input.</param>
    void Input<TInputType>(in TInputType input) where TInputType : struct;
    /// <summary>
    /// Triggers bindings to run in response to an output.
    /// </summary>
    /// <typeparam name="TOutputType">Output type.</typeparam>
    /// <param name="output">Output.</param>
    void Output<TOutputType>(in TOutputType output) where TOutputType : struct;
    /// <summary>
    /// Triggers bindings to run in response to an error.
    /// </summary>
    /// <param name="error">Error.</param>
    void AddError(Exception error);
  }

  internal sealed class FakeBinding : BindingBase, IFakeBinding {
    internal FakeBinding() { }

    public void Input<TInputType>(in TInputType input)
    where TInputType : struct =>
      (this as IStateChartBinding<TState>).MonitorInput(input);
    public void SetState(TState state) =>
      (this as IStateChartBinding<TState>).MonitorState(state);
    public void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct =>
      (this as IStateChartBinding<TState>).MonitorOutput(output);
    public void AddError(Exception error) =>
      (this as IStateChartBinding<TState>).MonitorException(error);
  }
}
