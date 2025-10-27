namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Integration tests for complex StateChart scenarios and end-to-end workflows.
/// Based on LogicBlocks integration testing patterns.
/// </summary>
public sealed partial class StateChartIntegrationTests : IDisposable {
  // Complex hierarchical state chart for integration testing
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class HierarchicalStateChart : StateChart<HierarchicalStateChart.State> {
    public static class Input {
      public readonly record struct Start;
      public readonly record struct Pause;
      public readonly record struct Resume;
      public readonly record struct Stop;
      public readonly record struct Increment;
    }

    public static class Output {
      public readonly record struct Started;
      public readonly record struct Paused;
      public readonly record struct Resumed;
      public readonly record struct Stopped;
      public readonly record struct ValueIncremented(int Value);
    }

    public class Data {
      public int Value { get; set; }
    }

    public abstract record State : StateLogic<State> {
      public sealed record Inactive : State, IGet<Input.Start> {
        public Inactive() {
          this.OnEnter(() => Output(new Output.Stopped()));
        }

        public Transition On(in Input.Start input) => To<Active.Running>();
      }

      public abstract record Active : State, IGet<Input.Stop> {
        public Transition On(in Input.Stop input) => To<Inactive>();

        public sealed record Running : Active, IGet<Input.Pause>, IGet<Input.Increment> {
          public Running() {
            this.OnEnter(() => Output(new Output.Started()));
          }

          public Transition On(in Input.Pause input) => To<Paused>();

          public Transition On(in Input.Increment input) {
            var data = Get<Data>();
            data.Value++;
            Output(new Output.ValueIncremented(data.Value));
            return ToSelf();
          }
        }

        public sealed record Paused : Active, IGet<Input.Resume> {
          public Paused() {
            this.OnEnter(() => Output(new Output.Paused()));
          }

          public Transition On(in Input.Resume input) {
            Output(new Output.Resumed());
            return To<Running>();
          }
        }
      }
    }

    public override Transition GetInitialState() => To<State.Inactive>();

    public HierarchicalStateChart() {
      Set(new Data());
    }
  }

  // StateChart for error recovery testing
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class ErrorRecoveryStateChart : StateChart<ErrorRecoveryStateChart.State> {
    public static class Input {
      public readonly record struct DoWork;
      public readonly record struct Recover;
    }

    public static class Output {
      public readonly record struct WorkCompleted;
      public readonly record struct ErrorOccurred;
      public readonly record struct Recovered;
    }

    public abstract record State : StateLogic<State> {
      public sealed record Working : State, IGet<Input.DoWork> {
        public Transition On(in Input.DoWork input) {
          Output(new Output.ErrorOccurred());
          return To<ErrorState>();
        }
      }

      public sealed record ErrorState : State, IGet<Input.Recover> {
        public Transition On(in Input.Recover input) {
          Output(new Output.Recovered());
          return To<Working>();
        }
      }
    }

    public override Transition GetInitialState() => To<State.Working>();
  }

  // StateChart for real-world task tracking scenario
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class TaskTrackerStateChart : StateChart<TaskTrackerStateChart.State> {
    public static class Input {
      public readonly record struct CreateTask;
      public readonly record struct StartTask;
      public readonly record struct CompleteTask;
      public readonly record struct CancelTask;
    }

    public class TaskData {
      public string TaskName { get; set; } = "";
      public int CompletedTasks { get; set; }
    }

    public abstract record State : StateLogic<State> {
      public sealed record Idle : State, IGet<Input.CreateTask> {
        public Transition On(in Input.CreateTask input) => To<TaskCreated>();
      }

      public sealed record TaskCreated : State, IGet<Input.StartTask>, IGet<Input.CancelTask> {
        public Transition On(in Input.StartTask input) => To<InProgress>();
        public Transition On(in Input.CancelTask input) => To<Idle>();
      }

      public sealed record InProgress : State, IGet<Input.CompleteTask>, IGet<Input.CancelTask> {
        public Transition On(in Input.CompleteTask input) {
          var data = Get<TaskData>();
          data.CompletedTasks++;
          return To<Idle>();
        }

        public Transition On(in Input.CancelTask input) => To<Idle>();
      }
    }

    public override Transition GetInitialState() => To<State.Idle>();

    public TaskTrackerStateChart() {
      Set(new TaskData());
    }
  }

