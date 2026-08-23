<script>
  // The kit's toggle. A real <input type="checkbox"> under a drawn track, so it is focusable,
  // announced, and submits with a form -- a div with a click handler is none of those things.
  //
  //   <Switch bind:checked label="Auto-refresh" />

  /**
   * @typedef {Object} Props
   * @property {boolean} [checked]
   * @property {string} [label] - visible text; when empty pass an aria-label instead
   * @property {string} [ariaLabel]
   * @property {boolean} [disabled]
   * @property {(checked: boolean) => void} [onchange]
   */

  /** @type {Props} */
  let { checked = $bindable(false), label = '', ariaLabel = '', disabled = false, onchange } = $props();
</script>

<label class="switch" class:disabled>
  <input
    type="checkbox"
    role="switch"
    bind:checked
    {disabled}
    aria-label={label ? undefined : ariaLabel}
    onchange={() => onchange?.(checked)}
  />
  <span class="track" aria-hidden="true"><span class="knob"></span></span>
  {#if label}<span class="label">{label}</span>{/if}
</label>

<style>
  .switch {
    display: inline-flex;
    align-items: center;
    gap: 9px;
    cursor: pointer;
  }

  .switch.disabled {
    opacity: 0.55;
    cursor: default;
  }

  /* Off-screen rather than display:none: a hidden input is not focusable and never reaches the
     accessibility tree, which is the whole reason for using a real checkbox here. */
  input {
    position: absolute;
    width: 1px;
    height: 1px;
    margin: -1px;
    padding: 0;
    border: 0;
    clip-path: inset(50%);
    overflow: hidden;
  }

  .track {
    position: relative;
    width: 38px;
    height: 21px;
    flex: 0 0 auto;
    border-radius: 999px;
    background: var(--toggle-off);
    transition: background 140ms ease;
  }

  .knob {
    position: absolute;
    top: 2px;
    left: 2px;
    width: 17px;
    height: 17px;
    border-radius: 999px;
    background: #ffffff;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.35);
    transition: transform 140ms ease;
  }

  input:checked + .track {
    background: var(--toggle-on);
  }

  input:checked + .track .knob {
    transform: translateX(17px);
  }

  input:focus-visible + .track {
    outline: 2px solid var(--accent);
    outline-offset: 2px;
  }

  .label {
    color: var(--ink);
  }
</style>
