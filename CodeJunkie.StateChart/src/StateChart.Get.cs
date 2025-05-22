namespace CodeJunkie.StateChart;

public abstract partial class StateChart<TState> {
  /// <summary>
  /// Defines an interface for handling specific types of inputs within a state chart.
  /// States that wish to process a particular type of input must implement this interface.
  /// </summary>
  /// <typeparam name="TInputType">The type of input that the state can handle.</typeparam>
  public interface IGet<TInputType> where TInputType : struct {
    /// <summary>
    /// Handles an input of type <typeparamref name="TInputType"/> and determines the next state.
    /// This method is invoked when the state receives an input of the specified type.
    /// </summary>
    /// <param name="input">The input value to process.</param>
    /// <returns>
    /// A <see cref="Transition"/> object representing the next state or action
    /// to be taken based on the input.
    /// </returns>
    Transition On(in TInputType input);
  }
}
