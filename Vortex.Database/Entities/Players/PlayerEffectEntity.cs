using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>One owned avatar-effect instance. Multiple rows may share the same <see cref="EffectId"/>
/// (stacking): the client groups them into a single inventory entry whose "inactive count" is the number
/// of not-yet-activated copies. <see cref="ActivatedAt"/> null means the copy is sitting inactive in the
/// inventory; a non-null value starts its countdown. <see cref="TotalDuration"/> 0 means permanent.
/// <see cref="IsSelected"/> marks the single instance currently worn on the avatar.</summary>
[Table("player_effects")]
[Index(nameof(PlayerEntityId))]
public class PlayerEffectEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("effect_id")]
    public required int EffectId { get; set; }

    [Column("sub_type")]
    [DefaultValue(0)]
    public int SubType { get; set; }

    [Column("total_duration")]
    [DefaultValue(0)]
    public int TotalDuration { get; set; }

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [Column("is_selected")]
    [DefaultValue(false)]
    public bool IsSelected { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
