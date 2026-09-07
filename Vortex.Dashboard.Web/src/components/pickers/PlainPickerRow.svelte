<script lang="ts">
  // The row every plain directory shares: a name, an id, and whatever the server thought worth
  // saying about it. Fourteen kinds render through this one file rather than fourteen branches.
  /** @type {{ row: any, onchoose: (row: any) => void }} */
  let { row, onchoose } = $props();
</script>

<button type="button" class="pick-row" onclick={() => onchoose(row)}>
  <span class="pick-icon" aria-hidden="true"></span>
  <span class="pick-main">
    <strong>{row.name}</strong>
    <small>#{row.id}{row.description ? ` - ${row.description}` : ''}</small>
  </span>
</button>
