# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run a Single Test
```bash
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

### Clean Build
```bash
dotnet clean && dotnet build
```

### Generate NuGet Packages
```bash
dotnet pack
```

## Architecture Overview

This repository contains a C# state chart framework for modeling behavior using statecharts with built-in support for hierarchy, nesting, and event-driven transitions. The framework is fully serializable and works well in AOT environments.

### Key Projects

- **CodeJunkie.StateChart**: Core state chart implementation
  - Provides the base `StateChart<TState>` and `StateLogic<TState>` classes
  - Handles state transitions, input processing, and output generation
  - Supports hierarchical and nested states
  - Includes serialization support

- **CodeJunkie.StateChart.DiagramGenerator**: Source generator for creating PlantUML diagrams
  - Automatically generates `.g.puml` files from state charts marked with `[StateChart(typeof(State), Diagram = true)]`
  - Visualizes state transitions and hierarchies

- **CodeJunkie.StateChart.Analyzers**: Roslyn analyzers for compile-time validation
  - Ensures proper state chart structure

- **CodeJunkie.StateChart.CodeFixes**: Code fixes for common state chart issues

### Core Concepts

1. **States**: Defined as nested record types inheriting from `StateLogic<TState>`
   - Each state can handle inputs via `IGet<TInput>` interfaces
   - States can have OnEnter/OnExit callbacks
   - States can produce outputs using `Output<TOutput>()`

2. **Inputs**: Readonly record structs that trigger state transitions
   - Defined in nested `Input` class
   - Processed via `Input<TInputType>()` method

3. **Outputs**: Readonly record structs emitted by states
   - Defined in nested `Output` class
   - Can be monitored via bindings

4. **Transitions**: Created using `To<TStateType>()` method
   - Returns next state to transition to

5. **Bindings**: Allow external monitoring of state changes, inputs, and outputs
   - Created via `StateChart.Bind()`
   - Support reactive programming patterns

### Usage Pattern

```csharp
[Meta, StateChart(typeof(State), Diagram = true)]
public partial class MyStateChart : StateChart<MyStateChart.State> {
    public static class Input {
        public readonly record struct MyInput;
    }
    
    public static class Output {
        public readonly record struct MyOutput;
    }
    
    public abstract record State : StateLogic<State> {
        public record StateA : State, IGet<Input.MyInput> {
            public Transition On(in Input.MyInput input) => To<StateB>();
        }
        
        public record StateB : State {
            public StateB() {
                OnEnter(() => Output(new Output.MyOutput()));
            }
        }
    }
    
    public override Transition GetInitialState() => To<State.StateA>();
}
```

### Working with Source Generators

The `DiagramGenerator` creates state chart diagrams in multiple formats:

1. **PlantUML** (`.g.puml`) - Visual state diagrams
2. **Mermaid** (`.g.mermaid`) - Web-friendly state diagrams
3. **Markdown** (`.g.md`) - Comprehensive documentation with embedded Mermaid diagram

To generate diagrams:
- For PlantUML only: `[StateChart(typeof(State), Diagram = true)]`
- For specific formats: `[StateChart(typeof(State), DiagramFormats = DiagramFormat.PlantUML | DiagramFormat.Mermaid)]`
- For all formats: `[StateChart(typeof(State), DiagramFormats = DiagramFormat.All)]`

Generated files appear next to source files with appropriate extensions.

### Testing Considerations

- Use `StateChart.CreateFakeBinding()` for unit testing
- States can be tested in isolation by calling `Enter()` and `Exit()` methods
- Input handlers can be tested directly