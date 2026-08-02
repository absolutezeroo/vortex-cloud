using System;

namespace Vortex.Primitives.Rooms.Object.Logic;

/// <summary>
/// Registers a logic class under a client logic name. Repeatable, because several client names can
/// share one server behaviour — the single-click reward boxes differ only in the dialog the client
/// puts on them, and five identical classes would only invite them to drift apart.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RoomObjectLogicAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
