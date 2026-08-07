using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

/// <summary>
/// One of a moodlight's three stored colour settings.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomDimmerPresetSnapshot
{
    /// <summary>1-based slot, and what the client sends back to overwrite it.</summary>
    [Id(0)]
    public required int Id { get; init; }

    /// <summary>1 tints the whole room, 2 only the background.</summary>
    [Id(1)]
    public required int EffectId { get; init; }

    /// <summary><c>#RRGGBB</c>. The client parses it with <c>parseInt(substr(1), 16)</c>, so the
    /// leading hash is required and the six digits are not optional.</summary>
    [Id(2)]
    public required string ColorHex { get; init; }

    /// <summary>Alpha, 0-255, shown as a brightness slider.</summary>
    [Id(3)]
    public required int Brightness { get; init; }
}
