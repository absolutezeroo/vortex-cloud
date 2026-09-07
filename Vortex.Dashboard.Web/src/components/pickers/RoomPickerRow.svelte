<script lang="ts">
  import { t } from '../../lib/i18n';

  /** @type {{ row: any, onchoose: (row: any) => void }} */
  let { row, onchoose } = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  <span class="pick-icon" aria-hidden="true"></span>
  <span class="pick-dot" class:on={row.usersNow > 0} aria-hidden="true"></span>
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>
      #{row.id}{row.ownerName ? ` - ${row.ownerName}` : ''} - {$t('pickerModal.roomOccupancy', {
        users: row.usersNow,
        max: row.playersMax,
      })}
    </small>
  </span>
</button>
