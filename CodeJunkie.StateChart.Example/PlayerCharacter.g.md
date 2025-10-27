# PlayerCharacter State Chart

Generated on: 2025-10-27 16:43:31

## State Hierarchy

- **PlayerCharacter State**
  - **Attacking**
    - OnTakeDamageInput: DamageTaken
    - **HeavyAttack**
      - OnEnter: AnimationTriggered, AttackPerformed
    - **LightAttack**
      - OnEnter: AnimationTriggered, AttackPerformed
  - **Dead**
    - OnEnter: AnimationTriggered, CharacterDied
  - **Idle**
    - OnEnter: AnimationTriggered, MovementStateChanged
    - OnTakeDamageInput: DamageTaken
  - **InAir**
    - OnLandInput: Landed
    - OnTakeDamageInput: DamageTaken
    - **Falling**
      - OnEnter: AnimationTriggered
    - **Jumping**
      - OnEnter: AnimationTriggered, JumpTriggered
  - **Moving**
    - OnTakeDamageInput: DamageTaken
    - **Crouching**
      - OnEnter: AnimationTriggered, MovementStateChanged
    - **Running**
      - OnEnter: AnimationTriggered, MovementStateChanged
    - **Walking**
      - OnEnter: AnimationTriggered, MovementStateChanged

## Initial States

- **Idle**

## State Transitions

| From State | Input | To State |
|------------|-------|----------|
| Attacking | AttackCompletedInput | Idle |
| Attacking | TakeDamageInput | Attacking |
| Attacking | TakeDamageInput | Dead |
| Idle | AttackInput | HeavyAttack |
| Idle | AttackInput | LightAttack |
| Idle | JumpInput | Jumping |
| Idle | MoveInput | Crouching |
| Idle | MoveInput | Running |
| Idle | MoveInput | Walking |
| Idle | TakeDamageInput | Dead |
| Idle | TakeDamageInput | Idle |
| InAir | AttackInput | HeavyAttack |
| InAir | AttackInput | LightAttack |
| InAir | LandInput | Idle |
| InAir | TakeDamageInput | Dead |
| InAir | TakeDamageInput | InAir |
| Moving | AttackInput | HeavyAttack |
| Moving | AttackInput | LightAttack |
| Moving | JumpInput | Jumping |
| Moving | StopMoveInput | Idle |
| Moving | TakeDamageInput | Dead |
| Moving | TakeDamageInput | Moving |
| Crouching | MoveInput | Crouching |
| Crouching | MoveInput | Walking |
| Running | MoveInput | Crouching |
| Running | MoveInput | Running |
| Running | MoveInput | Walking |
| Walking | MoveInput | Crouching |
| Walking | MoveInput | Running |
| Walking | MoveInput | Walking |

## State Outputs

| State | Context | Outputs |
|-------|---------|---------|
| Attacking | OnTakeDamageInput | DamageTaken |
| HeavyAttack | OnEnter | AnimationTriggered, AttackPerformed |
| LightAttack | OnEnter | AnimationTriggered, AttackPerformed |
| Dead | OnEnter | AnimationTriggered, CharacterDied |
| Idle | OnEnter | AnimationTriggered, MovementStateChanged |
| Idle | OnTakeDamageInput | DamageTaken |
| InAir | OnLandInput | Landed |
| InAir | OnTakeDamageInput | DamageTaken |
| Falling | OnEnter | AnimationTriggered |
| Jumping | OnEnter | AnimationTriggered, JumpTriggered |
| Moving | OnTakeDamageInput | DamageTaken |
| Crouching | OnEnter | AnimationTriggered, MovementStateChanged |
| Running | OnEnter | AnimationTriggered, MovementStateChanged |
| Walking | OnEnter | AnimationTriggered, MovementStateChanged |

## State Diagram

```mermaid
stateDiagram-v2
    state "PlayerCharacter State" as CodeJunkie_StateChart_Example_PlayerCharacter_State {
        state "Attacking" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking {
            state "HeavyAttack" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_HeavyAttack
            state "LightAttack" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_LightAttack
        }
        state "Dead" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead
        state "Idle" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle
        state "InAir" as CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir {
            state "Falling" as CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Falling
            state "Jumping" as CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Jumping
        }
        state "Moving" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving {
            state "Crouching" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching
            state "Running" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running
            state "Walking" as CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking
        }
    }
    [*] --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : AttackCompletedInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_HeavyAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_LightAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Jumping : JumpInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_HeavyAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_LightAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : LandInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir --> CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_HeavyAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_LightAttack : AttackInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : StopMoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Jumping : JumpInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving : TakeDamageInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking --> CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking : MoveInput
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking : OnTakeDamageInput → DamageTaken
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_HeavyAttack : OnEnter → AnimationTriggered, AttackPerformed
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Attacking_LightAttack : OnEnter → AnimationTriggered, AttackPerformed
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Dead : OnEnter → AnimationTriggered, CharacterDied
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : OnEnter → AnimationTriggered, MovementStateChanged
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Idle : OnTakeDamageInput → DamageTaken
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir : OnLandInput → Landed
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir : OnTakeDamageInput → DamageTaken
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Falling : OnEnter → AnimationTriggered
    CodeJunkie_StateChart_Example_PlayerCharacter_State_InAir_Jumping : OnEnter → AnimationTriggered, JumpTriggered
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving : OnTakeDamageInput → DamageTaken
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Crouching : OnEnter → AnimationTriggered, MovementStateChanged
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Running : OnEnter → AnimationTriggered, MovementStateChanged
    CodeJunkie_StateChart_Example_PlayerCharacter_State_Moving_Walking : OnEnter → AnimationTriggered, MovementStateChanged
```