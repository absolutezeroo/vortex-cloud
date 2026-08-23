<script>
  // Determinate or indeterminate progress bar, per the kit.
  //
  //   <Progress value={75} label="Import" />        determinate
  //   <Progress indeterminate />                    running, no known end
  //   <Progress value={40} small />

  /**
   * @typedef {Object} Props
   * @property {number} [value] - 0..100, ignored when indeterminate
   * @property {boolean} [indeterminate]
   * @property {boolean} [small]
   * @property {string} [label] - announced to assistive tech; also shown when showValue is set
   * @property {boolean} [showValue]
   */

  /** @type {Props} */
  let { value = 0, indeterminate = false, small = false, label = '', showValue = false } = $props();

  let clamped = $derived(Math.max(0, Math.min(100, Number(value) || 0)));
</script>

<div class="progress-row">
  <div
    class="track"
    class:small
    role="progressbar"
    aria-label={label || undefined}
    aria-valuenow={indeterminate ? undefined : clamped}
    aria-valuemin={indeterminate ? undefined : 0}
    aria-valuemax={indeterminate ? undefined : 100}
  >
    <div class="bar" class:indeterminate style={indeterminate ? '' : `width: ${clamped}%`}></div>
  </div>
  {#if showValue && !indeterminate}<small class="muted">{clamped}%</small>{/if}
</div>

<style>
  .progress-row {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .track {
    flex: 1;
    min-width: 0;
    height: 12px;
    border-radius: 999px;
    background: var(--input-bg);
    overflow: hidden;
  }

  .track.small {
    height: 7px;
  }

  .bar {
    height: 100%;
    border-radius: 999px;
    background: var(--button-bg);
    transition: width 220ms ease;
  }

  /* Diagonal stripes drifting sideways: the bar cannot say how far along it is, so it says only
     that something is still happening. */
  .bar.indeterminate {
    width: 100%;
    background-image: linear-gradient(
      115deg,
      var(--button-bg) 25%,
      var(--accent-strong) 25%,
      var(--accent-strong) 50%,
      var(--button-bg) 50%,
      var(--button-bg) 75%,
      var(--accent-strong) 75%
    );
    background-size: 22px 22px;
    animation: progress-drift 900ms linear infinite;
  }

  @keyframes progress-drift {
    to {
      background-position: 22px 0;
    }
  }

  @media (prefers-reduced-motion: reduce) {
    .bar.indeterminate {
      animation: none;
    }

    .bar {
      transition: none;
    }
  }

  small {
    min-width: 34px;
    text-align: right;
    font-variant-numeric: tabular-nums;
  }
</style>
