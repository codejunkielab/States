namespace CodeJunkie.StateChart;

using System;
using System.Collections.Concurrent;
#if WITH_SERIALIZATION
using CodeJunkie.Serialization;
#else
using CodeJunkie.Collections;
#endif

/// <summary>
/// <inheritdoc cref="StateChartBase" path="/summary" />
/// </summary>
public interface IStateChartBase {
  /// <summary>
  /// Current state of the states, if any. Reading this will not start
  /// the states and can return null.
  /// </summary>
  object? ValueAsObject { get; }

  /// <summary>State that will be restored when started, if any.</summary>
  object? RestoredState { get; }

  /// <summary>Internal blackboard of the states.</summary>
#if WITH_SERIALIZATION
  SerializableBlackboard Blackboard { get; }
#else
  Blackboard Blackboard { get; }
#endif

  /// <summary>
  /// Restore the state from a given object. Only works if the current
  /// state has not been initialized and the <paramref name="state"/> is
  /// of the correct type.
  /// </summary>
  /// <param name="state">State to restore.</param>
  void RestoreState(object state);
}

/// <summary>
/// Common, non-generic base type for all statess. This exists to allow
/// all statess in a codebase to be identified by inspecting the derived
/// types computed from the generated type registry that the statess
/// generator produces.
/// </summary>
public abstract class StateChartBase : IStateChartBase {
  /// <inheritdoc />
  public abstract object? ValueAsObject { get; }

  /// <inheritdoc />
  public object? RestoredState { get; set; }

  /// <inheritdoc />
#if WITH_SERIALIZATION
  public SerializableBlackboard Blackboard { get; } = new();
#else
  public Blackboard Blackboard { get; } = new();
#endif

  /// <inheritdoc />
  public abstract void RestoreState(object state);

  /// <summary>
  /// Used by the statess serializer to see if a given states state
  /// has diverged from an unaltered copy of the state that's stored here —
  /// one reference state for every type (not instance) of a states state.
  /// </summary>
  internal static ConcurrentDictionary<Type, object> ReferenceStates { get; } =
    new();
}
