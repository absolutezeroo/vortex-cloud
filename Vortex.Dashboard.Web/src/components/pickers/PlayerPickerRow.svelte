<script lang="ts">
  import { User } from '@lucide/svelte';
  import AssetImage from '../AssetImage.svelte';
  import { t } from '../../lib/i18n';

  /** @type {{ row: any, onchoose: (row: any) => void }} */
  let { row, onchoose } = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  <AssetImage src={row.avatarUrl} alt={row.name} size={38} fallbackIcon={User} />
  <span class="pick-dot" class:on={row.online} aria-hidden="true"></span>
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>#{row.id} - {row.online ? $t('pickerModal.online') : $t('pickerModal.offline')}</small>
  </span>
</button>
