using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredSetUserPermanentVariableResultComposer : IComposer
{
    [Id(0)]
    public required bool Success { get; init; }
}
