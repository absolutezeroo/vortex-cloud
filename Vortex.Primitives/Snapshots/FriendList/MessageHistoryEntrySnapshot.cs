using Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Snapshots.FriendList;

[GenerateSerializer, Immutable]
public record MessageHistoryEntrySnapshot
{
    [Id(0)]
    public required PlayerId SenderId { get; init; }

    [Id(1)]
    public required string SenderName { get; init; } = string.Empty;

    [Id(2)]
    public required string SenderFigure { get; init; } = string.Empty;

    [Id(3)]
    public required string Message { get; init; } = string.Empty;

    [Id(4)]
    public required int SecondsSinceSent { get; init; }

    [Id(5)]
    public required string MessageId { get; init; }

    /// <summary>
    /// The Habbicon this entry is, or 0 for a text entry. <see cref="Message"/> is not read when
    /// this is set — the client's console body is one or the other.
    /// </summary>
    [Id(6)]
    public int HabbiconId { get; init; }
}
