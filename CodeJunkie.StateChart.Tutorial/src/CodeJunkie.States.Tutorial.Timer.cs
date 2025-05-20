namespace CodeJunkie.StateChart.Tutorial;

using System;
using CodeJunkie.Log;
using CodeJunkie.Metadata;
using CodeJunkie.StateChart;

[Meta, StateChart(typeof(State), Diagram = true)]
public partial class TimerStates : StateChart<TimerStates.State> {
	public static class Input {
		public readonly record struct PowerButtonPressed;
	}

	public static class Output {
		public readonly record struct PoweredOff;
		public readonly record struct PoweredOn;
	}

	public abstract record State : StateLogic<State> {
		public record PoweredOff : State, IGet<Input.PowerButtonPressed> {
			private readonly Log _log = LogManager.GetLogger<TimerStates.State.PoweredOff>();

			public PoweredOff() {
				this.OnEnter(() => {
						_log.Info("TimerStates powered off.");
						Output(new Output.PoweredOff());
						});
				this.OnExit(() => { _log.Info("TimerStates powered on."); });
			}

			public Transition On(in Input.PowerButtonPressed input) {
				_log.Info("TimerStates powered on.");
				return To<PoweredOn>();
			}
		}

		public record PoweredOn : State, IGet<Input.PowerButtonPressed> {
			private readonly Log _log = LogManager.GetLogger<TimerStates.State.PoweredOn>();

			public PoweredOn() {
				this.OnEnter(() => {
						_log.Info("TimerStates powered on.");
						Output(new Output.PoweredOn());
						});
				this.OnExit(() => { _log.Info("TimerStates powered off."); });
			}

			public Transition On(in Input.PowerButtonPressed input) {
				_log.Info("TimerStates powered off.");
				return To<PoweredOff>();
			}
		}
	}

  public override Transition GetInitialState() => To<State.PoweredOff>();

	public TimerStates() {
		var log = LogManager.GetLogger<TimerStates>();
		log.Info("TimerStates initialized.");
	}
}

public sealed class TimerLauncher : IDisposable {
	private readonly TimerStates _timer;
  private TimerStates.IBinding _timerBinding { get; set; } = default!;

	public TimerLauncher() {
		_timer = new TimerStates();

    _timerBinding = _timer.Bind();
    _timerBinding
      .Handle((in TimerStates.Output.PoweredOff _) => {
					var log = LogManager.GetLogger<TimerLauncher>();
					log.Info("TimerStates powered off.");
      })
      .Handle((in TimerStates.Output.PoweredOn _) => {
					var log = LogManager.GetLogger<TimerLauncher>();
					log.Info("TimerStates powered on.");
      });

		_timer.Start();

		_timer.Input(new TimerStates.Input.PowerButtonPressed());
	}

	void IDisposable.Dispose() {
		_timerBinding?.Dispose();
		_timerBinding = default!;
	}
}
