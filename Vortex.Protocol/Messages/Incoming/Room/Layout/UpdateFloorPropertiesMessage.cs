using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Layout;

/// <summary>
/// Saving the floor-plan editor.
///
/// The body is variable length, which is the trap: the client's composer sends the model string
/// alone when every other argument is -1, six fields when the wall height is left at -1, and seven
/// otherwise. A parser that always reads seven ints throws on the two shorter forms.
/// </summary>
public record UpdateFloorPropertiesMessage : IMessageEvent
{
    /// <summary>The floor plan itself, rows separated by carriage returns. Base-33 heights, with
    /// <c>x</c> for a hole.</summary>
    public required string Model { get; init; }

    /// <summary>Door position and facing, or -1 apiece when the short form was sent.</summary>
    public required int DoorX { get; init; }

    public required int DoorY { get; init; }

    public required int DoorRotation { get; init; }

    public required int WallThickness { get; init; }

    public required int FloorThickness { get; init; }

    /// <summary>-1 means "work it out from the plan" — the editor sends a fixed height only when
    /// its wall-height setting is ticked.</summary>
    public required int WallHeight { get; init; }
}
