using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Room;

/// <summary>
/// One value held by a shared wired variable, readable from a room other than the one the box
/// stands in.
/// </summary>
/// <remarks>
/// Shared is the one availability a variable box offers that had nowhere to put its values. A
/// room-active variable lives in the owning room's memory and a persistent one on its own furni
/// row's extra data — both of which the referencing room cannot see, and the first of which does not
/// survive the owning room unloading. So a reference variable could name a variable and never read
/// one.
/// <para>
/// Its own table rather than the owning furni's extra data, because two rooms write here. The furni
/// row is rewritten wholesale by its room's save pass, so a second writer would lose whichever of
/// the two saved first, silently and at unload time.
/// </para>
/// </remarks>
[Table("wired_shared_variables")]
[Index(nameof(VariableId), nameof(StorageKey), IsUnique = true)]
public class WiredSharedVariableEntity : VortexEntity
{
    /// <summary>The variable, which is the id built from the box's furni id. Stored as a string
    /// because it is a 64-bit unsigned value and MySQL's signed BIGINT would not hold the top of the
    /// range.</summary>
    /// <remarks>Both halves of the unique index are capped: uncapped they become varchar(512) in
    /// utf8mb4, and two of those is 4096 bytes against MySQL's 3072-byte ceiling for a composite
    /// index — so the index that makes a value unrepeatable simply would not be created.</remarks>
    [Column("variable_id")]
    [MaxLength(ContentIdLength)]
    public required string VariableId { get; set; }

    /// <summary>Which target this value belongs to — the same storage key the in-memory stores use,
    /// so a value means the same thing on both sides.</summary>
    [Column("storage_key")]
    [MaxLength(ContentIdLength)]
    public required string StorageKey { get; set; }

    [Column("value")]
    public required int Value { get; set; }

    /// <summary>Unix milliseconds, to answer the "variable age" condition across rooms the same way
    /// the in-memory store answers it inside one.</summary>
    [Column("created_at_ms")]
    public required long CreatedAtMs { get; set; }

    [Column("updated_at_ms")]
    public required long UpdatedAtMs { get; set; }
}
