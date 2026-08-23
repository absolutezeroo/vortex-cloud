using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

/// <summary>
/// Why a wired reward did or did not pay out (header 2997).
///
/// Shape from WIN63's parser
/// (com/sulake/habbo/communication/messages/parser/userdefinedroomevents/_SafeCls_3242.as): a
/// single int.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record WiredRewardResultMessageComposer : IComposer
{
    [Id(0)]
    public required int Reason { get; init; }
}
