using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record ClubGiftNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int GiftsAvailable { get; init; }
}
