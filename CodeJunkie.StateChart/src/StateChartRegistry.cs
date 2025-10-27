namespace CodeJunkie.StateChart;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Provides a registry for tracking all active StateChart instances in the application.
/// Uses WeakReference to prevent memory leaks while allowing monitoring of state charts.
/// </summary>
public static class StateChartRegistry {
  private static readonly ConcurrentDictionary<Guid, WeakReference<IStateChartBase>> _instances = new();
  private static readonly ReaderWriterLockSlim _lock = new();
  private static int _cleanupCounter = 0;
  private const int CleanupInterval = 100; // Cleanup every 100 operations

  /// <summary>
  /// Gets or sets a value indicating whether StateChart monitoring is globally enabled.
  /// When disabled, no StateChart instances will be registered or tracked.
  /// </summary>
  public static bool IsMonitoringEnabled { get; set; } = true;

  /// <summary>
  /// Gets the number of active StateChart instances currently being tracked.
  /// </summary>
  public static int ActiveInstanceCount => CleanupAndCount();

  /// <summary>
  /// Registers a StateChart instance for monitoring.
  /// </summary>
  /// <param name="instance">The StateChart instance to register.</param>
  /// <param name="instanceId">The unique identifier for the instance.</param>
  internal static void Register(IStateChartBase instance, Guid instanceId) {
    if (!IsMonitoringEnabled || instance == null) return;

    _lock.EnterWriteLock();
    try {
      _instances[instanceId] = new WeakReference<IStateChartBase>(instance);
      
      // Periodic cleanup
      if (Interlocked.Increment(ref _cleanupCounter) % CleanupInterval == 0) {
        CleanupDeadReferences();
      }
    }
    finally {
      _lock.ExitWriteLock();
    }
  }

  /// <summary>
  /// Unregisters a StateChart instance from monitoring.
  /// </summary>
  /// <param name="instanceId">The unique identifier of the instance to unregister.</param>
  internal static void Unregister(Guid instanceId) {
    if (!IsMonitoringEnabled) return;

    _lock.EnterWriteLock();
    try {
      _instances.TryRemove(instanceId, out _);
    }
    finally {
      _lock.ExitWriteLock();
    }
  }

  /// <summary>
  /// Gets all active StateChart instances.
  /// </summary>
  /// <returns>A read-only list of all active StateChart instances.</returns>
  public static IReadOnlyList<IStateChartBase> GetAllActiveInstances() {
    if (!IsMonitoringEnabled) return Array.Empty<IStateChartBase>();

    var result = new List<IStateChartBase>();
    _lock.EnterReadLock();
    try {
      foreach (var kvp in _instances.ToArray()) {
        if (kvp.Value.TryGetTarget(out var instance)) {
          result.Add(instance);
        }
      }
    }
    finally {
      _lock.ExitReadLock();
    }
    return result.AsReadOnly();
  }

  /// <summary>
  /// Gets all active StateChart instances of a specific type.
  /// </summary>
  /// <typeparam name="T">The type of StateChart instances to retrieve.</typeparam>
  /// <returns>A read-only list of active StateChart instances of the specified type.</returns>
  public static IReadOnlyList<T> GetActiveInstances<T>() where T : IStateChartBase {
    return GetAllActiveInstances()
      .OfType<T>()
      .ToList()
      .AsReadOnly();
  }

  /// <summary>
  /// Gets all active StateChart instances that match the specified predicate.
  /// </summary>
  /// <param name="predicate">A function to test each StateChart instance.</param>
  /// <returns>A read-only list of StateChart instances that satisfy the condition.</returns>
  public static IReadOnlyList<IStateChartBase> GetActiveInstances(Func<IStateChartBase, bool> predicate) {
    if (predicate == null) throw new ArgumentNullException(nameof(predicate));
    
    return GetAllActiveInstances()
      .Where(predicate)
      .ToList()
      .AsReadOnly();
  }

  /// <summary>
  /// Gets StateChart instances grouped by their concrete type.
  /// </summary>
  /// <returns>A dictionary where keys are types and values are lists of instances.</returns>
  public static IReadOnlyDictionary<Type, IReadOnlyList<IStateChartBase>> GetActiveInstancesByType() {
    var result = new Dictionary<Type, IReadOnlyList<IStateChartBase>>();
    
    foreach (var group in GetAllActiveInstances().GroupBy(x => x.GetType())) {
      result[group.Key] = group.ToList().AsReadOnly();
    }
    
    return result;
  }

  /// <summary>
  /// Resets the registry by removing all tracked instances.
  /// This method is primarily intended for testing purposes.
  /// </summary>
  internal static void Reset() {
    _lock.EnterWriteLock();
    try {
      _instances.Clear();
      _cleanupCounter = 0;
    }
    finally {
      _lock.ExitWriteLock();
    }
  }

  /// <summary>
  /// Forces cleanup of dead WeakReference entries.
  /// This method is primarily intended for testing purposes.
  /// </summary>
  internal static void ForceCleanup() {
    _lock.EnterWriteLock();
    try {
      CleanupDeadReferences();
    }
    finally {
      _lock.ExitWriteLock();
    }
  }

  private static int CleanupAndCount() {
    _lock.EnterReadLock();
    try {
      int count = 0;
      foreach (var kvp in _instances) {
        if (kvp.Value.TryGetTarget(out _)) {
          count++;
        }
      }
      return count;
    }
    finally {
      _lock.ExitReadLock();
    }
  }

  private static void CleanupDeadReferences() {
    var deadKeys = new List<Guid>();
    
    foreach (var kvp in _instances) {
      if (!kvp.Value.TryGetTarget(out _)) {
        deadKeys.Add(kvp.Key);
      }
    }
    
    foreach (var key in deadKeys) {
      _instances.TryRemove(key, out _);
    }
  }
}