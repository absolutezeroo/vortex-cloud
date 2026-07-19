using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record NotificationDialogMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
