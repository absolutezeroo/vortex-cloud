<script lang="ts">
  import type { PickerRow } from '../../lib/pickers/directories';
  import { t } from '../../lib/i18n';

  type Props = { row: PickerRow; onchoose: (row: PickerRow) => void };

  let { row, onchoose }: Props = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  <span class="pick-icon" aria-hidden="true"></span>
  <span class="pick-dot" class:on={(row.usersNow ?? 0) > 0} aria-hidden="true"></span>
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>
      #{row.id}{row.ownerName ? ` - ${row.ownerName}` : ''} - {$t('pickerModal.roomOccupancy', {
        users: row.usersNow ?? 0,
        max: row.playersMax ?? 0,
      })}
    </small>
  </span>
</button>
