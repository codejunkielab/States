namespace System.Runtime.CompilerServices;

using System;

/// <summary>
/// Indicates that a class or struct is an interpolated string handler.
/// </summary>
[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct,
  AllowMultiple = false,
  Inherited = false)]
public sealed class InterpolatedStringHandlerAttribute : Attribute;
