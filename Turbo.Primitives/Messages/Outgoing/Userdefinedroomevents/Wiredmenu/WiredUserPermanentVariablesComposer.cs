using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredUserPermanentVariablesComposer : IComposer
{
    [Id(0)]
    public required WiredPermanentVariablesSnapshot Snapshot { get; init; }
}
