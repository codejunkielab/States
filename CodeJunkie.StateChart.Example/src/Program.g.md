# Timer State Chart

Generated on: 2025-07-28 17:56:52

## State Hierarchy

- **Timer State**
  - **PoweredOff**
  - **PoweredOn**

## Initial States

- **PoweredOff**

## State Transitions

| From State | Input | To State |
|------------|-------|----------|
| PoweredOff | PowerButtonPressed | PoweredOn |
| PoweredOn | PowerButtonPressed | PoweredOff |

## State Diagram

```mermaid
stateDiagram-v2
    state "Timer State" as CodeJunkie_StateChart_Example_Timer_State {
        state "PoweredOff" as CodeJunkie_StateChart_Example_Timer_State_PoweredOff
        state "PoweredOn" as CodeJunkie_StateChart_Example_Timer_State_PoweredOn
    }
    [*] --> CodeJunkie_StateChart_Example_Timer_State_PoweredOff
    CodeJunkie_StateChart_Example_Timer_State_PoweredOff --> CodeJunkie_StateChart_Example_Timer_State_PoweredOn : PowerButtonPressed
    CodeJunkie_StateChart_Example_Timer_State_PoweredOn --> CodeJunkie_StateChart_Example_Timer_State_PoweredOff : PowerButtonPressed

```