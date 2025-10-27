namespace CodeJunkie.StateChart.Tests;

using System;
using System.Collections.Generic;
using CodeJunkie.Metadata;
using Shouldly;
using Xunit;

/// <summary>
/// Performance tests for StateChart focusing on GC allocation behavior.
/// These tests verify that Bind() followed by Input/Output processing does not cause allocation spikes.
/// Uses GC.GetAllocatedBytesForCurrentThread() for precise allocation measurements.
/// </summary>
/// <remarks>
/// These tests are isolated in a separate xUnit collection to prevent GC and monitoring state
/// from interfering with other tests (especially StateChartRegistryTests).
/// </remarks>
[Collection("PerformanceTests")]
public sealed partial class StateChartPerformanceTests : IDisposable {
  // StateChart for performance testing
  [Meta, StateChart(typeof(State)), NoMonitoring]
  public partial class PerformanceTestStateChart : StateChart<PerformanceTestStateChart.State> {
    public static class Input {
      public readonly record struct InputA;
      public readonly record struct InputB;
      public readonly record struct InputC;
    }

    public static class Output {
      public readonly record struct OutputA;
      public readonly record struct OutputB;
      public readonly record struct OutputC;
    }

    public abstract record State : StateLogic<State> {
      public sealed record StateA : State, IGet<Input.InputA> {
        public StateA() {
          this.OnEnter(() => Output(new Output.OutputA()));
        }

        public Transition On(in Input.InputA input) => To<StateB>();
      }

      public sealed record StateB : State, IGet<Input.InputB> {
        public StateB() {
          this.OnEnter(() => {
            Output(new Output.OutputB());
          });
        }

        public Transition On(in Input.InputB input) => To<StateC>();
      }

      public sealed record StateC : State, IGet<Input.InputC> {
        public StateC() {
          this.OnEnter(() => Output(new Output.OutputC()));
        }

        public Transition On(in Input.InputC input) => To<StateA>();
      }
    }

    public override Transition GetInitialState() => To<State.StateA>();
  }

  public StateChartPerformanceTests() {
    StateChartRegistry.Reset();

    // Disable monitoring for performance tests to:
    // 1. Avoid WeakReference overhead in measurements
    // 2. Prevent GC interactions from affecting other tests (especially ConcurrentAccess_IsThreadSafe)
    // 3. Focus purely on allocation measurement without registry tracking
    StateChartRegistry.IsMonitoringEnabled = false;
  }

