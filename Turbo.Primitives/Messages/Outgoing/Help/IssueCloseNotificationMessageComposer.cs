using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record IssueCloseNotificationMessageComposer : IComposer
{
    /// <summary>1 = useless, 2 = sanctioned/abusive, 3 = resolved — the client falls back to a
    /// canned "${help.cfh.closed.*}" string per this value when <see cref="MessageText"/> is empty.</summary>
    [Id(0)]
    public required int CloseReason { get; init; }

    [Id(1)]
    public required string MessageText { get; init; }
}
