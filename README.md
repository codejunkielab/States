# StateChart

A powerful C# framework for modeling behavior using statecharts with built-in support for hierarchy, nesting, and event-driven transitions.

## Overview

StateChart is a type-safe, reactive state machine framework designed for complex behavior modeling in C# applications. Whether you're building game AI, workflow systems, UI state management, or any system requiring sophisticated state modeling, StateChart provides the tools you need.

### Why StateChart?

- **Type-Safe** - Leverage C#'s type system for compile-time safety
- **Hierarchical** - Model complex behaviors with nested state hierarchies
- **Reactive** - Built-in binding system for observing state changes and outputs
- **Serializable** - Full serialization support for save/load scenarios
- **AOT-Ready** - Works perfectly in AOT environments (Unity, NativeAOT)
- **Visual** - Automatic diagram generation (PlantUML, Mermaid, Markdown)
- **Developer-Friendly** - Roslyn analyzers catch errors at compile time
- **Debuggable** - Built-in monitoring and inspection tools

## Features

### Core Features
- **Hierarchical States** - Organize states in parent-child relationships
- **Event-Driven Inputs** - Process inputs through strongly-typed events
- **Output Emission** - Communicate state changes to external systems
- **Data Management** - Built-in blackboard pattern for state data
- **Lifecycle Callbacks** - OnEnter and OnExit hooks for state transitions
- **Reactive Bindings** - Subscribe to outputs and state changes

### Developer Experience
- **Diagram Generation** - Auto-generate PlantUML, Mermaid, and Markdown documentation
- **StateChart Monitoring** - Debug and inspect all active StateChart instances
- **Roslyn Analyzers** - Compile-time validation and code fixes
- **Unit Testing Support** - Fake bindings and state isolation for testing

### Performance & Compatibility
- **Zero-Allocation Options** - Pre-allocate states for performance-critical scenarios
- **Serialization** - JSON serialization with System.Text.Json
- **.NET Standard 2.1** - Compatible with .NET Core, .NET 5+, Unity, and more

## Installation

Install via NuGet:

```bash
dotnet add package CodeJunkie.StateChart
```

For diagram generation:
```bash
dotnet add package CodeJunkie.StateChart.DiagramGenerator
```

For analyzers and code fixes:
```bash
dotnet add package CodeJunkie.StateChart.Analyzers
```

## Quick Start

Here's a simple timer that can be turned on and off:

```csharp
using CodeJunkie.StateChart;
using CodeJunkie.Metadata;

[Meta, StateChart(typeof(State), Diagram = true)]
public partial class TimerStateChart : StateChart<TimerStateChart.State> {
    // Define inputs - events that trigger transitions
    public static class Input {
        public readonly record struct PowerButtonPressed;
    }

    // Define outputs - events emitted to external systems
    public static class Output {
        public readonly record struct PoweredOff;
        public readonly record struct PoweredOn;
    }

    // Define state hierarchy
    public abstract record State : StateLogic<State> {
        public record PoweredOff : State, IGet<Input.PowerButtonPressed> {
            public PoweredOff() {
                OnEnter(() => Output(new Output.PoweredOff()));
            }

            public Transition On(in Input.PowerButtonPressed input) => To<PoweredOn>();
        }

        public record PoweredOn : State, IGet<Input.PowerButtonPressed> {
            public PoweredOn() {
                OnEnter(() => Output(new Output.PoweredOn()));
            }

            public Transition On(in Input.PowerButtonPressed input) => To<PoweredOff>();
        }
    }

    public override Transition GetInitialState() => To<State.PoweredOff>();
}
```

Using the StateChart:

```csharp
// Create and start the StateChart
var timer = new TimerStateChart();

// Subscribe to outputs
timer.Bind()
    .Handle((in TimerStateChart.Output.PoweredOn _) =>
        Console.WriteLine("Timer powered on"))
    .Handle((in TimerStateChart.Output.PoweredOff _) =>
        Console.WriteLine("Timer powered off"));

// Start the StateChart
timer.Start();

// Send inputs to trigger transitions
timer.Input(new TimerStateChart.Input.PowerButtonPressed()); // Powers on
timer.Input(new TimerStateChart.Input.PowerButtonPressed()); // Powers off
```

## Core Concepts

### States

