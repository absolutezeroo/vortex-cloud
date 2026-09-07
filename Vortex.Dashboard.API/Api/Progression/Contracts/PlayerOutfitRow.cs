namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One saved wardrobe outfit.</summary>
public sealed record PlayerOutfitRow(int Id, int SlotId, string Figure, string Gender);
