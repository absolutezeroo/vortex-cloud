using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Poll;

[GenerateSerializer, Immutable]
public sealed record QuestionFinishedEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
