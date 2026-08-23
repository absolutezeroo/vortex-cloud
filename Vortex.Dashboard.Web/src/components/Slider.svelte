<script>
  // A range control with the kit's track and a readout. A real <input type="range"> underneath, so
  // arrow keys, Home/End and Page Up/Down all work without a line of code here.
  //
  //   <Slider bind:value min={0} max={100} label="Drop rate" suffix="%" />

  /**
   * @typedef {Object} Props
   * @property {number} [value]
   * @property {number} [min]
   * @property {number} [max]
   * @property {number} [step]
   * @property {string} [label]
   * @property {string} [suffix]
   * @property {boolean} [disabled]
   */

  /** @type {Props} */
  let {
    value = $bindable(0),
    min = 0,
    max = 100,
    step = 1,
    label = '',
    suffix = '',
    disabled = false,
  } = $props();

  // Percentage of the way along, used to colour the filled part of the track.
  let pct = $derived(max > min ? ((Number(value) - min) / (max - min)) * 100 : 0);
</script>

<div class="slider" style:--pct={`${pct}%`}>
  <input
    class="bare"
    type="range"
    bind:value
    {min}
    {max}
    {step}
    {disabled}
    aria-label={label || undefined}
  />
  <output>{value}{suffix}</output>
</div>

<style>
  .slider {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  output {
    min-width: 46px;
    border: 1px solid var(--line-strong);
    border-radius: 8px;
    background: var(--field-bg);
    color: var(--field-ink);
    padding: 4px 8px;
    text-align: center;
    font-variant-numeric: tabular-nums;
  }

  input {
    flex: 1;
    min-width: 0;
    appearance: none;
    height: 20px;
    background: transparent;
    padding: 0;
    border: 0;
  }

  /* The filled portion is painted onto the track with a hard-stop gradient at --pct rather than
     with a second element, which keeps the thumb free to sit on top of it. */
  input::-webkit-slider-runnable-track {
    height: 6px;
    border-radius: 999px;
    background: linear-gradient(
      to right,
      var(--button-bg) 0 var(--pct),
      var(--input-bg) var(--pct) 100%
    );
  }

  input::-moz-range-track {
    height: 6px;
    border-radius: 999px;
    background: linear-gradient(
      to right,
      var(--button-bg) 0 var(--pct),
      var(--input-bg) var(--pct) 100%
    );
  }

  input::-webkit-slider-thumb {
    appearance: none;
    width: 16px;
    height: 16px;
    margin-top: -5px;
    border: 0;
    border-radius: 999px;
    background: #ffffff;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
  }

  input::-moz-range-thumb {
    width: 16px;
    height: 16px;
    border: 0;
    border-radius: 999px;
    background: #ffffff;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
  }

  input:focus-visible {
    outline: 2px solid var(--accent);
    outline-offset: 3px;
    border-radius: 999px;
  }

  input:disabled {
    opacity: 0.5;
  }
</style>
