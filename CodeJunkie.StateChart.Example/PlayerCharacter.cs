namespace CodeJunkie.StateChart.Example;

using System;
using CodeJunkie.Log;
using CodeJunkie.Metadata;
using CodeJunkie.StateChart;

/// <summary>
/// Example StateChart demonstrating player character state management in a game.
/// This example showcases:
/// - Hierarchical state structures (Moving, InAir, Attacking)
/// - Event-driven input processing (Move, Jump, Attack)
/// - Output monitoring for external systems (animations, sound, VFX)
/// - Data state management (health, stamina)
/// </summary>
[Meta, StateChart(typeof(State), DiagramFormats = DiagramFormat.All)]
public partial class PlayerCharacter : StateChart<PlayerCharacter.State> {
  /// <summary>
  /// Input events that drive state transitions
  /// </summary>
  public static class Input {
    /// <summary>
    /// Player movement input with direction and modifiers
    /// </summary>
    public readonly record struct MoveInput(float DirectionX, float DirectionY, bool IsSprinting, bool IsCrouching);

    /// <summary>
    /// Player jump input
    /// </summary>
    public readonly record struct JumpInput;

    /// <summary>
    /// Player attack input
    /// </summary>
    public readonly record struct AttackInput(bool IsHeavy);

    /// <summary>
    /// Player takes damage
    /// </summary>
    public readonly record struct TakeDamageInput(int Damage);

    /// <summary>
    /// Player lands on ground
    /// </summary>
    public readonly record struct LandInput;

    /// <summary>
    /// Attack animation completed
    /// </summary>
    public readonly record struct AttackCompletedInput;

    /// <summary>
    /// Stop movement input
    /// </summary>
    public readonly record struct StopMoveInput;
  }

  /// <summary>
  /// Output events for external systems (animations, sound, VFX, UI)
  /// </summary>
  public static class Output {
    /// <summary>
    /// Movement state changed - used to trigger animations
    /// </summary>
    public readonly record struct MovementStateChanged(string StateName);

    /// <summary>
    /// Jump was triggered - trigger jump animation and sound
    /// </summary>
    public readonly record struct JumpTriggered(float JumpForce);

    /// <summary>
    /// Attack was performed - spawn attack hitbox and effects
    /// </summary>
    public readonly record struct AttackPerformed(string AttackType, int Damage);

    /// <summary>
    /// Character took damage - update UI and trigger hit effects
    /// </summary>
    public readonly record struct DamageTaken(int Damage, int RemainingHealth);

    /// <summary>
    /// Character died - trigger death animation and game over logic
    /// </summary>
    public readonly record struct CharacterDied;

    /// <summary>
    /// Animation should be triggered
    /// </summary>
    public readonly record struct AnimationTriggered(string AnimationName);

    /// <summary>
    /// Character landed on ground
    /// </summary>
    public readonly record struct Landed;
  }

  /// <summary>
  /// Character data that persists across states
  /// </summary>
  public record Data {
    public int Health { get; set; } = 100;
    public int MaxHealth { get; } = 100;
    public int Stamina { get; set; } = 100;
    public int MaxStamina { get; } = 100;
  }

  /// <summary>
  /// State hierarchy for player character
  /// </summary>
  public abstract record State : StateLogic<State> {
    /// <summary>
    /// Idle state - player is standing still
    /// </summary>
    public record Idle : State, IGet<Input.MoveInput>, IGet<Input.JumpInput>, IGet<Input.AttackInput>, IGet<Input.TakeDamageInput> {
      private readonly Log _log = LogManager.GetLogger<Idle>();

      public Idle() {
        this.OnEnter(() => {
          _log.Info("Player idle");
          Output(new Output.MovementStateChanged("Idle"));
          Output(new Output.AnimationTriggered("Idle"));
        });
      }

      public Transition On(in Input.MoveInput input) {
        if (input.IsCrouching) {
          return To<Moving.Crouching>();
        }
        if (input.IsSprinting) {
          var data = Get<Data>();
          if (data.Stamina > 0) {
            return To<Moving.Running>();
          }
        }
        return To<Moving.Walking>();
      }

      public Transition On(in Input.JumpInput input) => To<InAir.Jumping>();

      public Transition On(in Input.AttackInput input) {
        return input.IsHeavy ? To<Attacking.HeavyAttack>() : To<Attacking.LightAttack>();
      }

      public Transition On(in Input.TakeDamageInput input) {
        var data = Get<Data>();
        data.Health -= input.Damage;
        Output(new Output.DamageTaken(input.Damage, data.Health));

        if (data.Health <= 0) {
          return To<Dead>();
        }
        return ToSelf();
      }
    }

    /// <summary>
    /// Moving state - player is moving (hierarchical state with sub-states)
    /// </summary>
    public abstract record Moving : State, IGet<Input.StopMoveInput>, IGet<Input.JumpInput>, IGet<Input.AttackInput>, IGet<Input.TakeDamageInput> {
      public Transition On(in Input.StopMoveInput input) => To<Idle>();

      public Transition On(in Input.JumpInput input) => To<InAir.Jumping>();

      public Transition On(in Input.AttackInput input) {
        return input.IsHeavy ? To<Attacking.HeavyAttack>() : To<Attacking.LightAttack>();
      }

      public Transition On(in Input.TakeDamageInput input) {
        var data = Get<Data>();
        data.Health -= input.Damage;
        Output(new Output.DamageTaken(input.Damage, data.Health));

        if (data.Health <= 0) {
          return To<Dead>();
        }
        return ToSelf();
      }

      /// <summary>
      /// Walking state
      /// </summary>
      public record Walking : Moving, IGet<Input.MoveInput> {
        private readonly Log _log = LogManager.GetLogger<Walking>();

