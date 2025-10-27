namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for StateChart binding system that allows external monitoring of states, inputs, outputs, and errors.
/// Based on LogicBlocks binding testing patterns.
/// </summary>
public sealed partial class StateChartBindingTests : IDisposable {
  // StateChart for binding tests
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class BindingTestStateChart : StateChart<BindingTestStateChart.State> {
    public static class Input {
      public readonly record struct InputA;
      public readonly record struct InputB;
      public readonly record struct CauseError;
    }

    public static class Output {
      public readonly record struct OutputA;
      public readonly record struct OutputB;
      public readonly record struct OutputC;
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.InputA>, IGet<Input.CauseError> {
        public StateA() {
          this.OnEnter(() => Output(new Output.OutputA()));
        }

        public Transition On(in Input.InputA input) => To<StateB>();

        public Transition On(in Input.CauseError input) {
          throw new InvalidOperationException("Test error");
        }
      }

      public sealed record StateB : State, IGet<Input.InputB> {
        public StateB() {
          this.OnEnter(() => {
            Output(new Output.OutputB());
            Output(new Output.OutputC());
          });
        }

        public Transition On(in Input.InputB input) => To<StateA>();
      }

      public sealed record StateC : State {
        public StateC() {
          this.OnEnter(() => Output(new Output.OutputA()));
        }
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();
  }

  public StateChartBindingTests() {
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void Bind_CreatesBinding() {
    // Arrange
    var stateChart = new BindingTestStateChart();

    // Act
    var binding = stateChart.Bind();

    // Assert
    binding.ShouldNotBeNull();
  }

  [Fact]
  public void Watch_MonitorsInputs() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var inputs = new List<BindingTestStateChart.Input.InputA>();

    stateChart.Bind()
      .Watch<BindingTestStateChart.Input.InputA>((in BindingTestStateChart.Input.InputA input) => {
        inputs.Add(input);
      });

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    inputs.Count.ShouldBe(1);
  }

  [Fact]
  public void Watch_MonitorsMultipleInputTypes() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var inputACount = 0;
    var inputBCount = 0;

    stateChart.Bind()
      .Watch<BindingTestStateChart.Input.InputA>((in BindingTestStateChart.Input.InputA input) => inputACount++)
      .Watch<BindingTestStateChart.Input.InputB>((in BindingTestStateChart.Input.InputB input) => inputBCount++);

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());
    stateChart.Input(new BindingTestStateChart.Input.InputB());
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    inputACount.ShouldBe(2);
    inputBCount.ShouldBe(1);
  }

  [Fact]
  public void When_MonitorsStateChanges() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var states = new List<BindingTestStateChart.State>();

