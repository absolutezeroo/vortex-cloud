namespace Vortex.Primitives.Moderation;

public readonly record struct CfhTopicSnapshot(
    int Id,
    int CategoryId,
    string Name,
    string? Consequence,
    int? DefaultSanctionPresetId
);
