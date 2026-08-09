using Orleans;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Primitives.Orleans.Snapshots.Navigator;

/// <summary>
/// One row of the official/public rooms view. Which of <see cref="Tag"/>, <see cref="Room"/> and
/// <see cref="IsOpen"/> the client reads is decided by <see cref="Type"/> — the wire carries exactly
/// one of them, never all three.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record OfficialRoomEntrySnapshot
{
    [Id(0)]
    public required int Index { get; init; }

    [Id(1)]
    public required string PopupCaption { get; init; }

    [Id(2)]
    public required string PopupDescription { get; init; }

    [Id(3)]
    public required bool ShowDetails { get; init; }

    /// <summary>Caption drawn over the entry's picture.</summary>
    [Id(4)]
    public required string PictureText { get; init; }

    /// <summary>Asset name the client loads the entry's picture from; empty means no picture.</summary>
    [Id(5)]
    public required string PictureRef { get; init; }

    [Id(6)]
    public required int FolderId { get; init; }

    [Id(7)]
    public required int UserCount { get; init; }

    [Id(8)]
    public required OfficialRoomEntryType Type { get; init; }

    /// <summary>Only read when <see cref="Type"/> is <see cref="OfficialRoomEntryType.Tag"/>.</summary>
    [Id(9)]
    public string Tag { get; init; } = string.Empty;

    /// <summary>Only read when <see cref="Type"/> is <see cref="OfficialRoomEntryType.Room"/>.</summary>
    [Id(10)]
    public RoomInfoSnapshot? Room { get; init; }

    /// <summary>Only read for the remaining types (a folder node's expanded/collapsed state).</summary>
    [Id(11)]
    public bool IsOpen { get; init; }
}
