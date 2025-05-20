namespace CodeJunkie.StateChart;

using System;

public abstract partial class StateChart<TState> {
  /// <summary>State chart context provided to each states state.</summary>
  internal readonly struct DefaultContext : IContext {
    public StateChart<TState> Logic { get; }

    /// <summary>
    /// Creates a new states context for the given states.
    /// </summary>
    /// <param name="logic">State chart.</param>
    public DefaultContext(
      StateChart<TState> logic
    ) {
      Logic = logic;
    }

    /// <inheritdoc />
    public void Input<TInputType>(in TInputType input)
      where TInputType : struct => Logic.Input(input);

    /// <inheritdoc />
    public void Output<TOutputType>(in TOutputType output)
      where TOutputType : struct => Logic.OutputValue(output);

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : class =>
      Logic.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e) => Logic.AddError(e);

    /// <inheritdoc />
    public override bool Equals(object? obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Logic);
  }

  internal class ContextAdapter : IContext, IContextAdapter {
    public IContext? Context { get; private set; }

    public void Adapt(IContext context) => Context = context;
    public void Clear() => Context = null;
    public Action<Exception>? OnError =>
      Context is DefaultContext defaultContext
        ? defaultContext.Logic.AddError : null;

    /// <inheritdoc />
    public void Input<TInputType>(in TInputType input)
      where TInputType : struct {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot add input to a states with an uninitialized context."
        );
      }

      context.Input(input);
    }

    /// <inheritdoc />
    public void Output<TOutputType>(in TOutputType output)
    where TOutputType : struct {
      if (Context is not { } context) {
        throw new InvalidOperationException(
          "Cannot add output to a states with an uninitialized context."
        );
      }

      context.Output(in output);
    }

    /// <inheritdoc />
    public TDataType Get<TDataType>() where TDataType : class =>
      Context is not IContext context
        ? throw new InvalidOperationException(
          "Cannot get value from a states with an uninitialized context."
        )
        : context.Get<TDataType>();

    /// <inheritdoc />
    public void AddError(Exception e) {
      if (Context is not IContext context) {
        throw new InvalidOperationException(
          "Cannot add error to a states with an uninitialized context."
        );
      }

      context.AddError(e);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => true;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Context);
  }
}
