namespace CodeJunkie.StateChart.Example;

using System;
using CodeJunkie.Log;
using CodeJunkie.Metadata;
using CodeJunkie.StateChart;

[Meta, StateChart(typeof(State), Diagram = true, DiagramFormats = DiagramFormat.All)]
public partial class Timer : StateChart<Timer.State> {
	public static class Input {
		public readonly record struct PowerButtonPressed;
	}
	public abstract record State : StateLogic<State> {
		public record PoweredOff : State, IGet<Input.PowerButtonPressed> {
			private readonly Log _log = LogManager.GetLogger<Timer.State.PoweredOff>();

			public PoweredOff() {
				this.OnEnter(() => { _log.Info("Timer powered off."); });
				this.OnExit(() => { _log.Info("Timer powered on."); });
			}

			public Transition On(in Input.PowerButtonPressed input) {
				_log.Info("Timer powered on.");
				return To<PoweredOn>();
			}
		}

		public record PoweredOn : State, IGet<Input.PowerButtonPressed> {
			private readonly Log _log = LogManager.GetLogger<Timer.State.PoweredOn>();

			public PoweredOn() {
				this.OnEnter(() => { _log.Info("Timer powered on."); });
				this.OnExit(() => { _log.Info("Timer powered off."); });
			}

			public Transition On(in Input.PowerButtonPressed input) {
				_log.Info("Timer powered off.");
				return To<PoweredOff>();
			}
		}
	}

  public override Transition GetInitialState() => To<State.PoweredOff>();

	public Timer() {
		var log = LogManager.GetLogger<Timer>();
		log.Info("Timer initialized.");
	}
}

sealed class Program {
	static void Main(string[] args) {
		var log = LogManager.GetLogger<Program>();
		log.Info("Hello, World!");

		var timer = new Timer();
		timer.Input(new Timer.Input.PowerButtonPressed());
	}
}
