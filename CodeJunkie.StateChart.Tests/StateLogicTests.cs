namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for individual State behavior using CreateFakeContext pattern.
/// Based on LogicBlocks testing approach for state-level unit tests.
/// </summary>
public sealed partial class StateLogicTests {
  // Test StateChart for basic state testing
  [Meta, StateChart(typeof(State))]
  public partial class TestStateChart : StateChart<TestStateChart.State> {
    public static class Input {
      public readonly record struct GoToB;
      public readonly record struct GoToA;
      public readonly record struct StayHere;
      public readonly record struct IncrementCounter;
      public readonly record struct CauseError;
    }

    public static class Output {
      public readonly record struct EnteredA;
      public readonly record struct ExitedA;
      public readonly record struct EnteredB;
      public readonly record struct ExitedB;
      public readonly record struct EnteredFromA;
      public readonly record struct ExitingToB;
      public readonly record struct EnteredSelf;
      public readonly record struct CounterIncremented(int Value);
    }

    public class TestCounter {
      public int Value { get; set; }
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.GoToB> {
        public StateA() {
          this.OnEnter(() => Output(new Output.EnteredA()));
          this.OnExit(() => Output(new Output.ExitedA()));
        }

        public Transition On(in Input.GoToB input) => To<StateB>();
      }

      public sealed record StateB : State, IGet<Input.GoToA> {
        public StateB() {
          this.OnEnter(() => Output(new Output.EnteredB()));
          this.OnExit(() => Output(new Output.ExitedB()));
        }

        public Transition On(in Input.GoToA input) => To<StateA>();
      }

      public sealed record ConditionalState : State {
        public ConditionalState() {
          this.OnEnter(() => {
            // Can check Context for conditional logic if needed
            Output(new Output.EnteredSelf());
          });
        }
      }

      public sealed record SelfTransitionState : State, IGet<Input.StayHere> {
        public SelfTransitionState() {
          this.OnEnter(() => Output(new Output.EnteredSelf()));
        }

        public Transition On(in Input.StayHere input) => ToSelf();
      }

      public sealed record DataState : State, IGet<Input.IncrementCounter> {
        public Transition On(in Input.IncrementCounter input) {
          var counter = Get<TestCounter>();
          counter.Value++;
          Output(new Output.CounterIncremented(counter.Value));
          return ToSelf();
        }
      }

      public sealed record ErrorState : State, IGet<Input.CauseError> {
        public Transition On(in Input.CauseError input) {
          AddError(new InvalidOperationException("Test error"));
          return ToSelf();
        }
      }

      public sealed record AttachDetachState : State {
        public static int AttachCount = 0;
        public static int DetachCount = 0;

        public AttachDetachState() {
          this.OnAttach(() => AttachCount++);
          this.OnDetach(() => DetachCount++);
        }

        public static void Reset() {
          AttachCount = 0;
          DetachCount = 0;
        }
      }

      public sealed record MultipleCallbacksState : State {
        public static List<string> CallbackOrder = new();

        public MultipleCallbacksState() {
          this.OnEnter(() => CallbackOrder.Add("Enter1"));
          this.OnEnter(() => CallbackOrder.Add("Enter2"));
          this.OnEnter(() => CallbackOrder.Add("Enter3"));

          this.OnExit(() => CallbackOrder.Add("Exit1"));
          this.OnExit(() => CallbackOrder.Add("Exit2"));
          this.OnExit(() => CallbackOrder.Add("Exit3"));
        }

        public static void Reset() {
          CallbackOrder.Clear();
        }
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();
  }

  [Fact]
  public void State_CanBeCreated() {
    // Arrange & Act
    var state = new TestStateChart.State.StateA();

    // Assert
    state.ShouldNotBeNull();
  }

  [Fact]
  public void State_Enter_ExecutesEnterCallbacks() {
    // Arrange
    var state = new TestStateChart.State.StateA();
    var context = state.CreateFakeContext();

    // Act
    state.Enter();

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.EnteredA>();
  }

  [Fact]
  public void State_Exit_ExecutesExitCallbacks() {
    // Arrange
    var state = new TestStateChart.State.StateA();
    var context = state.CreateFakeContext();

    // Act
    state.Exit();

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.ExitedA>();
  }

  [Fact]
  public void State_Enter_WithPreviousStateType_ExecutesCallbacks() {
    // Arrange
    var state = new TestStateChart.State.ConditionalState();
    var context = state.CreateFakeContext();
    var previousState = new TestStateChart.State.StateA();

    // Act - Enter with previous state
    state.Enter(previousState);

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.EnteredSelf>();
  }

  [Fact]
  public void State_Enter_UsingGenericMethod_ExecutesCallbacks() {
    // Arrange
    var state = new TestStateChart.State.ConditionalState();
    var context = state.CreateFakeContext();

    // Act - Enter using generic method
    state.Enter<TestStateChart.State.StateA>();

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.EnteredSelf>();
  }

  [Fact]
  public void State_Exit_WithNextStateType_ExecutesCallbacks() {
    // Arrange
    var state = new TestStateChart.State.StateB();
    var context = state.CreateFakeContext();
    var nextState = new TestStateChart.State.StateA();

    // Act - Exit with next state
    state.Exit(nextState);

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.ExitedB>();
  }

