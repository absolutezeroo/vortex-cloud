using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Habbicons;

namespace Vortex.Database.Entities.Habbicons;

/// <summary>
/// One player's relationship to one Habbicon. The presence of a row is ownership; there is no
/// "owned" boolean to disagree with it.
/// </summary>
/// <remarks>
/// <para>
/// The unique index on (player, habbicon) is the last line of defence for grant idempotency. The
/// grain checks first and Orleans keeps a player's turns serial, so the check normally settles it —
/// but "normally" is not the same as "cannot", and the thing that must never happen is two rows.
/// </para>
/// <para>
/// <see cref="LastUsedAt"/> is what "recently used" is built from. A separate recents list would be
/// a second store of the same fact, and it would drift the first time a Habbicon was revoked.
/// </para>
/// </remarks>
[Table("player_habbicons")]
[Index(nameof(PlayerEntityId), nameof(HabbiconEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId), nameof(LastUsedAt))]
[Index(nameof(HabbiconEntityId))]
public class PlayerHabbiconEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("habbicon_id")]
    public required int HabbiconEntityId { get; set; }

    /// <summary>
    /// <see cref="HabbiconState.Claimable"/>, <see cref="HabbiconState.Owned"/> or
    /// <see cref="HabbiconState.Favourite"/>. Never <see cref="HabbiconState.NotOwned"/> — that is
    /// what having no row means.
    /// </summary>
    [Column("state")]
    public required HabbiconState State { get; set; }

    [Column("source")]
    [DefaultValue(HabbiconSource.Unknown)]
    public HabbiconSource Source { get; set; }

    [Column("acquired_at")]
    public required DateTime AcquiredAt { get; set; }

    /// <summary>Last time the player used it, anywhere. Null until they do.</summary>
    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(HabbiconEntityId))]
    public HabbiconEntity? Habbicon { get; set; }
}
