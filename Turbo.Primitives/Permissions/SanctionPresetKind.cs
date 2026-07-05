namespace Turbo.Primitives.Permissions;

/// <summary>Which mod-tool action a <c>SanctionPresetEntity</c> row applies to. The client's
/// sanctionTypeId/lockDurationTypeId is an index into its own local dropdown per kind — these
/// numberings are independent, hence the (Kind, PresetIndex) composite key on the entity.</summary>
public enum SanctionPresetKind
{
    Ban = 0,
    TradingLock = 1,
}
