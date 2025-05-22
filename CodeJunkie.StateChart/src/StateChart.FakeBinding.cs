namespace CodeJunkie.StateChart;

using System;

public abstract partial class StateChart<TState> {
  /// <summary>
  /// Provides a fake binding mechanism that allows manual triggering of bindings.
  /// This is particularly useful for testing objects that bind to states.
  /// </summary>
  public interface IFakeBinding : IBinding {
    /// <summary>
    /// Manually triggers bindings in response to a state change.
    /// </summary>
    /// <param name="state">The new state to set.</param>
    void SetState(TState state);

    /// <summary>
    /// Manually triggers bindings in response to a new input.
    /// </summary>
    /// <typeparam name="TInputType">The type of the input.</typeparam>
    /// <param name="input">The input value to process.</param>
    void Input<TInputType>(in TInputType input) where TInputType : struct;

    /// <summary>
    /// Manually triggers bindings in response to an output.
    /// </summary>
    /// <typeparam name="TOutputType">The type of the output.</typeparam>
    /// <param name="output">The output value to process.</param>
    void Output<TOutputType>(in TOutputType output) where TOutputType : struct;

    /// <summary>
    /// Manually triggers bindings in response to an error.
    /// </summary>
    /// <param name="error">The exception to handle.</param>
    void AddError(Exception error);
  }

  /// <summary>
  /// A concrete implementation of <see cref="IFakeBinding"/> that provides
  /// mechanisms to manually monitor state changes, inputs, outputs, and errors.
  /// </summary>
  internal sealed class FakeBinding : BindingBase, IFakeBinding {
    /// <summary>
    /// Initializes a new instance of the <see cref="FakeBinding"/> class.
    /// </summary>
    internal FakeBinding() { }

    /// <inheritdoc />
    public void Input<TInputType>(in TInputType input) where TInputType : struct =>
      (this as IStateChartBinding<TState>).MonitorInput(input);

    /// <inheritdoc />
    public void SetState(TState state) =>
      (this as IStateChartBinding<TState>).MonitorState(state);

    /// <inheritdoc />
    public void Output<TOutputType>(in TOutputType output) where TOutputType : struct =>
      (this as IStateChartBinding<TState>).MonitorOutput(output);

    /// <inheritdoc />
    public void AddError(Exception error) =>
      (this as IStateChartBinding<TState>).MonitorException(error);
  }
}
