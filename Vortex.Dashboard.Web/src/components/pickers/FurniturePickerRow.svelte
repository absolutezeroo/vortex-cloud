<script lang="ts">
  import { t } from '../../lib/i18n';

  /** @type {{ row: any, onchoose: (row: any) => void }} */
  let { row, onchoose } = $props();
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
