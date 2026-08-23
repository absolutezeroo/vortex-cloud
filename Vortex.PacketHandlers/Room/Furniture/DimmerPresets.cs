using Vortex.Protocol.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// All three dimmer handlers answer with the same packet — the dialog re-reads the whole state
/// after a save or a toggle, not just the part that changed.
/// </summary>
internal static class DimmerPresets
{
    public static RoomDimmerPresetsMessageComposer Compose(RoomDimmerStateSnapshot state) =>
        new()
        {
            Presets = state.Presets,
            SelectedPresetId = state.SelectedPresetId,
            IsOn = state.IsOn,
            ItemId = state.ItemId,
        };
}
