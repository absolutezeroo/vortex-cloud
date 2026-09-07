<script lang="ts">
  import type { PickerRow } from '../../lib/pickers/directories';
  import { t } from '../../lib/i18n';

  /** What a furniture row draws. The rest of the directory's payload rides along. */
  type Props = { row: PickerRow; onchoose: (row: PickerRow) => void };

  let { row, onchoose }: Props = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  {#if row.iconUrl}
    <img class="pick-icon" src={row.iconUrl} alt="" width="38" height="38" loading="lazy" />
  {:else}
    <span class="pick-icon" aria-hidden="true">{row.spriteId}</span>
  {/if}
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>
      #{row.id} - sprite {row.spriteId} - {row.type}{row.logic ? ` - ${row.logic}` : ''}{row.canTrade
        ? ''
        : ` - ${$t('pickerModal.noTrade')}`}
    </small>
  </span>
</button>