States represent distinct modes or configurations of your system. In StateChart, states are defined as nested record types that inherit from `StateLogic<TState>`.

```csharp
public abstract record State : StateLogic<State> {
    public record Idle : State { }
    public record Moving : State { }
    public record Attacking : State { }
}
```

### Inputs

Inputs are events that trigger state transitions. They're defined as readonly record structs for optimal performance and immutability.

```csharp
public static class Input {
    public readonly record struct Jump;
    public readonly record struct Attack(bool IsHeavy);
    public readonly record struct Move(float X, float Y);
}
```

States handle inputs by implementing `IGet<TInput>`:

```csharp
public record Idle : State, IGet<Input.Jump>, IGet<Input.Attack> {
    public Transition On(in Input.Jump input) => To<Jumping>();

    public Transition On(in Input.Attack input) {
        return input.IsHeavy ? To<HeavyAttack>() : To<LightAttack>();
    }
}
```

### Outputs

Outputs are events emitted by states to communicate with external systems (animations, sound, UI, etc.).

```csharp
public static class Output {
    public readonly record struct JumpStarted(float Force);
    public readonly record struct AttackPerformed(int Damage);
}
```

Emit outputs from within states:

```csharp
public record Jumping : State {
    public Jumping() {
        OnEnter(() => {
            Output(new Output.JumpStarted(10.0f));
        });
    }
}
```

### Transitions

Transitions move the StateChart from one state to another:

```csharp
// Transition to a different state
return To<NewState>();

// Stay in the current state
return ToSelf();

// Conditional transitions
return health > 0 ? To<Alive>() : To<Dead>();
```

### Data Management

StateChart includes a built-in blackboard pattern for sharing data across states:

```csharp
public record PlayerData {
    public int Health { get; set; } = 100;
    public int Stamina { get; set; } = 100;
}

// In StateChart constructor
public PlayerStateChart() {
    Set(new PlayerData());
}

// Access from any state
public Transition On(in Input.TakeDamage input) {
    var data = Get<PlayerData>();
    data.Health -= input.Amount;

    return data.Health <= 0 ? To<Dead>() : ToSelf();
}
```

### Bindings

Bindings allow external systems to reactively observe state changes and outputs:

```csharp
var binding = stateChart.Bind()
    // Handle specific outputs
    .Handle((in Output.JumpStarted jump) =>
        PlayAnimation("Jump"))

    // Observe state changes
    .When<State.Idle>((state) =>
        Console.WriteLine("Player is idle"))

    // Generic output handler
    .Handle<Output.AttackPerformed>((in Output.AttackPerformed attack) =>
        SpawnHitEffect(attack.Damage));
```

## Defining StateCharts

### Basic Structure

Every StateChart follows this pattern:

```csharp
[Meta, StateChart(typeof(State))]
public partial class MyStateChart : StateChart<MyStateChart.State> {
    // 1. Define inputs
    public static class Input {
        public readonly record struct MyInput;
    }

    // 2. Define outputs
    public static class Output {
        public readonly record struct MyOutput;
    }

    // 3. Define states
    public abstract record State : StateLogic<State> {
        public record MyState : State { }
    }

    // 4. Specify initial state
    public override Transition GetInitialState() => To<State.MyState>();
}
```

### Handling Inputs

States handle inputs by implementing `IGet<TInput>`:

```csharp
public record Idle : State,
    IGet<Input.Move>,
    IGet<Input.Jump>,
    IGet<Input.Attack> {

    public Transition On(in Input.Move input) {
        return input.IsSprinting ? To<Running>() : To<Walking>();
    }

    public Transition On(in Input.Jump input) => To<Jumping>();

    public Transition On(in Input.Attack input) => To<Attacking>();
}
```

### Lifecycle Callbacks

Use `OnEnter` and `OnExit` for state lifecycle management:

```csharp
public record Walking : State {
    public Walking() {
        OnEnter(() => {
            Console.WriteLine("Started walking");
            Output(new Output.AnimationChanged("Walk"));
        });

        OnExit(() => {
            Console.WriteLine("Stopped walking");
        });
    }
}
```

## Hierarchical States

Group related states under abstract parent states to share behavior:

