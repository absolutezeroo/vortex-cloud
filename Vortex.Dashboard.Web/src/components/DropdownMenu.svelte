<script lang="ts">
  // The kit's row-action menu: a trigger, and a list of items where the destructive one is marked.
  // Closes on Escape, on outside click, and after a pick; arrow keys walk the list.
  //
  //   <DropdownMenu label="Actions" items={[
  //     { id: 'edit', label: 'Edit item' },
  //     { id: 'dup', label: 'Duplicate' },
  //     { id: 'del', label: 'Delete item', danger: true },
  //   ]} onpick={(id) => run(id)} />
  import { ChevronDown } from '@lucide/svelte';

  type Props = {
    label?: string;
    items?: Array<{id: any, label: string, danger?: boolean, disabled?: boolean}>;
    onpick?: (id: any) => void;
    /** 'start' | 'end' */
    align?: string;
  };

  let { label = '', items = [], onpick, align = 'start' }: Props = $props();

  let open = $state(false);
  let cursor = $state(0);
  let root = $state<HTMLElement | undefined>();

  /**
   * Where the panel sits, in viewport coordinates.
   *
   * It used to be absolute, which put it inside whatever box happened to be positioned above it.
   * In a table cell that box is `.table-wrap`, which scrolls on overflow -- so the menu was clipped
   * by it and lengthened its scrollbar instead of floating over the page. Fixed coordinates escape
   * the overflow entirely; the trade-off is that a scroll moves the page out from under the panel,
   * which is why one closes it.
   */
  let at = $state({ top: 0, left: 0, right: 0 });

  function place() {
    const box = root?.getBoundingClientRect();

    if (box) {
      at = { top: box.bottom + 4, left: box.left, right: window.innerWidth - box.right };
    }
  }

  function toggle() {
    if (!open) {
      place();
    }

    open = !open;
  }

  function pick(item: { id: string; disabled?: boolean }) {
    if (item.disabled) return;
    open = false;
    onpick?.(item.id);
  }

  function onKeydown(event: KeyboardEvent) {
    if (!open) {
      if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        place();
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

    const away = (event: PointerEvent) => {
      if (root && !root.contains(event.target as Node)) open = false;
    };

    // A scroll moves the page under a fixed panel, so it closes rather than floating somewhere
    // that no longer means anything. Capture: the scroll that matters is an inner one.
    const gone = () => (open = false);

    document.addEventListener('pointerdown', away, true);
    document.addEventListener('scroll', gone, true);
    window.addEventListener('resize', gone);

    return () => {
      document.removeEventListener('pointerdown', away, true);
      document.removeEventListener('scroll', gone, true);
      window.removeEventListener('resize', gone);
    };
  });
</script>

<div class="dd" bind:this={root} onkeydown={onKeydown}>
  <button
    type="button"
    class="ghost-button"
    aria-haspopup="menu"
    aria-expanded={open}
    onclick={toggle}
  >
    <span>{label}</span>
    <ChevronDown size={14} strokeWidth={2} aria-hidden="true" />
  </button>

  {#if open}
    <div
      class="menu"
      class:end={align === 'end'}
      role="menu"
      style={align === 'end'
        ? `top:${at.top}px; right:${at.right}px;`
        : `top:${at.top}px; left:${at.left}px;`}
    >
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
    /* Placed from the trigger's rect in script: see `at`. */
    position: fixed;
    z-index: 90;
    min-width: 180px;
    display: grid;
    gap: 2px;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background: var(--surface-raised);
    padding: 5px;
    box-shadow: var(--shadow);
  }

  .menu.end {
    left: auto;
  }

  .item {
    width: 100%;
    text-align: left;
    border: 0;
    border-radius: 8px;
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
