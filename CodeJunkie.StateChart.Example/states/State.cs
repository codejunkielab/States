namespace CodeJunkie.StateChart.Example;

public partial class VendingMachine {
  public abstract partial record State : StateLogic<State>;
}