```csharp
public abstract record State : StateLogic<State> {
    // Parent state with shared input handling
    public abstract record Moving : State, IGet<Input.Stop> {
        // All Moving substates can handle Stop input
        public Transition On(in Input.Stop input) => To<Idle>();

        // Concrete substates
        public record Walking : Moving { }
        public record Running : Moving { }
        public record Crouching : Moving { }
    }

    public record Idle : State, IGet<Input.Move> {
        public Transition On(in Input.Move input) => To<Moving.Walking>();
    }
}
```

This allows:
- Shared input handlers across related states
- Logical grouping of states
- Cleaner state transitions

## Working with StateCharts

### Starting and Stopping

```csharp
var stateChart = new MyStateChart();

// Start the StateChart (enters initial state)
stateChart.Start();

// Check if started
if (stateChart.IsStarted) {
    // Send inputs
}

// Stop the StateChart
stateChart.Stop();
```

### Sending Inputs

```csharp
// Simple input
stateChart.Input(new Input.Jump());

// Input with data
stateChart.Input(new Input.Move(DirectionX: 1.0f, DirectionY: 0.0f));

// Conditional inputs
if (isAttacking) {
    stateChart.Input(new Input.Attack(IsHeavy: true));
}
```

### Observing State and Outputs

```csharp
var stateChart = new PlayerStateChart();

var binding = stateChart.Bind()
    // Handle specific output types
    .Handle((in Output.JumpStarted jump) => {
        ApplyForce(jump.Force);
        PlaySound("Jump");
    })

    // Observe when entering specific states
    .When<State.Idle>((state) => {
        StopMovement();
    })

    // Generic state change observer
    .When<State>((state) => {
        Debug.Log($"State changed to: {state.GetType().Name}");
    });

stateChart.Start();

// Don't forget to dispose bindings when done
binding.Dispose();
```

### Multiple Bindings

You can create multiple bindings to the same StateChart:

```csharp
// Animation system binding
var animBinding = stateChart.Bind()
    .Handle((in Output.AnimationChanged anim) =>
        PlayAnimation(anim.Name));

// Sound system binding
var soundBinding = stateChart.Bind()
    .Handle((in Output.JumpStarted _) =>
        PlaySound("Jump"))
    .Handle((in Output.AttackPerformed _) =>
        PlaySound("Attack"));

// UI binding
var uiBinding = stateChart.Bind()
    .Handle((in Output.HealthChanged health) =>
        UpdateHealthBar(health.Current, health.Max));
```

## Diagram Generation

StateChart can automatically generate visual diagrams of your state machines.

### Configuration

Enable diagram generation with the `StateChart` attribute:

```csharp
// Generate PlantUML only (legacy)
[Meta, StateChart(typeof(State), Diagram = true)]
public partial class MyStateChart : StateChart<MyStateChart.State> { }

// Generate specific formats
[Meta, StateChart(typeof(State), DiagramFormats = DiagramFormat.Mermaid)]
public partial class MyStateChart : StateChart<MyStateChart.State> { }

// Generate multiple formats
[Meta, StateChart(typeof(State),
    DiagramFormats = DiagramFormat.PlantUML | DiagramFormat.Mermaid)]
public partial class MyStateChart : StateChart<MyStateChart.State> { }

// Generate all formats
[Meta, StateChart(typeof(State), DiagramFormats = DiagramFormat.All)]
public partial class MyStateChart : StateChart<MyStateChart.State> { }
```

### Generated Files

The source generator creates files next to your StateChart source file:

- **`.g.puml`** - PlantUML diagram (view with PlantUML tools)
- **`.g.mermaid`** - Mermaid diagram (view in GitHub, GitLab, or Mermaid Live)
- **`.g.md`** - Markdown documentation with embedded Mermaid diagram

These diagrams visualize:
- All states and their hierarchy
- Input handlers
- State transitions
- Initial state

## StateChart Monitoring

StateChart includes a built-in registry system for monitoring all active StateChart instances in your application.

### Accessing Active Instances

```csharp
// Get all active StateChart instances
var allCharts = StateChartRegistry.GetAllActiveInstances();

// Get instances of a specific type
var playerCharts = StateChartRegistry.GetActiveInstances<PlayerStateChart>();

// Filter with a predicate
var startedCharts = StateChartRegistry.GetActiveInstances(x => x.IsStarted);

// Get instances grouped by type
var chartsByType = StateChartRegistry.GetActiveInstancesByType();

// Check count
int count = StateChartRegistry.ActiveInstanceCount;
```

