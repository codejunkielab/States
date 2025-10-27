namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for StateChart core functionality including lifecycle, input processing, and state management.
/// Based on LogicBlocks testing patterns for logic block-level tests.
/// </summary>
public sealed partial class StateChartCoreTests : IDisposable {
  // Simple StateChart for core testing
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class SimpleStateChart : StateChart<SimpleStateChart.State> {
    public static class Input {
      public readonly record struct GoToB;
      public readonly record struct GoToA;
      public readonly record struct Increment;
    }

    public static class Output {
      public readonly record struct EnteredA;
      public readonly record struct EnteredB;
      public readonly record struct ValueChanged(int Value);
    }

    public class Data {
      public int Value { get; set; }
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.GoToB> {
        public StateA() {
          this.OnEnter(() => Output(new Output.EnteredA()));
        }

        public Transition On(in Input.GoToB input) => To<StateB>();
      }

      public sealed record StateB : State, IGet<Input.GoToA>, IGet<Input.Increment> {
        public StateB() {
          this.OnEnter(() => Output(new Output.EnteredB()));
        }

        public Transition On(in Input.GoToA input) => To<StateA>();

        public Transition On(in Input.Increment input) {
          var data = Get<Data>();
          data.Value++;
          Output(new Output.ValueChanged(data.Value));
          return ToSelf();
        }
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();

    public SimpleStateChart() {
      Set(new Data());
    }
  }

  // StateChart for error handling tests
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class ErrorHandlingStateChart : StateChart<ErrorHandlingStateChart.State> {
    public static class Input {
      public readonly record struct CauseError;
    }

    public abstract record State : StateLogic<State> {
      public sealed record NormalState : State, IGet<Input.CauseError> {
        public Transition On(in Input.CauseError input) {
          throw new InvalidOperationException("Test exception");
        }
      }
    }

    public override Transition GetInitialState() => To<State.NormalState>();

    public List<Exception> HandledErrors { get; } = new();

    protected override void HandleError(Exception e) {
      HandledErrors.Add(e);
    }
  }

  // StateChart that tracks lifecycle events
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class LifecycleStateChart : StateChart<LifecycleStateChart.State> {
    public static class Input {
      public readonly record struct DoNothing;
    }

    public abstract record State : StateLogic<State> {
      public sealed record IdleState : State, IGet<Input.DoNothing> {
        public Transition On(in Input.DoNothing input) => ToSelf();
      }
    }

    public override Transition GetInitialState() => To<State.IdleState>();

    public bool OnStartCalled { get; private set; }
    public bool OnStopCalled { get; private set; }

    public override void OnStart() {
      OnStartCalled = true;
    }

    public override void OnStop() {
      OnStopCalled = true;
    }
  }

  public StateChartCoreTests() {
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void GetInitialState_ReturnsCorrectState() {
    // Arrange
    var stateChart = new SimpleStateChart();

    // Act
    var transition = stateChart.GetInitialState();

    // Assert
    transition.State.ShouldBeOfType<SimpleStateChart.State.StateA>();
  }

  [Fact]
  public void Start_InitializesToInitialState() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.IsStarted.ShouldBeFalse();

    // Act
    stateChart.Start();

    // Assert
    stateChart.IsStarted.ShouldBeTrue();
    stateChart.Value.ShouldBeOfType<SimpleStateChart.State.StateA>();
  }

  [Fact]
  public void Stop_ClearsState() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();
    stateChart.IsStarted.ShouldBeTrue();

    // Act
    stateChart.Stop();

    // Assert
    stateChart.IsStarted.ShouldBeFalse();
  }

  [Fact]
  public void Input_ProcessesSingleInput() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();

    // Act
    var result = stateChart.Input(new SimpleStateChart.Input.GoToB());

    // Assert
    result.ShouldBeOfType<SimpleStateChart.State.StateB>();
    stateChart.Value.ShouldBeOfType<SimpleStateChart.State.StateB>();
  }

  [Fact]
  public void Input_QueuesMultipleInputs() {
    // Arrange
    var stateChart = new SimpleStateChart();
    var outputs = new List<object>();
    stateChart.Bind()
      .Handle<SimpleStateChart.Output.EnteredA>((in SimpleStateChart.Output.EnteredA output) => outputs.Add(output))
      .Handle<SimpleStateChart.Output.EnteredB>((in SimpleStateChart.Output.EnteredB output) => outputs.Add(output));

    // Act - Queue inputs before Start
    stateChart.Input(new SimpleStateChart.Input.GoToB());
    stateChart.Input(new SimpleStateChart.Input.GoToA());
    stateChart.Start(); // Process all queued inputs

    // Assert
    stateChart.Value.ShouldBeOfType<SimpleStateChart.State.StateA>();
    outputs.Count.ShouldBe(3); // EnteredA (initial), EnteredB, EnteredA
  }

