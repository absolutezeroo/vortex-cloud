using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record RoomSettingsSaveErrorEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
