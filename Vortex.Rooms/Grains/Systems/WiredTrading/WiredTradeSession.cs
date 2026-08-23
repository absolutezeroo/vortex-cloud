using System;
using System.Collections.Generic;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains.Systems.WiredTrading;

/// <summary>
/// One player's open trade screen, whichever of the two things it is.
/// </summary>
/// <remarks>
/// A plain deposit and a contract offer are the same session because they are the same screen: the
/// client drives both with <c>WiredTradeUpdateItems</c>, <c>WiredTradeAccept</c> and
/// <c>WiredTradeClose</c>, and nothing in those three says which kind it is.
/// <para>
/// <see cref="Terms" /> is what tells them apart, and it is the same test the accept path already
/// made: null asks for nothing and gives nothing back, anything else has a price. The offer fields
/// below it only mean something when it is set.
/// </para>
/// <para>
/// This was two dictionaries, and a session was only legal when both agreed. Nothing enforced
/// that, and one of the three cleanup paths forgot the second: a player who walked out mid-offer
/// left the offer pending for the life of the room, and the stack listening for
/// <c>wf_trg_transaction_failed</c> never heard about them. One record cannot half-exist.
/// </para>
/// </remarks>
internal sealed record WiredTradeSession(int ChestId, HashSet<int> ItemIds)
{
    /// <summary>What the contract asks for and gives back, or null for a plain deposit.</summary>
    public TradeContract? Terms { get; init; }

    /// <summary>Whether this screen is a contract offer rather than a donation.</summary>
    public bool IsOffer => Terms is not null;

    /// <summary>The contract furni the offer came from; 0 when there is no offer.</summary>
    public int ContractId { get; init; }

    /// <summary>The client's transaction mode: 0 normal, 1 multiplier, 2 auto-multiplier.</summary>
    public int Mode { get; init; }

    /// <summary>How many times over the contract is being taken. Never below one.</summary>
    public int Multiplier { get; init; } = 1;

    /// <summary>
    /// When the offering box stops waiting, or null when it set no timeout.
    /// </summary>
    /// <remarks>
    /// Checked whenever the session is touched rather than on the room clock: a timeout only matters
    /// at the moment someone asks about it, and a tick that exists to notice nothing most of the
    /// time is a tick not worth paying for.
    /// </remarks>
    public DateTime? ExpiresAt { get; init; }
}
