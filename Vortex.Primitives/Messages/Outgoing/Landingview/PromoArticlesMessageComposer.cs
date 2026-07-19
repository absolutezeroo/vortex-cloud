using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Landingview;

[GenerateSerializer, Immutable]
public sealed record PromoArticlesMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
