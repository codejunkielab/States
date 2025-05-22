namespace CodeJunkie.StateChart;

using System;
using System.Diagnostics.CodeAnalysis;

public abstract partial class StateChart<TState> {
  /// <summary>Represents a transition to a new state.</summary>
  public readonly struct Transition {
    /// <summary>
    /// Gets the target state to which the transition is directed.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Constructor for internal use only. Do not instantiate transitions directly.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Obsolete("This constructor is for internal use only. Use To<T>() or ToSelf() instead.", error: true)]
    public Transition() {
      throw new NotSupportedException("This constructor is for internal use only. Use To<T>() or ToSelf() instead.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transition"/> struct.
    /// </summary>
    /// <param name="state">
    /// The target state for the transition.
    /// </param>
    internal Transition(TState state) {
      State = state;
    }

    /// <summary>
    /// Executes a specified action on the target state before completing the transition.
    /// This allows for additional setup or validation logic to be applied.
    /// </summary>
    /// <param name="action">
    /// The action to perform on the target state. This action is executed
    /// immediately and receives the target state as its parameter.
    /// </param>
    /// <returns>
    /// Returns the current <see cref="Transition"/> instance, enabling method chaining.
    /// </returns>
    public Transition With(Action<TState> action) {
      action(State);
      return this;
    }
  }
}
