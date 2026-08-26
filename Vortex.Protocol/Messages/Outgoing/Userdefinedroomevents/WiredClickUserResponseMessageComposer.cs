using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

/// <summary>
/// The answer to one <c>WiredClickUser</c>: may the clicker's context menu open?
/// </summary>
/// <remarks>
/// The client has already suppressed the menu by the time it asks, and re-opens it only on this
/// reply. Never answering leaves the info stand permanently without its buttons, so this is sent for
/// every click — including the ones where the answer is "yes, open it as usual".
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredClickUserResponseMessageComposer : IComposer
{
    /// <summary>
    /// The object id the client sent, echoed back. <c>AvatarInfoWidget.setupMenuView</c> ignores a
    /// reply whose index is not the click it is still waiting on.
    /// </summary>
    [Id(0)]
    public required int Index { get; init; }

    [Id(1)]
    public required bool OpenMenu { get; init; }
}
