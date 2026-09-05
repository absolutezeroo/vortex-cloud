using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record NewConsoleMessageMessageComposer : IComposer
{
    [Id(0)]
    public required int ChatId { get; init; }

    [Id(1)]
    public required string Message { get; init; }

    /// <summary>
    /// The Habbicon this message is, or 0 for a text message. The client reads the body as a tagged
    /// union and takes one or the other, never both, so setting this makes
    /// <see cref="Message"/> unread rather than combining with it.
    /// </summary>
    [Id(8)]
    public int HabbiconId { get; init; }

    [Id(2)]
    public required int SecondsSinceSent { get; init; }

    [Id(3)]
    public required string MessageId { get; init; }

    [Id(4)]
    public required int ConfirmationId { get; init; }

    [Id(5)]
    public required int SenderId { get; init; }

    [Id(6)]
    public required string SenderName { get; init; }

    [Id(7)]
    public required string SenderFigure { get; init; }
}
