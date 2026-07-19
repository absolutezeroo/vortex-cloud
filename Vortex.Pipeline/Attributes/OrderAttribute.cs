using System;

namespace Vortex.Pipeline.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OrderAttribute(int value) : Attribute
{
    public int Value { get; } = value;
}
