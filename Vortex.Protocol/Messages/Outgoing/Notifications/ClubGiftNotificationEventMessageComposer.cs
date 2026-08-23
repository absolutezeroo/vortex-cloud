using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record ClubGiftNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int GiftsAvailable { get; init; }
}