        public Walking() {
          this.OnEnter(() => {
            _log.Info("Player walking");
            Output(new Output.MovementStateChanged("Walking"));
            Output(new Output.AnimationTriggered("Walk"));
          });
        }

        public Transition On(in Input.MoveInput input) {
          if (input.IsCrouching) {
            return To<Crouching>();
          }
          if (input.IsSprinting) {
            var data = Get<Data>();
            if (data.Stamina > 0) {
              return To<Running>();
            }
          }
          return ToSelf();
        }
      }

      /// <summary>
      /// Running state - consumes stamina
      /// </summary>
      public record Running : Moving, IGet<Input.MoveInput> {
        private readonly Log _log = LogManager.GetLogger<Running>();

        public Running() {
          this.OnEnter(() => {
            _log.Info("Player running");
            Output(new Output.MovementStateChanged("Running"));
            Output(new Output.AnimationTriggered("Run"));
          });
        }

        public Transition On(in Input.MoveInput input) {
          var data = Get<Data>();

          if (input.IsCrouching) {
            return To<Crouching>();
          }

          if (!input.IsSprinting) {
            return To<Walking>();
          }

          // Deplete stamina while running
          data.Stamina = Math.Max(0, data.Stamina - 1);
          if (data.Stamina <= 0) {
            _log.Info("Out of stamina");
            return To<Walking>();
          }

          return ToSelf();
        }
      }

      /// <summary>
      /// Crouching state
      /// </summary>
      public record Crouching : Moving, IGet<Input.MoveInput> {
        private readonly Log _log = LogManager.GetLogger<Crouching>();

        public Crouching() {
          this.OnEnter(() => {
            _log.Info("Player crouching");
            Output(new Output.MovementStateChanged("Crouching"));
            Output(new Output.AnimationTriggered("Crouch"));
          });
        }

        public Transition On(in Input.MoveInput input) {
          if (!input.IsCrouching) {
            return To<Walking>();
          }
          return ToSelf();
        }
      }
    }

    /// <summary>
    /// InAir state - player is in the air (jumping or falling)
    /// </summary>
    public abstract record InAir : State, IGet<Input.LandInput>, IGet<Input.AttackInput>, IGet<Input.TakeDamageInput> {
      public Transition On(in Input.LandInput input) {
        Output(new Output.Landed());
        return To<Idle>();
      }

      public Transition On(in Input.AttackInput input) {
        // Air attacks are allowed
        return input.IsHeavy ? To<Attacking.HeavyAttack>() : To<Attacking.LightAttack>();
      }

      public Transition On(in Input.TakeDamageInput input) {
        var data = Get<Data>();
        data.Health -= input.Damage;
        Output(new Output.DamageTaken(input.Damage, data.Health));

        if (data.Health <= 0) {
          return To<Dead>();
        }
        return ToSelf();
      }

      /// <summary>
      /// Jumping state
      /// </summary>
      public record Jumping : InAir {
        private readonly Log _log = LogManager.GetLogger<Jumping>();

        public Jumping() {
          this.OnEnter(() => {
            _log.Info("Player jumping");
            Output(new Output.JumpTriggered(10.0f));
            Output(new Output.AnimationTriggered("Jump"));
          });
        }
      }

      /// <summary>
      /// Falling state
      /// </summary>
      public record Falling : InAir {
        private readonly Log _log = LogManager.GetLogger<Falling>();

        public Falling() {
          this.OnEnter(() => {
            _log.Info("Player falling");
            Output(new Output.AnimationTriggered("Fall"));
          });
        }
      }
    }

    /// <summary>
    /// Attacking state - player is performing an attack
    /// </summary>
    public abstract record Attacking : State, IGet<Input.AttackCompletedInput>, IGet<Input.TakeDamageInput> {
      public Transition On(in Input.AttackCompletedInput input) => To<Idle>();

      public Transition On(in Input.TakeDamageInput input) {
        var data = Get<Data>();
        data.Health -= input.Damage;
        Output(new Output.DamageTaken(input.Damage, data.Health));

        if (data.Health <= 0) {
          return To<Dead>();
        }
        // Continue attack despite taking damage
        return ToSelf();
      }

      /// <summary>
      /// Light attack - fast, low damage
      /// </summary>
      public record LightAttack : Attacking {
        private readonly Log _log = LogManager.GetLogger<LightAttack>();

        public LightAttack() {
          this.OnEnter(() => {
            _log.Info("Player performs light attack");
            Output(new Output.AttackPerformed("Light", 10));
            Output(new Output.AnimationTriggered("LightAttack"));
          });
        }
      }

      /// <summary>
      /// Heavy attack - slow, high damage
      /// </summary>
      public record HeavyAttack : Attacking {
        private readonly Log _log = LogManager.GetLogger<HeavyAttack>();

        public HeavyAttack() {
          this.OnEnter(() => {
            _log.Info("Player performs heavy attack");
            Output(new Output.AttackPerformed("Heavy", 30));
            Output(new Output.AnimationTriggered("HeavyAttack"));
          });
        }
      }
    }

    /// <summary>
    /// Dead state - player has died, no inputs accepted
    /// </summary>
    public record Dead : State {
      private readonly Log _log = LogManager.GetLogger<Dead>();

      public Dead() {
        this.OnEnter(() => {
          _log.Info("Player died");
          Output(new Output.CharacterDied());
          Output(new Output.AnimationTriggered("Death"));
        });
      }
    }
  }

  public override Transition GetInitialState() => To<State.Idle>();

  public PlayerCharacter() {
    var log = LogManager.GetLogger<PlayerCharacter>();
    log.Info("PlayerCharacter initialized");

    // Initialize character data
    Set(new Data());
  }
}
