using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record ModeratorUserInfoEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