  public StateChartIntegrationTests() {
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void HierarchicalStates_TransitionCorrectly() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    stateChart.Start();

    // Act & Assert - Start
    stateChart.Value.ShouldBeOfType<HierarchicalStateChart.State.Inactive>();

    stateChart.Input(new HierarchicalStateChart.Input.Start());
    stateChart.Value.ShouldBeOfType<HierarchicalStateChart.State.Active.Running>();

    // Act & Assert - Pause
    stateChart.Input(new HierarchicalStateChart.Input.Pause());
    stateChart.Value.ShouldBeOfType<HierarchicalStateChart.State.Active.Paused>();

    // Act & Assert - Resume
    stateChart.Input(new HierarchicalStateChart.Input.Resume());
    stateChart.Value.ShouldBeOfType<HierarchicalStateChart.State.Active.Running>();

    // Act & Assert - Stop
    stateChart.Input(new HierarchicalStateChart.Input.Stop());
    stateChart.Value.ShouldBeOfType<HierarchicalStateChart.State.Inactive>();
  }

  [Fact]
  public void ComplexInputOutputSequence_WorksCorrectly() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    var outputs = new List<object>();

    stateChart.Bind()
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => outputs.Add(o))
      .Handle<HierarchicalStateChart.Output.Paused>((in HierarchicalStateChart.Output.Paused o) => outputs.Add(o))
      .Handle<HierarchicalStateChart.Output.Resumed>((in HierarchicalStateChart.Output.Resumed o) => outputs.Add(o))
      .Handle<HierarchicalStateChart.Output.Stopped>((in HierarchicalStateChart.Output.Stopped o) => outputs.Add(o))
      .Handle<HierarchicalStateChart.Output.ValueIncremented>((in HierarchicalStateChart.Output.ValueIncremented o) => outputs.Add(o));

    // Act
    stateChart.Start();
    stateChart.Input(new HierarchicalStateChart.Input.Start());
    stateChart.Input(new HierarchicalStateChart.Input.Increment());
    stateChart.Input(new HierarchicalStateChart.Input.Increment());
    stateChart.Input(new HierarchicalStateChart.Input.Pause());
    stateChart.Input(new HierarchicalStateChart.Input.Resume());
    stateChart.Input(new HierarchicalStateChart.Input.Stop());

    // Assert - 8 outputs total (Start() triggers initial Inactive.OnEnter)
    outputs.Count.ShouldBe(8);
    outputs[0].ShouldBeOfType<HierarchicalStateChart.Output.Stopped>(); // Inactive OnEnter (from Start())
    outputs[1].ShouldBeOfType<HierarchicalStateChart.Output.Started>(); // Running OnEnter (from Input.Start)
    outputs[2].ShouldBeOfType<HierarchicalStateChart.Output.ValueIncremented>();
    outputs[3].ShouldBeOfType<HierarchicalStateChart.Output.ValueIncremented>();
    outputs[4].ShouldBeOfType<HierarchicalStateChart.Output.Paused>(); // Paused OnEnter
    outputs[5].ShouldBeOfType<HierarchicalStateChart.Output.Resumed>(); // Resume action
    outputs[6].ShouldBeOfType<HierarchicalStateChart.Output.Started>(); // Running OnEnter again
    outputs[7].ShouldBeOfType<HierarchicalStateChart.Output.Stopped>(); // Inactive OnEnter (from Input.Stop)
  }

  [Fact]
  public void BlackboardDataFlow_BetweenStates_WorksCorrectly() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    stateChart.Start();

    // Act
    stateChart.Input(new HierarchicalStateChart.Input.Start());
    stateChart.Input(new HierarchicalStateChart.Input.Increment());
    stateChart.Input(new HierarchicalStateChart.Input.Increment());
    stateChart.Input(new HierarchicalStateChart.Input.Increment());

    // Assert
    var data = stateChart.Get<HierarchicalStateChart.Data>();
    data.Value.ShouldBe(3);
  }

  [Fact]
  public void MultipleBindings_OnSameStateChart_AllReceiveNotifications() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    var binding1Outputs = new List<object>();
    var binding2Outputs = new List<object>();

    var binding1 = stateChart.Bind()
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => binding1Outputs.Add(o));

    var binding2 = stateChart.Bind()
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => binding2Outputs.Add(o));

    // Act
    stateChart.Start();
    stateChart.Input(new HierarchicalStateChart.Input.Start());

    // Assert
    binding1Outputs.Count.ShouldBe(1);
    binding2Outputs.Count.ShouldBe(1);
  }

  [Fact]
  public void ErrorRecovery_Workflow_WorksCorrectly() {
    // Arrange
    var stateChart = new ErrorRecoveryStateChart();
    var outputs = new List<object>();

    stateChart.Bind()
      .Handle<ErrorRecoveryStateChart.Output.ErrorOccurred>((in ErrorRecoveryStateChart.Output.ErrorOccurred o) => outputs.Add(o))
      .Handle<ErrorRecoveryStateChart.Output.Recovered>((in ErrorRecoveryStateChart.Output.Recovered o) => outputs.Add(o));

    stateChart.Start();

    // Act
    stateChart.Input(new ErrorRecoveryStateChart.Input.DoWork());
    stateChart.Input(new ErrorRecoveryStateChart.Input.Recover());

    // Assert
    outputs.Count.ShouldBe(2);
    outputs[0].ShouldBeOfType<ErrorRecoveryStateChart.Output.ErrorOccurred>();
    outputs[1].ShouldBeOfType<ErrorRecoveryStateChart.Output.Recovered>();
    stateChart.Value.ShouldBeOfType<ErrorRecoveryStateChart.State.Working>();
  }

  [Fact]
  public void LongStateTransitionChain_ExecutesCorrectly() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    var stateChanges = new List<Type>();

    stateChart.Bind()
      .When<HierarchicalStateChart.State>((state) => stateChanges.Add(state.GetType()));

    // Act - Chain of state transitions
    stateChart.Start();
    stateChart.Input(new HierarchicalStateChart.Input.Start());
    stateChart.Input(new HierarchicalStateChart.Input.Pause());
    stateChart.Input(new HierarchicalStateChart.Input.Resume());
    stateChart.Input(new HierarchicalStateChart.Input.Stop());
    stateChart.Input(new HierarchicalStateChart.Input.Start());

    // Assert
    stateChanges.Count.ShouldBe(6);
    stateChanges[0].ShouldBe(typeof(HierarchicalStateChart.State.Inactive));
    stateChanges[1].ShouldBe(typeof(HierarchicalStateChart.State.Active.Running));
    stateChanges[2].ShouldBe(typeof(HierarchicalStateChart.State.Active.Paused));
    stateChanges[3].ShouldBe(typeof(HierarchicalStateChart.State.Active.Running));
    stateChanges[4].ShouldBe(typeof(HierarchicalStateChart.State.Inactive));
    stateChanges[5].ShouldBe(typeof(HierarchicalStateChart.State.Active.Running));
  }

  [Fact]
  public void ConcurrentBindingUpdates_AllExecute() {
    // Arrange
    var stateChart = new HierarchicalStateChart();
    var handler1Count = 0;
    var handler2Count = 0;
    var handler3Count = 0;

    stateChart.Bind()
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => handler1Count++)
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => handler2Count++)
      .Handle<HierarchicalStateChart.Output.Started>((in HierarchicalStateChart.Output.Started o) => handler3Count++);

    // Act
    stateChart.Start();
    stateChart.Input(new HierarchicalStateChart.Input.Start());

    // Assert
    handler1Count.ShouldBe(1);
    handler2Count.ShouldBe(1);
    handler3Count.ShouldBe(1);
  }

  [Fact]
  public void RealWorld_MiniStateMachine_Scenario() {
    // A mini real-world scenario: A simple task tracker
    // Arrange
    var tracker = new TaskTrackerStateChart();
    tracker.Start();

    // Act - Complete workflow
    tracker.Input(new TaskTrackerStateChart.Input.CreateTask());
    tracker.Input(new TaskTrackerStateChart.Input.StartTask());
    tracker.Input(new TaskTrackerStateChart.Input.CompleteTask());

    // Assert
    tracker.Value.ShouldBeOfType<TaskTrackerStateChart.State.Idle>();
    var data = tracker.Get<TaskTrackerStateChart.TaskData>();
    data.CompletedTasks.ShouldBe(1);
  }
}
