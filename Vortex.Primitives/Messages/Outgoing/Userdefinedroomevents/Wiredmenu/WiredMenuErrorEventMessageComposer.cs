using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

/// <summary>
/// A wired-menu operation failed (header 1230).
///
/// Shape from WIN63's parser
/// (com/sulake/habbo/communication/messages/parser/userdefinedroomevents/wiredmenu/_SafeCls_4262.as):
/// a <b>short</b>, not an int — the only one of the wired messages that narrows its error code.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record WiredMenuErrorEventMessageComposer : IComposer
{
    [Id(0)]
    public required short ErrorCode { get; init; }
}
