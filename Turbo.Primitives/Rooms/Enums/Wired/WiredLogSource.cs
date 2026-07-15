namespace Turbo.Primitives.Rooms.Enums.Wired;

/// <summary>Best-effort values, not verified against a real WIN63 client capture — see the plan's
/// residual-risk notes for the wired room-logs feature.</summary>
public enum WiredLogSource : byte
{
    System = 0,
    Trigger = 1,
    Condition = 2,
    Action = 3,
}
