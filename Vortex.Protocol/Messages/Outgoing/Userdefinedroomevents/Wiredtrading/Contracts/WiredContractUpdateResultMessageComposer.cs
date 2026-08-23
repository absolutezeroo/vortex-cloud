using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;

/// <summary>
/// What became of a contract save.
/// </summary>
/// <remarks>
/// This is what closes the editor: the client shuts its frames on a success and shows
/// <c>wiredcontracts.error.&lt;failCode&gt;</c> otherwise. The contents reply redraws the window, it
/// does not dismiss it — so a save answered only with contents leaves the editor open on a contract
/// that was already stored.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredContractUpdateResultMessageComposer : IComposer
{
    [Id(0)]
    public required int ContractId { get; init; }

    [Id(1)]
    public required bool IsSuccess { get; init; }

    /// <summary>The tail of a localization key. Empty on success, since nothing reads it then.</summary>
    [Id(2)]
    public required string FailCode { get; init; }
}