### Building Monitoring Tools

```csharp
public class StateChartDebugger {
    private Timer _timer;

    public void StartMonitoring() {
        _timer = new Timer(_ => {
            Console.WriteLine("=== Active StateCharts ===");

            var charts = StateChartRegistry.GetAllActiveInstances();
            foreach (var chart in charts) {
                var type = chart.GetType().Name;
                var state = chart.Value?.GetType().Name ?? "null";
                var started = chart.IsStarted;

                Console.WriteLine($"{type}: {state} (Started: {started})");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
}
```

### Opting Out of Monitoring

For performance-critical StateCharts, disable monitoring:

```csharp
[NoMonitoring]
public class HighPerformanceStateChart : StateChart<State> {
    // This instance won't be tracked by the registry
}
```

Or disable globally:

```csharp
StateChartRegistry.IsMonitoringEnabled = false;
```

### Memory Management

- Uses `WeakReference` to prevent memory leaks
- StateCharts are automatically removed when garbage collected
- No explicit cleanup required

## Testing

### Unit Testing States

States can be tested in isolation:

```csharp
[Fact]
public void Idle_OnJumpInput_TransitionsToJumping() {
    // Arrange
    var stateChart = new PlayerStateChart();
    stateChart.Start();

    // Assert initial state
    Assert.IsType<PlayerStateChart.State.Idle>(stateChart.Value);

    // Act
    stateChart.Input(new PlayerStateChart.Input.Jump());

    // Assert transition occurred
    Assert.IsType<PlayerStateChart.State.Jumping>(stateChart.Value);
}
```

### Testing Outputs

Use bindings to verify outputs:

```csharp
[Fact]
public void Jumping_OnEnter_EmitsJumpOutput() {
    // Arrange
    var stateChart = new PlayerStateChart();
    var jumpEmitted = false;

    stateChart.Bind()
        .Handle((in PlayerStateChart.Output.JumpStarted _) => {
            jumpEmitted = true;
        });

    stateChart.Start();

    // Act
    stateChart.Input(new PlayerStateChart.Input.Jump());

    // Assert
    Assert.True(jumpEmitted);
}
```

### Fake Bindings for Testing

Create fake bindings that don't execute callbacks:

```csharp
[Fact]
public void TestWithoutSideEffects() {
    var stateChart = new MyStateChart();
    var fakeBinding = StateChart.CreateFakeBinding();

    // Binding won't execute any handlers
    stateChart.Start();
}
```

### Test Isolation

Reset the registry between tests:

```csharp
public class MyTests : IDisposable {
    public MyTests() {
        StateChartRegistry.Reset();
    }

    public void Dispose() {
        StateChartRegistry.Reset();
    }
}
```

## Complete Example: PlayerCharacter

Here's a comprehensive example modeling a game character with multiple states:

