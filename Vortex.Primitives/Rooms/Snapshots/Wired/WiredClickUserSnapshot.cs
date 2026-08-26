using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// What the room's click-user wired boxes mean for the client's own click handling.
/// </summary>
/// <remarks>
/// Both answers come from the same look at the same boxes, so they are read together rather than
/// through two calls that could disagree if a box were picked up between them.
/// </remarks>
[GenerateSerializer, Immutable]
public readonly record struct WiredClickUserSnapshot
{
    /// <summary>True when at least one <c>wf_trg_click_user</c> box is in the room.</summary>
    [Id(0)]
    public bool Present { get; init; }

    /// <summary>
    /// True when a present box asks for the clicker's context menu to stay shut. With several boxes
    /// one asking to block is enough: a menu half-suppressed is not a state the client has.
    /// </summary>
    [Id(1)]
    public bool BlocksMenu { get; init; }

    /// <summary>What a room with no click-user box looks like: nothing enabled, nothing blocked.</summary>
    public static WiredClickUserSnapshot None => new() { Present = false, BlocksMenu = false };
}
