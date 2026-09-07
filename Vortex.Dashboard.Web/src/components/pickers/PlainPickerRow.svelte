<script lang="ts">
  import type { PickerRow } from '../../lib/pickers/directories';
  // The row every plain directory shares: a name, an id, and whatever the server thought worth
  // saying about it. Fourteen kinds render through this one file rather than fourteen branches.
  type Props = { row: PickerRow; onchoose: (row: PickerRow) => void };

  let { row, onchoose }: Props = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  <span class="pick-icon" aria-hidden="true"></span>
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>#{row.id}{row.description ? ` - ${row.description}` : ''}</small>
  </span>
</button>