  [Fact]
  public void State_Exit_UsingGenericMethod_ExecutesCallbacks() {
    // Arrange
    var state = new TestStateChart.State.StateB();
    var context = state.CreateFakeContext();

    // Act - Exit using generic method
    state.Exit<TestStateChart.State.StateA>();

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.ExitedB>();
  }

  [Fact]
  public void State_InputHandler_ExecutesAndTransitionsState() {
    // Arrange
    var state = new TestStateChart.State.StateA();
    var context = state.CreateFakeContext();
    context.Set(new TestStateChart.State.StateB());

    // Act
    var transition = state.On(new TestStateChart.Input.GoToB());

    // Assert
    transition.State.ShouldBeOfType<TestStateChart.State.StateB>();
  }

  [Fact]
  public void State_Output_ProducesOutput() {
    // Arrange
    var state = new TestStateChart.State.StateA();
    var context = state.CreateFakeContext();

    // Act
    state.Enter();

    // Assert
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    outputs[0].ShouldBeOfType<TestStateChart.Output.EnteredA>();
  }

  [Fact]
  public void State_ToSelf_ReturnsCurrentState() {
    // Arrange
    var state = new TestStateChart.State.SelfTransitionState();
    var context = state.CreateFakeContext();

    // Act
    var transition = state.On(new TestStateChart.Input.StayHere());

    // Assert
    transition.State.ShouldBeSameAs(state);
  }

  [Fact]
  public void State_Get_RetrievesFromBlackboard() {
    // Arrange
    var state = new TestStateChart.State.DataState();
    var context = state.CreateFakeContext();
    var counter = new TestStateChart.TestCounter { Value = 5 };
    context.Set(counter);

    // Act
    var transition = state.On(new TestStateChart.Input.IncrementCounter());

    // Assert
    counter.Value.ShouldBe(6);
    var outputs = context.Outputs.ToList();
    outputs.Count.ShouldBe(1);
    var output = outputs[0].ShouldBeOfType<TestStateChart.Output.CounterIncremented>();
    output.Value.ShouldBe(6);
  }

  [Fact]
  public void State_AddError_AddsErrorToContext() {
    // Arrange
    var state = new TestStateChart.State.ErrorState();
    var context = state.CreateFakeContext();

    // Act
    var transition = state.On(new TestStateChart.Input.CauseError());

    // Assert
    var errors = context.Errors.ToList();
    errors.Count.ShouldBe(1);
    var error = errors[0];
    error.ShouldBeOfType<InvalidOperationException>();
    error.Message.ShouldBe("Test error");
  }

  [Fact]
  public void State_OnAttach_ExecutesAttachCallbacks() {
    // Arrange
    TestStateChart.State.AttachDetachState.Reset();
    var state = new TestStateChart.State.AttachDetachState();
    var context = state.CreateFakeContext();

    // Act
    state.Attach(context);

    // Assert
    TestStateChart.State.AttachDetachState.AttachCount.ShouldBe(1);
    TestStateChart.State.AttachDetachState.DetachCount.ShouldBe(0);
  }

  [Fact]
  public void State_OnDetach_ExecutesDetachCallbacks() {
    // Arrange
    TestStateChart.State.AttachDetachState.Reset();
    var state = new TestStateChart.State.AttachDetachState();
    var context = state.CreateFakeContext();
    state.Attach(context);

    // Act
    state.Detach();

    // Assert
    TestStateChart.State.AttachDetachState.AttachCount.ShouldBe(1);
    TestStateChart.State.AttachDetachState.DetachCount.ShouldBe(1);
  }

  [Fact]
  public void State_CreateFakeContext_CreatesNewContext() {
    // Arrange
    var state = new TestStateChart.State.StateA();

    // Act
    var context = state.CreateFakeContext();

    // Assert
    context.ShouldNotBeNull();
    context.Inputs.ShouldBeEmpty();
    context.Outputs.ShouldBeEmpty();
    context.Errors.ShouldBeEmpty();
  }

  [Fact]
  public void State_MultipleEnterCallbacks_ExecuteInOrder() {
    // Arrange
    TestStateChart.State.MultipleCallbacksState.Reset();
    var state = new TestStateChart.State.MultipleCallbacksState();
    var context = state.CreateFakeContext();

    // Act
    state.Enter();

    // Assert
    var order = TestStateChart.State.MultipleCallbacksState.CallbackOrder;
    order.Count.ShouldBe(3);
    order.ShouldBe(new[] { "Enter1", "Enter2", "Enter3" });
  }

  [Fact]
  public void State_MultipleExitCallbacks_ExecuteInReverseOrder() {
    // Arrange
    TestStateChart.State.MultipleCallbacksState.Reset();
    var state = new TestStateChart.State.MultipleCallbacksState();
    var context = state.CreateFakeContext();

    // Act
    state.Exit();

    // Assert
    var order = TestStateChart.State.MultipleCallbacksState.CallbackOrder;
    order.Count.ShouldBe(3);
    order.ShouldBe(new[] { "Exit3", "Exit2", "Exit1" });
  }
}
