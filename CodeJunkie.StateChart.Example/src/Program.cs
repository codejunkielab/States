namespace CodeJunkie.StateChart.Example;

using System;
using System.Collections.Generic;
using CodeJunkie.Log;
using CodeJunkie.StateChart;

sealed class Program {
  static void Main(string[] args) {
    var log = LogManager.GetLogger<Program>();
    log.Info("StateChart Example - PlayerCharacter Demo");
    log.Info("===========================================");

    // Create player character
    var player = new PlayerCharacter();

    // Track outputs for demonstration
    var outputs = new List<string>();

    // Bind to outputs to visualize what's happening
    player.Bind()
      .Handle<PlayerCharacter.Output.MovementStateChanged>((in PlayerCharacter.Output.MovementStateChanged output) => {
        outputs.Add($"Movement: {output.StateName}");
      })
      .Handle<PlayerCharacter.Output.JumpTriggered>((in PlayerCharacter.Output.JumpTriggered output) => {
        outputs.Add($"Jump with force: {output.JumpForce}");
      })
      .Handle<PlayerCharacter.Output.AttackPerformed>((in PlayerCharacter.Output.AttackPerformed output) => {
        outputs.Add($"Attack: {output.AttackType} (Damage: {output.Damage})");
      })
      .Handle<PlayerCharacter.Output.DamageTaken>((in PlayerCharacter.Output.DamageTaken output) => {
        outputs.Add($"Damage taken: {output.Damage} (Health: {output.RemainingHealth})");
      })
      .Handle<PlayerCharacter.Output.AnimationTriggered>((in PlayerCharacter.Output.AnimationTriggered output) => {
        outputs.Add($"Animation: {output.AnimationName}");
      })
      .Handle<PlayerCharacter.Output.CharacterDied>((in PlayerCharacter.Output.CharacterDied output) => {
        outputs.Add("Character died!");
      })
      .Handle<PlayerCharacter.Output.Landed>((in PlayerCharacter.Output.Landed output) => {
        outputs.Add("Landed on ground");
      });

    // Start the state chart
    log.Info("\n1. Starting player character (Idle state)");
    player.Start();
    PrintOutputs(outputs);

    // Demonstrate walking
    log.Info("\n2. Move input - Walking");
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, false, false));
    PrintOutputs(outputs);

    // Demonstrate running
    log.Info("\n3. Sprint input - Running");
    player.Input(new PlayerCharacter.Input.MoveInput(1.0f, 0f, true, false));
    PrintOutputs(outputs);

    // Demonstrate jumping
    log.Info("\n4. Jump input - In Air");
    player.Input(new PlayerCharacter.Input.JumpInput());
    PrintOutputs(outputs);

    // Demonstrate air attack
    log.Info("\n5. Attack input while in air - Air Attack");
    player.Input(new PlayerCharacter.Input.AttackInput(false));
    PrintOutputs(outputs);

    // Complete attack
    log.Info("\n6. Attack completed - Return to Idle");
    player.Input(new PlayerCharacter.Input.AttackCompletedInput());
    PrintOutputs(outputs);

    // Demonstrate taking damage
    log.Info("\n7. Take damage");
    player.Input(new PlayerCharacter.Input.TakeDamageInput(30));
    PrintOutputs(outputs);

    var data = player.Get<PlayerCharacter.Data>();
    log.Info($"   Current Health: {data.Health}/{data.MaxHealth}");

    // Demonstrate heavy attack
    log.Info("\n8. Heavy attack");
    player.Input(new PlayerCharacter.Input.AttackInput(true));
    PrintOutputs(outputs);
    player.Input(new PlayerCharacter.Input.AttackCompletedInput());

    log.Info("\nDemo completed!");
    log.Info($"Final State: {player.Value.GetType().Name}");
    log.Info($"Final Health: {data.Health}/{data.MaxHealth}");
    log.Info($"Final Stamina: {data.Stamina}/{data.MaxStamina}");
  }

  static void PrintOutputs(List<string> outputs) {
    if (outputs.Count > 0) {
      Console.WriteLine("   Outputs:");
      foreach (var output in outputs) {
        Console.WriteLine($"   - {output}");
      }
      outputs.Clear();
    }
  }
}
