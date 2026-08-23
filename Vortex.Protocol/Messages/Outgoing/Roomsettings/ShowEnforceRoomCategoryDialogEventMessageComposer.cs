using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record ShowEnforceRoomCategoryDialogEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
