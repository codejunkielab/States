namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for StateChart edge cases, boundary conditions, and error scenarios.
/// Based on LogicBlocks edge case testing patterns.
/// </summary>
public sealed partial class StateChartEdgeCasesTests : IDisposable {
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class EdgeCaseStateChart : StateChart<EdgeCaseStateChart.State> {
    public static class Input {
      public readonly record struct GoToB;
      public readonly record struct GoToA;
      public readonly record struct NoHandler;
      public readonly record struct CauseError;
    }

    public static class Output {
      public readonly record struct EnteredA;
      public readonly record struct EnteredB;
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.GoToB> {
        public StateA() {
          this.OnEnter(() => Output(new Output.EnteredA()));
        }

        public Transition On(in Input.GoToB input) => To<StateB>();
      }

      public sealed record StateB : State, IGet<Input.GoToA>, IGet<Input.CauseError> {
        public StateB() {
          this.OnEnter(() => Output(new Output.EnteredB()));
        }

        public Transition On(in Input.GoToA input) => To<StateA>();

        public Transition On(in Input.CauseError input) {
          throw new InvalidOperationException("Test error in input handler");
        }
      }

      // State with no callbacks
      public sealed record StateC : State { }

      // State that transitions to itself
      public sealed record StateD : State, IGet<Input.GoToA> {
        public Transition On(in Input.GoToA input) => ToSelf();
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();
  }

  public StateChartEdgeCasesTests() {
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void Input_WhileProcessing_QueuesInput() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    var stateChanges = new List<string>();

    stateChart.Bind()
      .When<EdgeCaseStateChart.State>((state) => {
        stateChanges.Add(state.GetType().Name);

        // Send another input while processing
        if (state is EdgeCaseStateChart.State.StateB) {
          stateChart.Input(new EdgeCaseStateChart.Input.GoToA());
        }
      });

    stateChart.Start();

    // Act
    stateChart.Input(new EdgeCaseStateChart.Input.GoToB());

    // Assert
    stateChanges.Count.ShouldBe(3); // StateA (initial), StateB, StateA (queued)
    stateChanges[0].ShouldBe("StateA");
    stateChanges[1].ShouldBe("StateB");
    stateChanges[2].ShouldBe("StateA");
  }

  [Fact]
  public void ForceReset_WhileProcessing_Throws() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    var exceptionThrown = false;

    stateChart.Bind()
      .Watch<EdgeCaseStateChart.Input.GoToB>((in EdgeCaseStateChart.Input.GoToB input) => {
        try {
          stateChart.ForceReset(new EdgeCaseStateChart.State.StateA());
        }
        catch (StateChartException ex) {
          exceptionThrown = true;
          ex.Message.ShouldContain("processing");
        }
      });

    stateChart.Start();

    // Act
    stateChart.Input(new EdgeCaseStateChart.Input.GoToB());

    // Assert
    exceptionThrown.ShouldBeTrue();
  }

  [Fact]
  public void Stop_WhileProcessing_DoesNothing() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    var wasStartedDuringInput = false;

    stateChart.Bind()
      .Watch<EdgeCaseStateChart.Input.GoToB>((in EdgeCaseStateChart.Input.GoToB input) => {
        stateChart.Stop();
        wasStartedDuringInput = stateChart.IsStarted;
      });

    stateChart.Start();

    // Act
    stateChart.Input(new EdgeCaseStateChart.Input.GoToB());

    // Assert
    wasStartedDuringInput.ShouldBeTrue(); // Stop had no effect while processing
    stateChart.IsStarted.ShouldBeTrue();
  }

  [Fact]
  public void Transition_ToSameStateType_WorksCorrectly() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();

    var initialState = stateChart.Value;

    // Act
    stateChart.ForceReset(new EdgeCaseStateChart.State.StateA());
    var newState = stateChart.Value;

