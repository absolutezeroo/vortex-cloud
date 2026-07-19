using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Error;

[GenerateSerializer, Immutable]
public sealed record ErrorReportEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
