using System;

namespace Vortex.Primitives.Rooms.Object.Logic;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RoomObjectLogicAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
