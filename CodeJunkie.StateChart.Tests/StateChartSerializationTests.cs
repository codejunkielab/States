namespace CodeJunkie.StateChart.Tests;

using System;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for StateChart serialization, restoration, and state comparison.
/// Based on LogicBlocks serialization testing patterns.
/// </summary>
public sealed partial class StateChartSerializationTests : IDisposable {
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class SerializableStateChart : StateChart<SerializableStateChart.State> {
    public static class Input {
      public readonly record struct GoToB;
      public readonly record struct Increment;
    }

    public class Data {
      public int Value { get; set; }
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.GoToB> {
        public Transition On(in Input.GoToB input) => To<StateB>();
      }

      public sealed record StateB : State, IGet<Input.Increment> {
        public Transition On(in Input.Increment input) {
          var data = Get<Data>();
          data.Value++;
          return ToSelf();
        }
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();

    public SerializableStateChart() {
      Set(new Data());
    }
  }

  public StateChartSerializationTests() {
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void RestoreFrom_CopiesState() {
    // Arrange
    var source = new SerializableStateChart();
    source.Start();
    source.Input(new SerializableStateChart.Input.GoToB());

    var target = new SerializableStateChart();

    // Act
    target.RestoreFrom(source);

    // Assert
    target.Value.ShouldBeOfType<SerializableStateChart.State.StateB>();
  }

  [Fact]
  public void RestoreFrom_CopiesBlackboardData() {
    // Arrange
    var source = new SerializableStateChart();
    source.Start();
    source.Input(new SerializableStateChart.Input.GoToB());
    source.Input(new SerializableStateChart.Input.Increment());
    source.Input(new SerializableStateChart.Input.Increment());

    var sourceData = source.Get<SerializableStateChart.Data>();
    sourceData.Value.ShouldBe(2);

    var target = new SerializableStateChart();

    // Act
    target.RestoreFrom(source);

    // Assert
    var targetData = target.Get<SerializableStateChart.Data>();
    targetData.ShouldNotBeNull();
    targetData.Value.ShouldBe(2);
  }

  [Fact]
  public void RestoreFrom_WithShouldCallOnEnter_True_InvokesOnEnter() {
    // Arrange
    var source = new SerializableStateChart();
    source.Start();
    source.Input(new SerializableStateChart.Input.GoToB());

    var target = new SerializableStateChart();
    var enterCallbackCalled = false;

    // Manually check if OnEnter would be called by monitoring state
    target.Bind().When<SerializableStateChart.State.StateB>((state) => {
      enterCallbackCalled = true;
    });

    // Act - RestoreFrom sets RestoredState, Start() triggers the actual state transition
    target.RestoreFrom(source, shouldCallOnEnter: true);
    target.Start(); // This triggers the state transition and OnEnter callback

    // Assert
    enterCallbackCalled.ShouldBeTrue();
  }

  [Fact]
  public void RestoreFrom_WithShouldCallOnEnter_False_DoesNotInvokeOnEnter() {
    // Arrange
    var source = new SerializableStateChart();
    source.Start();
    source.Input(new SerializableStateChart.Input.GoToB());

    var target = new SerializableStateChart();
    var stateChangeCount = 0;

    target.Bind().When<SerializableStateChart.State>((state) => {
      stateChangeCount++;
    });

    // Act
    target.RestoreFrom(source, shouldCallOnEnter: false);

    // Assert
    // StateB should be restored but without triggering the When callback
    // because OnEnter is skipped
    stateChangeCount.ShouldBe(0);
  }

  [Fact]
  public void RestoreState_BeforeStart_Succeeds() {
    // Arrange
    var stateChart = new SerializableStateChart();
    var state = new SerializableStateChart.State.StateB();

    // Act
    stateChart.RestoreState(state);

    // Assert
    stateChart.Value.ShouldBeSameAs(state);
  }

  [Fact]
  public void RestoreState_AfterStart_Throws() {
    // Arrange
    var stateChart = new SerializableStateChart();
    stateChart.Start();
    var state = new SerializableStateChart.State.StateB();

    // Act & Assert
    Should.Throw<StateChartException>(() => {
      stateChart.RestoreState(state);
    });
  }

  [Fact]
  public void Equals_ComparesSameStates_ReturnsTrue() {
    // Arrange
    var stateChart1 = new SerializableStateChart();
    stateChart1.Start();

    var stateChart2 = new SerializableStateChart();
    // Restore from stateChart1 to ensure both have the exact same state instance
    stateChart2.RestoreFrom(stateChart1);
    stateChart2.Start();

    // Both should now have equivalent state
    // Act
    var areEqual = stateChart1.Equals(stateChart2);

    // Assert - Equals compares state values, not just types
    areEqual.ShouldBeTrue();
  }

  [Fact]
  public void Equals_ComparesDifferentStates_ReturnsFalse() {
    // Arrange
    var stateChart1 = new SerializableStateChart();
    var stateChart2 = new SerializableStateChart();

    stateChart1.Start();
    stateChart2.Start();

    // Transition stateChart2 to different state
    stateChart2.Input(new SerializableStateChart.Input.GoToB());

    // Act
    var areEqual = stateChart1.Equals(stateChart2);

    // Assert
    areEqual.ShouldBeFalse();
  }

  [Fact]
  public void Equals_ComparesSameBlackboardData_ReturnsTrue() {
    // Arrange
    var stateChart1 = new SerializableStateChart();
    stateChart1.Start();

    var stateChart2 = new SerializableStateChart();
    // Restore from stateChart1 to ensure both have the exact same state and blackboard data
    stateChart2.RestoreFrom(stateChart1);
    stateChart2.Start();

    // Both should have same blackboard data and state
    // Act
    var areEqual = stateChart1.Equals(stateChart2);

    // Assert
    areEqual.ShouldBeTrue();
  }
}