  public void Dispose() {
    // Re-enable monitoring for subsequent tests
    StateChartRegistry.IsMonitoringEnabled = true;
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  #region Helper Methods

  /// <summary>
  /// Warms up the JIT compiler and any caching mechanisms by running the state chart multiple times.
  /// This ensures that allocation measurements are not affected by one-time initialization costs.
  /// </summary>
  private void WarmUp(PerformanceTestStateChart stateChart, int iterations = 100) {
    for (int i = 0; i < iterations; i++) {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
      stateChart.Input(new PerformanceTestStateChart.Input.InputB());
      stateChart.Input(new PerformanceTestStateChart.Input.InputC());
    }
  }

  /// <summary>
  /// Forces garbage collection to establish a clean baseline for allocation measurements.
  /// </summary>
  private void ForceGarbageCollection() {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
  }

  /// <summary>
  /// Measures the memory allocated during an action execution.
  /// </summary>
  /// <param name="action">The action to measure</param>
  /// <returns>The number of bytes allocated</returns>
  private long MeasureAllocation(Action action) {
    ForceGarbageCollection();

    long before = GC.GetAllocatedBytesForCurrentThread();
    action();
    long after = GC.GetAllocatedBytesForCurrentThread();

    return after - before;
  }

  #endregion

  #region Input Processing Allocation Tests

  [Fact]
  public void InputProcessing_AfterWarmup_AllocatesMinimalMemory() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // Create binding
    stateChart.Bind();

    // Warmup to ensure JIT compilation is complete
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation for a single input processing
    long allocated = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    });

    // Assert - Should allocate minimal memory (allowing some tolerance for internal bookkeeping)
    // In an ideal zero-allocation scenario, this should be 0, but we allow up to 64 bytes
    // for potential internal optimizations or runtime overhead
    allocated.ShouldBeLessThanOrEqualTo(64);
  }

  [Fact]
  public void InputProcessing_WithoutBinding_AllocatesMinimalMemory() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // No binding created - testing baseline input processing

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation for a single input processing
    long allocated = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    });

    // Assert - Without binding overhead, should be even more minimal
    allocated.ShouldBeLessThanOrEqualTo(64);
  }

  [Fact]
  public void InputProcessing_MultipleInputs_MaintainsStableAllocation() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();
    stateChart.Bind();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation for multiple consecutive inputs
    var allocations = new List<long>();
    for (int i = 0; i < 10; i++) {
      long allocated = MeasureAllocation(() => {
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
        stateChart.Input(new PerformanceTestStateChart.Input.InputB());
        stateChart.Input(new PerformanceTestStateChart.Input.InputC());
      });
      allocations.Add(allocated);
    }

    // Assert - All allocations should be consistently low
    foreach (var allocation in allocations) {
      allocation.ShouldBeLessThanOrEqualTo(200); // 3 inputs, allowing some overhead
    }

    // Verify stability - no growing allocation pattern
    var avgFirst5 = allocations.Take(5).Average();
    var avgLast5 = allocations.Skip(5).Average();

    // Last 5 iterations should not allocate significantly more than first 5
    avgLast5.ShouldBeLessThanOrEqualTo(avgFirst5 * 1.5); // Allow 50% variance
  }

  #endregion

  #region Output Processing Allocation Tests

  [Fact]
  public void OutputProcessing_AfterWarmup_AllocatesMinimalMemory() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var outputCount = 0;

    stateChart.Bind()
      .Handle<PerformanceTestStateChart.Output.OutputA>((in PerformanceTestStateChart.Output.OutputA output) => {
        outputCount++;
      });

    stateChart.Start(); // This triggers OutputA via StateA.OnEnter

    // Warmup - cycle through states to trigger all outputs
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation when triggering an output
    long allocated = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA()); // Transitions to StateB, emits OutputB
    });

    // Assert - struct outputs should not cause boxing allocations
    allocated.ShouldBeLessThanOrEqualTo(64);
  }

  [Fact]
  public void OutputProcessing_MultipleOutputs_MaintainsStableAllocation() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var outputs = new List<PerformanceTestStateChart.Output.OutputA>();

    stateChart.Bind()
      .Handle<PerformanceTestStateChart.Output.OutputA>((in PerformanceTestStateChart.Output.OutputA output) => {
        outputs.Add(output);
      });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation for multiple cycles that produce outputs
    var allocations = new List<long>();
    for (int i = 0; i < 10; i++) {
      long allocated = MeasureAllocation(() => {
        // Full cycle: StateA -> StateB -> StateC -> StateA
        // Each transition triggers an OnEnter with Output
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
        stateChart.Input(new PerformanceTestStateChart.Input.InputB());
        stateChart.Input(new PerformanceTestStateChart.Input.InputC());
      });
      allocations.Add(allocated);
    }

    // Assert - All allocations should be consistently low
    foreach (var allocation in allocations) {
      allocation.ShouldBeLessThanOrEqualTo(200);
    }
  }

  #endregion

  #region Binding Callback Allocation Tests

  [Fact]
  public void BindingCallbacks_AfterWarmup_AllocatesMinimalMemory() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var inputCount = 0;
    var outputCount = 0;

    stateChart.Bind()
      .Watch<PerformanceTestStateChart.Input.InputA>((in PerformanceTestStateChart.Input.InputA input) => {
        inputCount++;
      })
      .Handle<PerformanceTestStateChart.Output.OutputB>((in PerformanceTestStateChart.Output.OutputB output) => {
        outputCount++;
      });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation when callbacks are invoked
    long allocated = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    });

    // Assert - Callback invocation should not allocate significantly
    allocated.ShouldBeLessThanOrEqualTo(64);

    // Verify callbacks were executed
    inputCount.ShouldBeGreaterThan(0);
    outputCount.ShouldBeGreaterThan(0);
  }

  [Fact]
  public void BindingCallbacks_MultipleHandlers_AllocatesMinimalMemory() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var handler1Count = 0;
    var handler2Count = 0;
    var handler3Count = 0;

    stateChart.Bind()
      .Watch<PerformanceTestStateChart.Input.InputA>((in PerformanceTestStateChart.Input.InputA input) => {
        handler1Count++;
      })
      .Watch<PerformanceTestStateChart.Input.InputA>((in PerformanceTestStateChart.Input.InputA input) => {
        handler2Count++;
      })
      .Watch<PerformanceTestStateChart.Input.InputA>((in PerformanceTestStateChart.Input.InputA input) => {
        handler3Count++;
      });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation when multiple callbacks are invoked
    long allocated = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    });

    // Assert - Even with multiple handlers, allocation should be minimal
    allocated.ShouldBeLessThanOrEqualTo(128); // Slightly higher tolerance for multiple handlers

    // Verify all handlers were executed
    handler1Count.ShouldBeGreaterThan(0);
    handler2Count.ShouldBeGreaterThan(0);
    handler3Count.ShouldBeGreaterThan(0);
  }

  #endregion

  #region Struct Input/Output Boxing Avoidance Tests

  [Fact]
  public void StructInputsAndOutputs_AvoidBoxing() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var capturedOutputs = new List<PerformanceTestStateChart.Output.OutputA>();

    stateChart.Bind()
      .Handle<PerformanceTestStateChart.Output.OutputA>((in PerformanceTestStateChart.Output.OutputA output) => {
        // This lambda should receive the output by ref, avoiding boxing
        capturedOutputs.Add(output);
      });

    stateChart.Start(); // StateA emits OutputA

    // Warmup - cycle back to StateA to trigger OutputA
    for (int i = 0; i < 100; i++) {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA()); // StateA -> StateB (emits OutputB)
      stateChart.Input(new PerformanceTestStateChart.Input.InputB()); // StateB -> StateC (emits OutputC)
      stateChart.Input(new PerformanceTestStateChart.Input.InputC()); // StateC -> StateA (emits OutputA)
    }

    capturedOutputs.Clear();

    // Act - Measure allocation
    long allocated = MeasureAllocation(() => {
      for (int i = 0; i < 100; i++) {
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
        stateChart.Input(new PerformanceTestStateChart.Input.InputB());
        stateChart.Input(new PerformanceTestStateChart.Input.InputC()); // This emits OutputA
      }
    });

    // Assert - 300 struct inputs and 300 struct outputs should not cause significant boxing allocation
    // If boxing occurred, we'd see ~9600 bytes (300 * ~32 bytes per box)
    // With proper struct handling, allocation should be much lower
    allocated.ShouldBeLessThanOrEqualTo(2000); // Well below what boxing would cause

    // Verify outputs were captured
    capturedOutputs.Count.ShouldBe(100);
  }

  #endregion

  #region Multiple Cycles Stability Tests

  [Fact]
  public void MultipleInputOutputCycles_MaintainsStableAllocation() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var outputACount = 0;
    var outputBCount = 0;
    var outputCCount = 0;

    stateChart.Bind()
      .Handle<PerformanceTestStateChart.Output.OutputA>((in PerformanceTestStateChart.Output.OutputA output) => {
        outputACount++;
      })
      .Handle<PerformanceTestStateChart.Output.OutputB>((in PerformanceTestStateChart.Output.OutputB output) => {
        outputBCount++;
      })
      .Handle<PerformanceTestStateChart.Output.OutputC>((in PerformanceTestStateChart.Output.OutputC output) => {
        outputCCount++;
      });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Act - Measure allocation across many cycles
    var allocations = new List<long>();
    for (int cycle = 0; cycle < 20; cycle++) {
      long allocated = MeasureAllocation(() => {
        for (int i = 0; i < 10; i++) {
          stateChart.Input(new PerformanceTestStateChart.Input.InputA());
          stateChart.Input(new PerformanceTestStateChart.Input.InputB());
          stateChart.Input(new PerformanceTestStateChart.Input.InputC());
        }
      });
      allocations.Add(allocated);
    }

    // Assert - Allocation should remain stable across all cycles
    var avgAllocation = allocations.Average();

    foreach (var allocation in allocations) {
      // Each allocation should be within 50% of average (indicating stability)
      allocation.ShouldBeLessThanOrEqualTo((long)(avgAllocation * 1.5));
      allocation.ShouldBeGreaterThanOrEqualTo((long)(avgAllocation * 0.5));
    }

    // Overall allocation per cycle should be low
    avgAllocation.ShouldBeLessThanOrEqualTo(600); // 30 operations per cycle

    // Verify callbacks were executed extensively
    outputACount.ShouldBeGreaterThan(100);
    outputBCount.ShouldBeGreaterThan(100);
    outputCCount.ShouldBeGreaterThan(100);
  }

  [Fact]
  public void LongRunningStateChart_DoesNotAccumulateAllocations() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Bind()
      .Handle<PerformanceTestStateChart.Output.OutputA>((in PerformanceTestStateChart.Output.OutputA output) => { })
      .Handle<PerformanceTestStateChart.Output.OutputB>((in PerformanceTestStateChart.Output.OutputB output) => { })
      .Handle<PerformanceTestStateChart.Output.OutputC>((in PerformanceTestStateChart.Output.OutputC output) => { });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 100);

    // Measure allocation at different points in a long run
    var allocationAt100 = MeasureAllocation(() => {
      for (int i = 0; i < 100; i++) {
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
      }
    });

    // Run another 1000 iterations without measuring
    for (int i = 0; i < 1000; i++) {
      stateChart.Input(new PerformanceTestStateChart.Input.InputB());
    }

    var allocationAt1100 = MeasureAllocation(() => {
      for (int i = 0; i < 100; i++) {
        stateChart.Input(new PerformanceTestStateChart.Input.InputC());
      }
    });

    // Assert - Allocation should not grow over time
    // Later iterations should not allocate more than earlier ones
    allocationAt1100.ShouldBeLessThanOrEqualTo((long)(allocationAt100 * 1.2)); // Allow 20% variance
  }

  #endregion

  #region Baseline Comparison Tests

  [Fact]
  public void Warmup_ReducesSubsequentAllocations() {
    // This test demonstrates the importance of warmup by comparing
    // allocations before and after warmup

    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Bind();
    stateChart.Start();

    // Act - Measure allocation on first few runs (before full warmup)
    ForceGarbageCollection();
    long before = GC.GetAllocatedBytesForCurrentThread();
    stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    stateChart.Input(new PerformanceTestStateChart.Input.InputB());
    stateChart.Input(new PerformanceTestStateChart.Input.InputC());
    long after = GC.GetAllocatedBytesForCurrentThread();
    long firstFewRunsAllocation = after - before;

    // Warmup extensively
    WarmUp(stateChart, iterations: 200);

    // Measure allocation after extensive warmup
    long afterWarmup = MeasureAllocation(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
      stateChart.Input(new PerformanceTestStateChart.Input.InputB());
      stateChart.Input(new PerformanceTestStateChart.Input.InputC());
    });

    // Assert - After warmup, allocation should be minimal or equal to early runs
    // (In a well-optimized scenario, both might be 0, which is excellent!)
    afterWarmup.ShouldBeLessThanOrEqualTo(Math.Max(firstFewRunsAllocation, 200L));

    // After warmup should be minimal
    afterWarmup.ShouldBeLessThanOrEqualTo(200);
  }

  #endregion

  #region Zero-Allocation Tests (Strict 0-byte Verification)

  /// <summary>
  /// Measures allocation and provides detailed diagnostic information if non-zero allocation is detected.
  /// </summary>
  private (long allocated, string diagnosticInfo) MeasureAllocationWithDiagnostics(Action action, string scenarioName) {
    ForceGarbageCollection();

    long before = GC.GetAllocatedBytesForCurrentThread();
    action();
    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;

    string diagnosticInfo = $"Scenario: {scenarioName}\n" +
                           $"  Allocated: {allocated} bytes\n" +
                           $"  Before: {before} bytes\n" +
                           $"  After: {after} bytes";

    return (allocated, diagnosticInfo);
  }

  [Fact]
  public void InputProcessing_ZeroAllocation_SingleInput() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // No binding - testing baseline input processing for absolute zero allocation

    // Extensive warmup to ensure JIT compilation and all optimizations are complete
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation for a single input processing
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    }, "Single Input Without Binding");

    // Assert - Strict zero-allocation requirement
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void InputProcessing_ZeroAllocation_MultipleInputs() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // No binding - pure input processing

    // Warmup
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation for multiple consecutive inputs
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      // Process 10 inputs in a row
      for (int i = 0; i < 10; i++) {
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
        stateChart.Input(new PerformanceTestStateChart.Input.InputB());
        stateChart.Input(new PerformanceTestStateChart.Input.InputC());
      }
    }, "30 Inputs Without Binding");

    // Assert - Strict zero-allocation requirement
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void OutputProcessing_ZeroAllocation_SingleOutput() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();

    // No binding - testing baseline output emission
    stateChart.Start(); // Emits OutputA via StateA.OnEnter

    // Warmup - cycle through states to trigger outputs
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation when an output is emitted
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA()); // StateA -> StateB, emits OutputB
    }, "Single Output Without Binding");

    // Assert - Strict zero-allocation requirement
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void OutputProcessing_ZeroAllocation_WithBindingButNoHandlers() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();

    // Create binding but don't register any handlers
    stateChart.Bind();

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation with binding present but no handlers
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    }, "Output With Empty Binding");

    // Assert - Even with empty binding, should be zero allocation
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void InputProcessing_ZeroAllocation_WithNonMatchingBinding() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    var dummyCount = 0;

    // Register a handler for a different input type
    stateChart.Bind()
      .Watch<PerformanceTestStateChart.Input.InputB>((in PerformanceTestStateChart.Input.InputB input) => {
        dummyCount++;
      });

    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 500);

    // Act - Process InputA (which has no registered handler)
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    }, "Input With Non-Matching Binding");

    // Assert - Should be zero allocation when handler doesn't match
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void FullCycle_ZeroAllocation_InputAndOutput() {
    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // No binding - pure state machine operation

    // Warmup
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation for a full state transition cycle
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      // Complete cycle: StateA -> StateB -> StateC -> StateA
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
      stateChart.Input(new PerformanceTestStateChart.Input.InputB());
      stateChart.Input(new PerformanceTestStateChart.Input.InputC());
    }, "Full State Cycle Without Binding");

    // Assert - Full cycle should be zero allocation
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void StructPassing_ZeroAllocation_ByRef() {
    // This test specifically verifies that struct inputs/outputs passed by ref
    // do not cause any boxing or allocation

    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // Warmup
    for (int i = 0; i < 500; i++) {
      stateChart.Input(new PerformanceTestStateChart.Input.InputA());
    }

    // Act - Pass struct inputs repeatedly
    var (allocated, diagnosticInfo) = MeasureAllocationWithDiagnostics(() => {
      for (int i = 0; i < 100; i++) {
        // Each input is a struct passed by ref (via 'in' parameter)
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
      }
    }, "100 Struct Inputs By Ref");

    // Assert - Struct passing by ref should never allocate
    allocated.ShouldBe(0L, $"Expected zero allocation but got {allocated} bytes.\n{diagnosticInfo}");
  }

  [Fact]
  public void ConsecutiveOperations_ZeroAllocation_Stability() {
    // Verify that zero allocation is consistent across multiple measurement cycles

    // Arrange
    var stateChart = new PerformanceTestStateChart();
    stateChart.Start();

    // Warmup
    WarmUp(stateChart, iterations: 500);

    // Act - Measure allocation across 10 consecutive cycles
    var allocationResults = new List<long>();
    for (int cycle = 0; cycle < 10; cycle++) {
      var (allocated, _) = MeasureAllocationWithDiagnostics(() => {
        stateChart.Input(new PerformanceTestStateChart.Input.InputA());
        stateChart.Input(new PerformanceTestStateChart.Input.InputB());
        stateChart.Input(new PerformanceTestStateChart.Input.InputC());
      }, $"Cycle {cycle}");

      allocationResults.Add(allocated);
    }

    // Assert - Every cycle should be zero allocation
    foreach (var (allocation, index) in allocationResults.Select((a, i) => (a, i))) {
      allocation.ShouldBe(0L, $"Cycle {index}: Expected zero allocation but got {allocation} bytes");
    }

    // All measurements should be consistently zero
    allocationResults.All(a => a == 0).ShouldBeTrue("All cycles should have zero allocation");
  }

  #endregion
}
