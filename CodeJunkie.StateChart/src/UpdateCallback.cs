namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Represents a callback function that is invoked during the update lifecycle of a state.
/// </summary>
internal readonly struct UpdateCallback {
  /// <summary>
  /// The function to be invoked during the update lifecycle.
  /// </summary>
  public Action<object?> Callback { get; }

  /// <summary>
  /// The type of the state that this callback is associated with.
  /// </summary>
  public Type Type { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="UpdateCallback"/> struct.
  /// </summary>
  /// <param name="callback">The function to be invoked during the update lifecycle.</param>
  /// <param name="type">The expected type of the object associated with this callback.</param>
  public UpdateCallback(Action<object?> callback, Type type) {
    Callback = callback;
    Type = type;
  }

  /// <summary>
  /// Determines whether the specified object is of the expected type or a subtype.
  /// If the object represents a type, it will be checked against the expected type.
  /// This method is compatible with reflection-free mode:
  /// <see href="https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/reflection-free-mode.md">Reflection-Free Mode</see>.
  /// </summary>
  /// <param name="obj">The object to check, which can be an instance or a type.</param>
  /// <returns>
  /// <c>true</c> if the specified object is of the expected type or a subtype; otherwise, <c>false</c>.
  /// </returns>
  public bool IsType(object? obj) =>
    obj is Type type
    ? Type.IsAssignableFrom(type)
    : obj is not null && Type.IsAssignableFrom(obj.GetType());
}
