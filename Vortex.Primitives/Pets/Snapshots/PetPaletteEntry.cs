namespace Vortex.Primitives.Pets.Snapshots;

public sealed record PetPaletteEntry
{
    public required int PetType { get; init; }
    public required int BreedIndex { get; init; }
    public required int Color { get; init; }
    public required bool Sellable { get; init; }
    public required bool Rare { get; init; }
}