```csharp
using CodeJunkie.StateChart;
using CodeJunkie.Metadata;

[Meta, StateChart(typeof(State), DiagramFormats = DiagramFormat.All)]
public partial class PlayerCharacter : StateChart<PlayerCharacter.State> {
    public static class Input {
        public readonly record struct Move(float X, float Y, bool IsSprinting);
        public readonly record struct Jump;
        public readonly record struct Attack(bool IsHeavy);
        public readonly record struct TakeDamage(int Amount);
        public readonly record struct Land;
        public readonly record struct AttackComplete;
    }

    public static class Output {
        public readonly record struct AnimationChanged(string Name);
        public readonly record struct JumpStarted(float Force);
        public readonly record struct AttackPerformed(string Type, int Damage);
        public readonly record struct HealthChanged(int Current, int Max);
        public readonly record struct Died;
    }

    public record PlayerData {
        public int Health { get; set; } = 100;
        public int MaxHealth { get; } = 100;
        public int Stamina { get; set; } = 100;
    }

    public abstract record State : StateLogic<State> {
        // Idle state
        public record Idle : State,
            IGet<Input.Move>,
            IGet<Input.Jump>,
            IGet<Input.Attack>,
            IGet<Input.TakeDamage> {

            public Idle() {
                OnEnter(() => Output(new Output.AnimationChanged("Idle")));
            }

            public Transition On(in Input.Move input) {
                return input.IsSprinting ? To<Running>() : To<Walking>();
            }

            public Transition On(in Input.Jump input) => To<Jumping>();

            public Transition On(in Input.Attack input) {
                return input.IsHeavy ? To<HeavyAttack>() : To<LightAttack>();
            }

            public Transition On(in Input.TakeDamage input) {
                var data = Get<PlayerData>();
                data.Health -= input.Amount;
                Output(new Output.HealthChanged(data.Health, data.MaxHealth));

                return data.Health <= 0 ? To<Dead>() : ToSelf();
            }
        }

        // Movement states (hierarchical)
        public abstract record Moving : State, IGet<Input.Jump>, IGet<Input.Attack> {
            public Transition On(in Input.Jump input) => To<Jumping>();

            public Transition On(in Input.Attack input) {
                return input.IsHeavy ? To<HeavyAttack>() : To<LightAttack>();
            }

            public record Walking : Moving, IGet<Input.Move> {
                public Walking() {
                    OnEnter(() => Output(new Output.AnimationChanged("Walk")));
                }

                public Transition On(in Input.Move input) {
                    if (input is { X: 0, Y: 0 }) return To<Idle>();
                    return input.IsSprinting ? To<Running>() : ToSelf();
                }
            }

            public record Running : Moving, IGet<Input.Move> {
                public Running() {
                    OnEnter(() => Output(new Output.AnimationChanged("Run")));
                }

                public Transition On(in Input.Move input) {
                    if (input is { X: 0, Y: 0 }) return To<Idle>();

                    var data = Get<PlayerData>();
                    data.Stamina = Math.Max(0, data.Stamina - 1);

                    if (data.Stamina == 0 || !input.IsSprinting) {
                        return To<Walking>();
                    }

                    return ToSelf();
                }
            }
        }

        // Jumping
        public record Jumping : State, IGet<Input.Land>, IGet<Input.Attack> {
            public Jumping() {
                OnEnter(() => {
                    Output(new Output.JumpStarted(10.0f));
                    Output(new Output.AnimationChanged("Jump"));
                });
            }

            public Transition On(in Input.Land input) => To<Idle>();

            public Transition On(in Input.Attack input) {
                return input.IsHeavy ? To<HeavyAttack>() : To<LightAttack>();
            }
        }

        // Attack states
        public abstract record Attacking : State, IGet<Input.AttackComplete> {
            public Transition On(in Input.AttackComplete input) => To<Idle>();

            public record LightAttack : Attacking {
                public LightAttack() {
                    OnEnter(() => {
                        Output(new Output.AttackPerformed("Light", 10));
                        Output(new Output.AnimationChanged("LightAttack"));
                    });
                }
            }

            public record HeavyAttack : Attacking {
                public HeavyAttack() {
                    OnEnter(() => {
                        Output(new Output.AttackPerformed("Heavy", 25));
                        Output(new Output.AnimationChanged("HeavyAttack"));
                    });
                }
            }
        }

        // Dead state
        public record Dead : State {
            public Dead() {
                OnEnter(() => {
                    Output(new Output.Died());
                    Output(new Output.AnimationChanged("Death"));
                });
            }
        }
    }

    public override Transition GetInitialState() => To<State.Idle>();

    public PlayerCharacter() {
        Set(new PlayerData());
    }
}
```

Usage:

```csharp
// Create and configure player
var player = new PlayerCharacter();

// Hook up to animation system
player.Bind()
    .Handle((in PlayerCharacter.Output.AnimationChanged anim) =>
        animator.Play(anim.Name))
    .Handle((in PlayerCharacter.Output.JumpStarted jump) =>
        rigidbody.AddForce(Vector3.up * jump.Force))
    .Handle((in PlayerCharacter.Output.AttackPerformed attack) =>
        SpawnAttackHitbox(attack.Type, attack.Damage))
    .Handle((in PlayerCharacter.Output.HealthChanged health) =>
        healthBar.SetValue(health.Current, health.Max))
    .Handle((in PlayerCharacter.Output.Died _) =>
        GameOver());

// Start the player StateChart
player.Start();

// Game loop
void Update() {
    // Movement input
    var moveX = Input.GetAxis("Horizontal");
    var moveY = Input.GetAxis("Vertical");
    var isSprinting = Input.GetKey(KeyCode.LeftShift);

    if (moveX != 0 || moveY != 0) {
        player.Input(new PlayerCharacter.Input.Move(moveX, moveY, isSprinting));
    }

    // Jump input
    if (Input.GetKeyDown(KeyCode.Space)) {
        player.Input(new PlayerCharacter.Input.Jump());
    }

    // Attack input
    if (Input.GetMouseButtonDown(0)) {
        var isHeavy = Input.GetKey(KeyCode.LeftControl);
        player.Input(new PlayerCharacter.Input.Attack(isHeavy));
    }
}
```

