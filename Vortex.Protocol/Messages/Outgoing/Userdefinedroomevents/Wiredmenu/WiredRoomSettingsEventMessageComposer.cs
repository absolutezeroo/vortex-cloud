using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredRoomSettingsEventMessageComposer : IComposer
{
    [Id(0)]
    public required int ModifyPermissionMask { get; init; }

    [Id(1)]
    public required int ReadPermissionMask { get; init; }

    [Id(2)]
    public required string Timezone { get; init; }
}
