<script>
  // The kit's row-action menu: a trigger, and a list of items where the destructive one is marked.
  // Closes on Escape, on outside click, and after a pick; arrow keys walk the list.
  //
  //   <DropdownMenu label="Actions" items={[
  //     { id: 'edit', label: 'Edit item' },
  //     { id: 'dup', label: 'Duplicate' },
  //     { id: 'del', label: 'Delete item', danger: true },
  //   ]} onpick={(id) => run(id)} />
  import { ChevronDown } from '@lucide/svelte';

  /**
   * @typedef {Object} Props
   * @property {string} [label]
   * @property {Array<{id: any, label: string, danger?: boolean, disabled?: boolean}>} [items]
   * @property {(id: any) => void} [onpick]
   * @property {string} [align] - 'start' | 'end'
   */

  /** @type {Props} */
  let { label = '', items = [], onpick, align = 'start' } = $props();

  let open = $state(false);
  let cursor = $state(0);
  let root = $state();

  function pick(item) {
    if (item.disabled) return;
    open = false;
    onpick?.(item.id);
  }

  function onKeydown(event) {
    if (!open) {
      if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        open = true;
        cursor = 0;
      }
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      open = false;
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      cursor = (cursor + 1) % items.length;
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      cursor = (cursor - 1 + items.length) % items.length;
    } else if (event.key === 'Enter') {
      event.preventDefault();
      pick(items[cursor]);
    }
  }

  // Pointerdown rather than click: a click that lands on another control should close this menu
  // before that control reacts, not after.
  $effect(() => {
    if (!open) return;

    const away = (event) => {
      if (root && !root.contains(event.target)) open = false;
    };

    document.addEventListener('pointerdown', away, true);
    return () => document.removeEventListener('pointerdown', away, true);
  });
</script>

<div class="dd" bind:this={root} onkeydown={onKeydown}>
  <button
    type="button"
    class="ghost-button"
    aria-haspopup="menu"
    aria-expanded={open}
    onclick={() => (open = !open)}
  >
    <span>{label}</span>
    <ChevronDown size={14} strokeWidth={2} aria-hidden="true" />
  </button>

  {#if open}
    <div class="menu" class:end={align === 'end'} role="menu">
      {#each items as item, index (item.id)}
        <button
          type="button"
          role="menuitem"
          class="item"
          class:danger={item.danger}
          class:active={index === cursor}
          disabled={item.disabled}
          onmouseenter={() => (cursor = index)}
          onclick={() => pick(item)}
        >
          {item.label}
        </button>
      {/each}
    </div>
  {/if}
</div>

<style>
  .dd {
    position: relative;
    display: inline-flex;
  }

  .menu {
    position: absolute;
    top: calc(100% + 6px);
    left: 0;
    z-index: 90;
    min-width: 180px;
    display: grid;
    gap: 2px;
    border: 1px solid var(--line-strong);
    border-radius: 10px;
    background: var(--surface-raised);
    padding: 5px;
    box-shadow: var(--shadow);
  }

  .menu.end {
    left: auto;
    right: 0;
  }

  .item {
    width: 100%;
    text-align: left;
    border: 0;
    border-radius: 7px;
    background: transparent;
    color: var(--ink);
    padding: 7px 9px;
    font-weight: 500;
  }

  .item.active:not(:disabled) {
    background: var(--surface-hover);
  }

  .item.danger {
    color: var(--danger);
  }

  .item:disabled {
    opacity: 0.5;
    cursor: default;
  }
</style>
