namespace CodeJunkie.StateChart;

public abstract partial class StateChart<TState> {
  /// <summary>
  /// Input handler interface. State chart states must implement this interface
  /// for each type of input they wish to handle.
  /// </summary>
  /// <typeparam name="TInputType">Type of input to handle.</typeparam>
  public interface IGet<TInputType> where TInputType : struct {
    /// <summary>
    /// Method invoked on the state when the states receives an input of
    /// the corresponding type <typeparamref name="TInputType"/>.
    /// </summary>
    /// <param name="input">Input value.</param>
    /// <returns>The next state of the states.</returns>
    Transition On(in TInputType input);
  }
}