## API Reference

### Core Types

#### `StateChart<TState>`
Base class for all StateCharts.

**Methods:**
- `void Start()` - Start the StateChart and enter initial state
- `void Stop()` - Stop the StateChart and exit current state
- `void Input<TInput>(in TInput input)` - Send an input to the current state
- `IBinding Bind()` - Create a new binding for observing the StateChart
- `T Get<T>()` - Get data from the blackboard
- `void Set<T>(T data)` - Set data on the blackboard

**Properties:**
- `TState Value` - Current state
- `bool IsStarted` - Whether the StateChart is started

#### `StateLogic<TState>`
Base class for state definitions.

**Methods:**
- `void OnEnter(Action callback)` - Register callback for state entry
- `void OnExit(Action callback)` - Register callback for state exit
- `Transition To<T>()` - Create transition to state T
- `Transition ToSelf()` - Stay in current state
- `void Output<TOutput>(in TOutput output)` - Emit an output
- `T Get<T>()` - Get data from blackboard

#### `IBinding`
Interface for StateChart bindings.

**Methods:**
- `IBinding Handle<TOutput>(OutputHandler<TOutput> handler)` - Handle specific output type
- `IBinding When<TState>(StateHandler<TState> handler)` - Observe state changes
- `void Dispose()` - Dispose the binding

### Attributes

#### `[StateChart(Type stateType)]`
Mark a partial class as a StateChart. Required for source generators.

**Properties:**
- `bool Diagram` - Generate PlantUML diagram (legacy)
- `DiagramFormat DiagramFormats` - Specify which diagram formats to generate

#### `[Meta]`
Metadata attribute for code generation.

#### `[NoMonitoring]`
Exclude StateChart from the global registry.

### StateChartRegistry

Static class for monitoring active StateChart instances.

**Methods:**
- `IEnumerable<IStateChartBase> GetAllActiveInstances()`
- `IEnumerable<T> GetActiveInstances<T>()`
- `IEnumerable<IStateChartBase> GetActiveInstances(Predicate<IStateChartBase> predicate)`
- `Dictionary<Type, List<IStateChartBase>> GetActiveInstancesByType()`
- `void Reset()` - Clear the registry (testing only)

**Properties:**
- `int ActiveInstanceCount` - Count of active instances
- `bool IsMonitoringEnabled` - Enable/disable monitoring globally

## Best Practices

### State Design

1. **Keep states focused** - Each state should have a single responsibility
2. **Use hierarchies** - Group related states under abstract parent states
3. **Prefer composition** - Break complex states into smaller substates
4. **Immutable inputs/outputs** - Use readonly record structs for safety

### Performance

1. **Pre-allocate states** - For zero-allocation scenarios, pre-allocate state instances
2. **Disable monitoring** - Use `[NoMonitoring]` for performance-critical StateCharts
3. **Minimize OnEnter/OnExit** - Keep lifecycle callbacks lightweight
4. **Batch inputs** - Send multiple inputs in sequence rather than one per frame

### Testing

1. **Test states in isolation** - Don't rely on state transitions for setup
2. **Use fake bindings** - Avoid side effects in unit tests
3. **Reset registry** - Always reset `StateChartRegistry` between tests
4. **Verify outputs** - Test that states emit expected outputs

### Organization

1. **One StateChart per file** - Keep StateChart definitions focused
2. **Nested classes** - Use nested Input/Output/Data classes for organization
3. **Meaningful names** - Use descriptive state and input names
4. **Document complex logic** - Add XML comments for non-obvious behavior

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]

## Support

For issues, questions, or contributions, please visit [your repository URL].