  [Fact]
  public void Input_ProcessesSequentially() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();
    var data = stateChart.Get<SimpleStateChart.Data>();

    // Act - Send multiple Increment inputs
    stateChart.Input(new SimpleStateChart.Input.GoToB());
    stateChart.Input(new SimpleStateChart.Input.Increment());
    stateChart.Input(new SimpleStateChart.Input.Increment());
    stateChart.Input(new SimpleStateChart.Input.Increment());

    // Assert - Each increment was processed in order
    data.Value.ShouldBe(3);
  }

  [Fact]
  public void StateTransition_ChangesState() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();
    var initialState = stateChart.Value;

    // Act
    stateChart.Input(new SimpleStateChart.Input.GoToB());
    var newState = stateChart.Value;

    // Assert
    initialState.ShouldBeOfType<SimpleStateChart.State.StateA>();
    newState.ShouldBeOfType<SimpleStateChart.State.StateB>();
    newState.GetType().ShouldNotBe(initialState.GetType());
  }

  [Fact]
  public void IsStarted_TracksLifecycle() {
    // Arrange
    var stateChart = new SimpleStateChart();

    // Assert initial
    stateChart.IsStarted.ShouldBeFalse();

    // Act & Assert - Start
    stateChart.Start();
    stateChart.IsStarted.ShouldBeTrue();

    // Act & Assert - Stop
    stateChart.Stop();
    stateChart.IsStarted.ShouldBeFalse();
  }

  [Fact]
  public void IsProcessing_TrueWhileProcessing() {
    // Arrange
    var stateChart = new SimpleStateChart();
    bool processingDuringInput = false;

    stateChart.Bind()
      .Watch<SimpleStateChart.Input.GoToB>((in SimpleStateChart.Input.GoToB input) => {
        processingDuringInput = stateChart.IsProcessing;
      });

    stateChart.Start();

    // Act
    stateChart.Input(new SimpleStateChart.Input.GoToB());

    // Assert
    processingDuringInput.ShouldBeTrue();
    stateChart.IsProcessing.ShouldBeFalse(); // Not processing after input completes
  }

  [Fact]
  public void Value_LazyInitializesStateChart() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.IsStarted.ShouldBeFalse();

    // Act
    var value = stateChart.Value;

    // Assert
    stateChart.IsStarted.ShouldBeTrue();
    value.ShouldBeOfType<SimpleStateChart.State.StateA>();
  }

  [Fact]
  public void ForceReset_ChangesToSpecificState() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();
    var newState = new SimpleStateChart.State.StateB();

    // Act
    var result = stateChart.ForceReset(newState);

    // Assert
    result.ShouldBeSameAs(newState);
    stateChart.Value.ShouldBeSameAs(newState);
  }

  [Fact]
  public void ForceReset_ThrowsWhenProcessing() {
    // Arrange
    var stateChart = new SimpleStateChart();
    bool exceptionThrown = false;

    stateChart.Bind()
      .Watch<SimpleStateChart.Input.GoToB>((in SimpleStateChart.Input.GoToB input) => {
        try {
          stateChart.ForceReset(new SimpleStateChart.State.StateA());
        }
        catch (StateChartException) {
          exceptionThrown = true;
        }
      });

    stateChart.Start();

    // Act
    stateChart.Input(new SimpleStateChart.Input.GoToB());

    // Assert
    exceptionThrown.ShouldBeTrue();
  }

  [Fact]
  public void RestoreFrom_CopiesStateWithOnEnter() {
    // Arrange
    var source = new SimpleStateChart();
    source.Start();
    source.Input(new SimpleStateChart.Input.GoToB());

    var target = new SimpleStateChart();
    var outputs = new List<object>();
    target.Bind()
      .Handle<SimpleStateChart.Output.EnteredB>((in SimpleStateChart.Output.EnteredB output) => outputs.Add(output));

    // Act
    target.RestoreFrom(source, shouldCallOnEnter: true);

    // Assert
    target.Value.ShouldBeOfType<SimpleStateChart.State.StateB>();
    outputs.Count.ShouldBe(1); // OnEnter was called
  }

  [Fact]
  public void RestoreFrom_CopiesStateWithoutOnEnter() {
    // Arrange
    var source = new SimpleStateChart();
    source.Start();
    source.Input(new SimpleStateChart.Input.GoToB());

    var target = new SimpleStateChart();
    var outputs = new List<object>();
    target.Bind()
      .Handle<SimpleStateChart.Output.EnteredB>((in SimpleStateChart.Output.EnteredB output) => outputs.Add(output));

    // Act
    target.RestoreFrom(source, shouldCallOnEnter: false);

    // Assert
    target.Value.ShouldBeOfType<SimpleStateChart.State.StateB>();
    outputs.ShouldBeEmpty(); // OnEnter was NOT called
  }

  [Fact]
  public void Blackboard_SetGetHasOverwrite() {
    // Arrange
    var stateChart = new SimpleStateChart();

    // Act & Assert - Has (SimpleStateChart constructor already adds Data)
    stateChart.Has<SimpleStateChart.Data>().ShouldBeTrue();

    // Act & Assert - Get
    var initialData = stateChart.Get<SimpleStateChart.Data>();
    initialData.ShouldNotBeNull();
    initialData.Value.ShouldBe(0); // Default value

    // Act & Assert - Overwrite existing data
    var data = new SimpleStateChart.Data { Value = 42 };
    stateChart.Overwrite(data);
    var retrieved = stateChart.Get<SimpleStateChart.Data>();
    retrieved.ShouldBeSameAs(data);
    retrieved.Value.ShouldBe(42);

    // Act & Assert - Overwrite again
    var newData = new SimpleStateChart.Data { Value = 100 };
    stateChart.Overwrite(newData);
    var overwritten = stateChart.Get<SimpleStateChart.Data>();
    overwritten.ShouldBeSameAs(newData);
    overwritten.Value.ShouldBe(100);
  }

  [Fact]
  public void Error_HandledByHandleError() {
    // Arrange
    var stateChart = new ErrorHandlingStateChart();
    stateChart.Start();

    // Act
    stateChart.Input(new ErrorHandlingStateChart.Input.CauseError());

    // Assert
    stateChart.HandledErrors.Count.ShouldBe(1);
    stateChart.HandledErrors[0].ShouldBeOfType<InvalidOperationException>();
  }

  [Fact]
  public void Error_AnnouncedToBindings() {
    // Arrange
    var stateChart = new ErrorHandlingStateChart();
    var errors = new List<Exception>();
    stateChart.Bind()
      .Catch<InvalidOperationException>(ex => errors.Add(ex));
    stateChart.Start();

    // Act
    stateChart.Input(new ErrorHandlingStateChart.Input.CauseError());

    // Assert
    errors.Count.ShouldBe(1);
    errors[0].ShouldBeOfType<InvalidOperationException>();
  }

  [Fact]
  public void Start_MultipleCallsAreIdempotent() {
    // Arrange
    var stateChart = new SimpleStateChart();

    // Act
    stateChart.Start();
    var firstValue = stateChart.Value;
    stateChart.Start();
    var secondValue = stateChart.Value;

    // Assert
    stateChart.IsStarted.ShouldBeTrue();
    secondValue.ShouldBeSameAs(firstValue);
  }

  [Fact]
  public void Stop_MultipleCallsAreIdempotent() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();

    // Act
    stateChart.Stop();
    stateChart.Stop();

    // Assert
    stateChart.IsStarted.ShouldBeFalse();
  }

  [Fact]
  public void OnStart_OnStop_CallbacksExecuted() {
    // Arrange
    var stateChart = new LifecycleStateChart();

    // Act & Assert - Start
    stateChart.Start();
    stateChart.OnStartCalled.ShouldBeTrue();
    stateChart.OnStopCalled.ShouldBeFalse();

    // Act & Assert - Stop
    stateChart.Stop();
    stateChart.OnStartCalled.ShouldBeTrue();
    stateChart.OnStopCalled.ShouldBeTrue();
  }

  [Fact]
  public void Context_AccessibleFromStates() {
    // Arrange
    var stateChart = new SimpleStateChart();
    stateChart.Start();

    // Act - Access context through StateChart
    var context = stateChart.Context;

    // Assert
    context.ShouldNotBeNull();
    context.Get<SimpleStateChart.Data>().ShouldBeSameAs(stateChart.Get<SimpleStateChart.Data>());
  }
}
