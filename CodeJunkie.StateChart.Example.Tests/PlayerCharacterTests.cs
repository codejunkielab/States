namespace CodeJunkie.StateChart.Example.Tests;

using System;
using System.Collections.Generic;
using CodeJunkie.StateChart.Example;
using Shouldly;
using Xunit;

/// <summary>
/// Example tests for the PlayerCharacter StateChart.
/// These tests demonstrate comprehensive testing patterns for complex StateCharts with:
/// - Hierarchical state transitions (Moving, InAir, Attacking)
/// - Data state management (health, stamina)
/// - Output event monitoring
/// - Edge cases and game logic validation
///
/// Use these patterns as a reference when testing your own game StateCharts.
/// </summary>
public sealed class PlayerCharacterTests : IDisposable {
  public PlayerCharacterTests() {
    // Clean up registry before each test
    StateChartRegistry.Reset();
  }

  public void Dispose() {
    // Clean up registry after each test
    StateChartRegistry.Reset();
    GC.SuppressFinalize(this);
  }

  #region Initial State Tests

  [Fact]
  public void PlayerCharacter_StartsInIdleState() {
    // Arrange & Act
    var player = new PlayerCharacter();
    player.Start();

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();
  }

  [Fact]
  public void PlayerCharacter_InitializesWithFullHealthAndStamina() {
    // Arrange & Act
    var player = new PlayerCharacter();
    player.Start();

    // Assert
    var data = player.Get<PlayerCharacter.Data>();
    data.Health.ShouldBe(100);
    data.Stamina.ShouldBe(100);
  }

  #endregion

  #region Movement State Tests