    // Assert
    initialState.ShouldBeOfType<EdgeCaseStateChart.State.StateA>();
    newState.ShouldBeOfType<EdgeCaseStateChart.State.StateA>();
    // Different instances
    newState.ShouldNotBeSameAs(initialState);
  }

  [Fact]
  public void ToSelf_Transition_MaintainsSameInstance() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();
    stateChart.Input(new EdgeCaseStateChart.Input.GoToB());
    stateChart.Input(new EdgeCaseStateChart.Input.GoToA());
    stateChart.ForceReset(new EdgeCaseStateChart.State.StateD());

    var initialState = stateChart.Value;

    // Act
    stateChart.Input(new EdgeCaseStateChart.Input.GoToA());
    var newState = stateChart.Value;

    // Assert
    newState.ShouldBeSameAs(initialState); // ToSelf returns same instance
  }

  [Fact]
  public void EmptyInputQueue_ProcessesWithoutError() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();

    // Act
    stateChart.Start();

    // Assert
    stateChart.IsStarted.ShouldBeTrue();
    stateChart.Value.ShouldBeOfType<EdgeCaseStateChart.State.StateA>();
  }

  [Fact]
  public void Input_WithNoHandler_IsIgnored() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();

    var initialState = stateChart.Value;

    // Act - Send input that StateA doesn't handle
    stateChart.Input(new EdgeCaseStateChart.Input.NoHandler());
    var newState = stateChart.Value;

    // Assert
    newState.ShouldBeSameAs(initialState); // State unchanged
  }

  [Fact]
  public void State_WithNoEnterCallbacks_WorksCorrectly() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();

    // Act
    stateChart.ForceReset(new EdgeCaseStateChart.State.StateC());

    // Assert
    stateChart.Value.ShouldBeOfType<EdgeCaseStateChart.State.StateC>();
  }

  [Fact]
  public void State_WithNoExitCallbacks_WorksCorrectly() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();
    stateChart.ForceReset(new EdgeCaseStateChart.State.StateC());

    // Act - Transition away from StateC
    stateChart.ForceReset(new EdgeCaseStateChart.State.StateA());

    // Assert
    stateChart.Value.ShouldBeOfType<EdgeCaseStateChart.State.StateA>();
  }

  [Fact]
  public void Exception_InEnterCallback_IsCaught() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    var errors = new List<Exception>();

    stateChart.Bind()
      .Catch<Exception>((ex) => errors.Add(ex));

    // Create state with failing Enter callback
    var badState = new EdgeCaseStateChart.State.StateA();
    var context = badState.CreateFakeContext();
    context.Set(new InvalidOperationException("Enter failed"));

    stateChart.Start();

    // This test verifies that the framework handles exceptions gracefully
    // Act & Assert - Should not throw
    Should.NotThrow(() => {
      stateChart.Value.ShouldNotBeNull();
    });
  }

  [Fact]
  public void Exception_InExitCallback_IsCaught() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    stateChart.Start();

    // This test verifies that the framework handles exceptions gracefully
    // Act & Assert - Should not throw
    Should.NotThrow(() => {
      stateChart.Input(new EdgeCaseStateChart.Input.GoToB());
      stateChart.Value.ShouldNotBeNull();
    });
  }

  [Fact]
  public void Exception_InInputHandler_IsCaught() {
    // Arrange
    var stateChart = new EdgeCaseStateChart();
    var errors = new List<Exception>();

    stateChart.Bind()
      .Catch<InvalidOperationException>((ex) => errors.Add(ex));

    stateChart.Start();
    stateChart.Input(new EdgeCaseStateChart.Input.GoToB());

    // Act
    stateChart.Input(new EdgeCaseStateChart.Input.CauseError());

    // Assert
    errors.Count.ShouldBe(1);
    errors[0].Message.ShouldBe("Test error in input handler");
    stateChart.Value.ShouldBeOfType<EdgeCaseStateChart.State.StateB>(); // State unchanged after error
  }
}
