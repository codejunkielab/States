namespace CodeJunkie.StateChart;

using System;

/// <summary>
/// Attribute to mark a class as a test state.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public partial class TestStateAttribute : Attribute;
