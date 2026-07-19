using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record CallForHelpPendingCallsDeletedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