    stateChart.Bind()
      .When<BindingTestStateChart.State>((state) => states.Add(state));

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    states.Count.ShouldBe(2); // Initial StateA + StateB
    states[0].ShouldBeOfType<BindingTestStateChart.State.StateA>();
    states[1].ShouldBeOfType<BindingTestStateChart.State.StateB>();
  }

  [Fact]
  public void When_MonitorsSpecificStateType() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var stateBCount = 0;

    stateChart.Bind()
      .When<BindingTestStateChart.State.StateB>((state) => stateBCount++);

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());
    stateChart.Input(new BindingTestStateChart.Input.InputB());
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    stateBCount.ShouldBe(2); // StateB entered twice
  }

  [Fact]
  public void Handle_MonitorsOutputs() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var outputs = new List<BindingTestStateChart.Output.OutputA>();

    stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => {
        outputs.Add(output);
      });

    // Act
    stateChart.Start();

    // Assert
    outputs.Count.ShouldBe(1); // OutputA from StateA's OnEnter
  }

  [Fact]
  public void Handle_MonitorsMultipleOutputTypes() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var outputACount = 0;
    var outputBCount = 0;
    var outputCCount = 0;

    stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => outputACount++)
      .Handle<BindingTestStateChart.Output.OutputB>((in BindingTestStateChart.Output.OutputB output) => outputBCount++)
      .Handle<BindingTestStateChart.Output.OutputC>((in BindingTestStateChart.Output.OutputC output) => outputCCount++);

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    outputACount.ShouldBe(1); // From StateA
    outputBCount.ShouldBe(1); // From StateB
    outputCCount.ShouldBe(1); // From StateB
  }

  [Fact]
  public void Catch_MonitorsExceptions() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var exceptions = new List<InvalidOperationException>();

    stateChart.Bind()
      .Catch<InvalidOperationException>((ex) => exceptions.Add(ex));

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.CauseError());

    // Assert
    exceptions.Count.ShouldBe(1);
    exceptions[0].Message.ShouldBe("Test error");
  }

  [Fact]
  public void Catch_MonitorsMultipleExceptionTypes() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var invalidOpCount = 0;
    var exceptionCount = 0;

    stateChart.Bind()
      .Catch<InvalidOperationException>((ex) => invalidOpCount++)
      .Catch<Exception>((ex) => exceptionCount++);

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.CauseError());

    // Assert
    invalidOpCount.ShouldBe(1);
    exceptionCount.ShouldBe(1); // Base Exception also matches
  }

  [Fact]
  public void Binding_SupportsMultipleHandlersForSameType() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var handler1Called = false;
    var handler2Called = false;

    stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => handler1Called = true)
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => handler2Called = true);

    // Act
    stateChart.Start();

    // Assert
    handler1Called.ShouldBeTrue();
    handler2Called.ShouldBeTrue();
  }

  [Fact]
  public void Binding_SupportsFluentChaining() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var inputReceived = false;
    var stateReceived = false;
    var outputReceived = false;

    // Act
    var binding = stateChart.Bind()
      .Watch<BindingTestStateChart.Input.InputA>((in BindingTestStateChart.Input.InputA input) => inputReceived = true)
      .When<BindingTestStateChart.State.StateB>((state) => stateReceived = true)
      .Handle<BindingTestStateChart.Output.OutputB>((in BindingTestStateChart.Output.OutputB output) => outputReceived = true);

    stateChart.Start();
    stateChart.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    inputReceived.ShouldBeTrue();
    stateReceived.ShouldBeTrue();
    outputReceived.ShouldBeTrue();
    binding.ShouldNotBeNull();
  }

  [Fact]
  public void Dispose_RemovesBinding() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var callCount = 0;

    var binding = stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => callCount++);

    stateChart.Start(); // OutputA from StateA
    callCount.ShouldBe(1);

    // Act
    binding.Dispose();

    // Force a state change to StateC which also outputs OutputA
    stateChart.ForceReset(new BindingTestStateChart.State.StateC());

    // Assert - Handler not called after dispose
    callCount.ShouldBe(1); // Still 1, not incremented
  }

  [Fact]
  public void AddBinding_RemoveBinding_ManualManagement() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var callCount = 0;

    var binding = stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => callCount++);

    stateChart.Start();
    callCount.ShouldBe(1);

    // Act - Remove binding manually
    stateChart.RemoveBinding((IStateChartBinding<BindingTestStateChart.State>)binding);

    stateChart.ForceReset(new BindingTestStateChart.State.StateC());

    // Assert
    callCount.ShouldBe(1); // Not incremented after removal
  }

  [Fact]
  public void FakeBinding_SetState_TriggersWhenCallbacks() {
    // Arrange
    var fakeBinding = StateChart<BindingTestStateChart.State>.CreateFakeBinding();
    var states = new List<BindingTestStateChart.State>();

    fakeBinding.When<BindingTestStateChart.State>((state) => states.Add(state));

    // Act
    var state = new BindingTestStateChart.State.StateA();
    fakeBinding.SetState(state);

    // Assert
    states.Count.ShouldBe(1);
    states[0].ShouldBeSameAs(state);
  }

  [Fact]
  public void FakeBinding_Input_TriggersWatchCallbacks() {
    // Arrange
    var fakeBinding = StateChart<BindingTestStateChart.State>.CreateFakeBinding();
    var inputs = new List<BindingTestStateChart.Input.InputA>();

    fakeBinding.Watch<BindingTestStateChart.Input.InputA>((in BindingTestStateChart.Input.InputA input) => {
      inputs.Add(input);
    });

    // Act
    fakeBinding.Input(new BindingTestStateChart.Input.InputA());

    // Assert
    inputs.Count.ShouldBe(1);
  }

  [Fact]
  public void FakeBinding_Output_TriggersHandleCallbacks() {
    // Arrange
    var fakeBinding = StateChart<BindingTestStateChart.State>.CreateFakeBinding();
    var outputs = new List<BindingTestStateChart.Output.OutputA>();

    fakeBinding.Handle<BindingTestStateChart.Output.OutputA>((in BindingTestStateChart.Output.OutputA output) => {
      outputs.Add(output);
    });

    // Act
    fakeBinding.Output(new BindingTestStateChart.Output.OutputA());

    // Assert
    outputs.Count.ShouldBe(1);
  }

  [Fact]
  public void FakeBinding_AddError_TriggersCatchCallbacks() {
    // Arrange
    var fakeBinding = StateChart<BindingTestStateChart.State>.CreateFakeBinding();
    var errors = new List<Exception>();

    fakeBinding.Catch<InvalidOperationException>((ex) => errors.Add(ex));

    // Act
    var exception = new InvalidOperationException("Test");
    fakeBinding.AddError(exception);

    // Assert
    errors.Count.ShouldBe(1);
    errors[0].ShouldBeSameAs(exception);
  }

  [Fact]
  public void Binding_DoesNotAffectStateTransitions() {
    // Arrange
    var stateChart = new BindingTestStateChart();
    var outputCount = 0;

    stateChart.Bind()
      .Handle<BindingTestStateChart.Output.OutputB>((in BindingTestStateChart.Output.OutputB output) => outputCount++);

    stateChart.Start();

    // Act
    stateChart.Input(new BindingTestStateChart.Input.InputA());
    var currentState = stateChart.Value;

    // Assert
    currentState.ShouldBeOfType<BindingTestStateChart.State.StateB>();
    outputCount.ShouldBe(1);
  }
}
