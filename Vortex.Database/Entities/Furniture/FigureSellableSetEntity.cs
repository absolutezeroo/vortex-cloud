using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Furniture;

/// <summary>
/// A figure set that has to be owned before it can be worn.
/// </summary>
/// <remarks>
/// <para>
/// <c>figuredata.xml</c> marks these <c>sellable="1"</c>, and the client's avatar editor already
/// greys out the ones the player lacks — but that is the client's own check, made against a list the
/// client holds. Nothing stopped a figure arriving with any set id in it.
/// </para>
/// <para>
/// This is that list, server-side, so a saved look can be judged by the same rule. A set that is not
/// here is free to wear, which is the default for the majority of them.
/// </para>
/// </remarks>
[Table("figure_sellable_sets")]
[Index(nameof(FigureSetId), IsUnique = true)]
public class FigureSellableSetEntity : VortexEntity
{
    [Column("figure_set_id")]
    public required int FigureSetId { get; set; }
}
