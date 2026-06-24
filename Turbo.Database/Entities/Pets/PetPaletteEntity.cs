using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Pets;

[Table("pet_palettes")]
[Index(nameof(PetType))]
[Index(nameof(PetType), nameof(BreedIndex), nameof(Rare), IsUnique = true)]
public class PetPaletteEntity : TurboEntity
{
    [Column("pet_type")]
    public required int PetType { get; set; }

    [Column("breed_index")]
    public required int BreedIndex { get; set; }

    [Column("color")]
    public required int Color { get; set; }

    [Column("sellable")]
    public required bool Sellable { get; set; }

    [Column("rare")]
    [DefaultValue(false)]
    public required bool Rare { get; set; }
}
