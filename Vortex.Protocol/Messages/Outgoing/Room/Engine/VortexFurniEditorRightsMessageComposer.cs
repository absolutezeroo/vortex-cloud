using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>
/// Tells the client whether this account holds <c>room.furni.edit</c>, so it knows whether to offer
/// the furni-editor button at all. Sent once, during the handshake. Vortex-specific: no AS3 or Habbo
/// equivalent.
///
/// This is a UI hint only — it decides whether a button is drawn, never whether an edit is allowed.
/// Both furni-editor handlers re-resolve the capability on every request.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record VortexFurniEditorRightsMessageComposer : IComposer
{
    [Id(0)]
    public required bool CanEdit { get; init; }
}
