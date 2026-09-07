// Row layouts, by the name a directory asks for. Four, not seventeen: most directories differ only
// in where their rows come from, and a shared layout is why adding one is a single line.
import FurniturePickerRow from './FurniturePickerRow.svelte';
import PlainPickerRow from './PlainPickerRow.svelte';
import PlayerPickerRow from './PlayerPickerRow.svelte';
import RoomPickerRow from './RoomPickerRow.svelte';

export const PICKER_ROWS = {
  furniture: FurniturePickerRow,
  room: RoomPickerRow,
  player: PlayerPickerRow,
  plain: PlainPickerRow,
};
