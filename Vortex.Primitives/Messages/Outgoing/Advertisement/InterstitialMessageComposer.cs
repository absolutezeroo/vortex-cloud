using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Advertisement;

/// <summary>
/// Whether the client may show an interstitial ad right now (header 3898).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1719/_SafeCls_1718.as): a single boolean.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record InterstitialMessageComposer : IComposer
{
    [Id(0)]
    public required bool CanShowInterstitial { get; init; }
}