  [Fact]
  public void MoveInput_FromIdle_TransitionsToWalking() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Walking>();
  }

  [Fact]
  public void MoveInput_WithSprintFlag_TransitionsToRunning() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Running>();
  }

  [Fact]
  public void MoveInput_WithCrouchFlag_TransitionsToCrouching() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, true));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Crouching>();
  }

  [Fact]
  public void StopMoveInput_FromWalking_TransitionsToIdle() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Act
    player.Input(new PlayerCharacter.Input.StopMoveInput());

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();
  }

  [Fact]
  public void Running_DepletesStamina() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));

    var initialStamina = player.Get<PlayerCharacter.Data>().Stamina;

    // Act - Run for a bit
    for (int i = 0; i < 10; i++) {
      player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));
    }

    // Assert
    var currentStamina = player.Get<PlayerCharacter.Data>().Stamina;
    currentStamina.ShouldBeLessThan(initialStamina);
  }

  [Fact]
  public void Running_WithZeroStamina_TransitionsToWalking() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Deplete stamina
    var data = player.Get<PlayerCharacter.Data>();
    data.Stamina = 1;

    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));

    // Act - Try to keep running with no stamina
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Walking>();
  }

  [Fact]
  public void Walking_TransitionsToRunning_WhenSprintIsPressed() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Act
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Running>();
  }

  [Fact]
  public void Crouching_TransitionsToWalking_WhenCrouchIsReleased() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, true));

    // Act
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Walking>();
  }

  #endregion

  #region Jump and InAir State Tests

  [Fact]
  public void JumpInput_FromIdle_TransitionsToJumping() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.InAir.Jumping>();
  }

  [Fact]
  public void JumpInput_FromWalking_TransitionsToJumping() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Act
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.InAir.Jumping>();
  }

  [Fact]
  public void JumpInput_EmitsJumpTriggeredOutput() {
    // Arrange
    var player = new PlayerCharacter();
    var jumpOutputs = new List<PlayerCharacter.Output.JumpTriggered>();

    player.Bind()
      .Handle<PlayerCharacter.Output.JumpTriggered>((in PlayerCharacter.Output.JumpTriggered output) => {
        jumpOutputs.Add(output);
      });

    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Assert
    jumpOutputs.Count.ShouldBe(1);
    jumpOutputs[0].JumpForce.ShouldBe(10.0f);
  }

  [Fact]
  public void LandInput_FromJumping_TransitionsToIdle() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Act
    player.Input(new PlayerCharacter.Input.LandInput());

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();
  }

  [Fact]
  public void LandInput_EmitsLandedOutput() {
    // Arrange
    var player = new PlayerCharacter();
    var landedOutputs = new List<PlayerCharacter.Output.Landed>();

    player.Bind()
      .Handle<PlayerCharacter.Output.Landed>((in PlayerCharacter.Output.Landed output) => {
        landedOutputs.Add(output);
      });

    player.Start();
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Act
    player.Input(new PlayerCharacter.Input.LandInput());

    // Assert
    landedOutputs.Count.ShouldBe(1);
  }

  #endregion

  #region Attack State Tests

  [Fact]
  public void AttackInput_WithLightAttack_TransitionsToLightAttack() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.LightAttack>();
  }

  [Fact]
  public void AttackInput_WithHeavyAttack_TransitionsToHeavyAttack() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(true));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.HeavyAttack>();
  }

  [Fact]
  public void LightAttack_EmitsCorrectAttackPerformed() {
    // Arrange
    var player = new PlayerCharacter();
    var attackOutputs = new List<PlayerCharacter.Output.AttackPerformed>();

    player.Bind()
      .Handle<PlayerCharacter.Output.AttackPerformed>((in PlayerCharacter.Output.AttackPerformed output) => {
        attackOutputs.Add(output);
      });

    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Assert
    attackOutputs.Count.ShouldBe(1);
    attackOutputs[0].AttackType.ShouldBe("Light");
    attackOutputs[0].Damage.ShouldBe(10);
  }

  [Fact]
  public void HeavyAttack_EmitsCorrectAttackPerformed() {
    // Arrange
    var player = new PlayerCharacter();
    var attackOutputs = new List<PlayerCharacter.Output.AttackPerformed>();

    player.Bind()
      .Handle<PlayerCharacter.Output.AttackPerformed>((in PlayerCharacter.Output.AttackPerformed output) => {
        attackOutputs.Add(output);
      });

    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(true));

    // Assert
    attackOutputs.Count.ShouldBe(1);
    attackOutputs[0].AttackType.ShouldBe("Heavy");
    attackOutputs[0].Damage.ShouldBe(30);
  }

  [Fact]
  public void AttackCompletedInput_FromLightAttack_TransitionsToIdle() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Act
    player.Input(new PlayerCharacter.Input.AttackCompletedInput());

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();
  }

  [Fact]
  public void AttackInput_FromWalking_TransitionsToAttacking() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.LightAttack>();
  }

  [Fact]
  public void AttackInput_FromInAir_TransitionsToAttacking() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.JumpInput());

    // Act
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.LightAttack>();
  }

  #endregion

  #region Damage and Death Tests

  [Fact]
  public void TakeDamageInput_ReducesHealth() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    var initialHealth = player.Get<PlayerCharacter.Data>().Health;

    // Act
    player.Input(new PlayerCharacter.Input.TakeDamageInput(20));

    // Assert
    var currentHealth = player.Get<PlayerCharacter.Data>().Health;
    currentHealth.ShouldBe(initialHealth - 20);
  }

  [Fact]
  public void TakeDamageInput_EmitsDamageTakenOutput() {
    // Arrange
    var player = new PlayerCharacter();
    var damageOutputs = new List<PlayerCharacter.Output.DamageTaken>();

    player.Bind()
      .Handle<PlayerCharacter.Output.DamageTaken>((in PlayerCharacter.Output.DamageTaken output) => {
        damageOutputs.Add(output);
      });

    player.Start();

    // Act
    player.Input(new PlayerCharacter.Input.TakeDamageInput(20));

    // Assert
    damageOutputs.Count.ShouldBe(1);
    damageOutputs[0].Damage.ShouldBe(20);
    damageOutputs[0].RemainingHealth.ShouldBe(80);
  }

  [Fact]
  public void TakeDamageInput_WhenHealthReachesZero_TransitionsToDead() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();

    var data = player.Get<PlayerCharacter.Data>();
    data.Health = 20;

    // Act
    player.Input(new PlayerCharacter.Input.TakeDamageInput(20));

    // Assert
    player.Value.ShouldBeOfType<PlayerCharacter.State.Dead>();
  }

  [Fact]
  public void TakeDamageInput_WhenHealthReachesZero_EmitsCharacterDied() {
    // Arrange
    var player = new PlayerCharacter();
    var diedOutputs = new List<PlayerCharacter.Output.CharacterDied>();

    player.Bind()
      .Handle<PlayerCharacter.Output.CharacterDied>((in PlayerCharacter.Output.CharacterDied output) => {
        diedOutputs.Add(output);
      });

    player.Start();

    var data = player.Get<PlayerCharacter.Data>();
    data.Health = 10;

    // Act
    player.Input(new PlayerCharacter.Input.TakeDamageInput(10));

    // Assert
    diedOutputs.Count.ShouldBe(1);
  }

  [Fact]
  public void TakeDamageInput_WhileAttacking_DoesNotInterruptAttack() {
    // Arrange
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.AttackInput(false));

    // Act
    player.Input(new PlayerCharacter.Input.TakeDamageInput(10));

    // Assert - Should still be in attacking state
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.LightAttack>();
  }

  [Fact]
  public void TakeDamageInput_WithLethalDamage_TransitionsToDeadFromAnyState() {
    // Test from Walking
    var player = new PlayerCharacter();
    player.Start();
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));
    player.Input(new PlayerCharacter.Input.TakeDamageInput(100));
    player.Value.ShouldBeOfType<PlayerCharacter.State.Dead>();
  }

  #endregion

  #region Output Monitoring Tests

  [Fact]
  public void MovementStateChanged_EmittedForEachMovementTransition() {
    // Arrange
    var player = new PlayerCharacter();
    var movementOutputs = new List<PlayerCharacter.Output.MovementStateChanged>();

    player.Bind()
      .Handle<PlayerCharacter.Output.MovementStateChanged>((in PlayerCharacter.Output.MovementStateChanged output) => {
        movementOutputs.Add(output);
      });

    // Act
    player.Start(); // Idle
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false)); // Walking
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false)); // Running
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, true)); // Crouching

    // Assert
    movementOutputs.Count.ShouldBe(4);
    movementOutputs[0].StateName.ShouldBe("Idle");
    movementOutputs[1].StateName.ShouldBe("Walking");
    movementOutputs[2].StateName.ShouldBe("Running");
    movementOutputs[3].StateName.ShouldBe("Crouching");
  }

  [Fact]
  public void AnimationTriggered_EmittedForAllStateChanges() {
    // Arrange
    var player = new PlayerCharacter();
    var animationOutputs = new List<PlayerCharacter.Output.AnimationTriggered>();

    player.Bind()
      .Handle<PlayerCharacter.Output.AnimationTriggered>((in PlayerCharacter.Output.AnimationTriggered output) => {
        animationOutputs.Add(output);
      });

    // Act
    player.Start(); // Idle animation
    player.Input(new PlayerCharacter.Input.JumpInput()); // Jump animation
    player.Input(new PlayerCharacter.Input.LandInput()); // Back to Idle animation

    // Assert
    animationOutputs.Count.ShouldBeGreaterThanOrEqualTo(3);
  }

  #endregion

  #region Complex Scenario Tests

  [Fact]
  public void CompleteGameplayScenario_WalkJumpAttackTakeDamage() {
    // This test demonstrates a realistic gameplay scenario

    // Arrange
    var player = new PlayerCharacter();
    var outputs = new List<object>();

    player.Bind()
      .Handle<PlayerCharacter.Output.MovementStateChanged>((in PlayerCharacter.Output.MovementStateChanged output) => outputs.Add(output))
      .Handle<PlayerCharacter.Output.JumpTriggered>((in PlayerCharacter.Output.JumpTriggered output) => outputs.Add(output))
      .Handle<PlayerCharacter.Output.AttackPerformed>((in PlayerCharacter.Output.AttackPerformed output) => outputs.Add(output))
      .Handle<PlayerCharacter.Output.DamageTaken>((in PlayerCharacter.Output.DamageTaken output) => outputs.Add(output));

    // Act - Simulate gameplay
    player.Start();
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();

    // Walk
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));
    player.Value.ShouldBeOfType<PlayerCharacter.State.Moving.Walking>();

    // Jump
    player.Input(new PlayerCharacter.Input.JumpInput());
    player.Value.ShouldBeOfType<PlayerCharacter.State.InAir.Jumping>();

    // Air attack
    player.Input(new PlayerCharacter.Input.AttackInput(false));
    player.Value.ShouldBeOfType<PlayerCharacter.State.Attacking.LightAttack>();

    // Complete attack
    player.Input(new PlayerCharacter.Input.AttackCompletedInput());
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();

    // Take damage
    player.Input(new PlayerCharacter.Input.TakeDamageInput(30));
    player.Value.ShouldBeOfType<PlayerCharacter.State.Idle>();

    // Assert - Verify all outputs were emitted
    outputs.Count.ShouldBeGreaterThan(0);
    player.Get<PlayerCharacter.Data>().Health.ShouldBe(70);
  }

  #endregion
}
