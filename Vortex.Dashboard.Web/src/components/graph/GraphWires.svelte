<script>
  // The wires, drawn once behind every node.
  //
  // One SVG for the whole canvas rather than one per connection: they cross each other, so they
  // have to share a stacking context, and a bezier that starts at one node and ends at another
  // cannot live inside either of them.
  import { portColour } from '../../lib/graph/model';

  /** @type {{ wires: any[], geometry: (wire: any) => any, dragging: any }} */
  let { wires, geometry, dragging = null } = $props();

  /**
   * A horizontal bezier: both handles reach sideways, never up.
   *
   * That is what makes a graph readable at a glance -- every wire leaves an output to the right and
   * enters an input from the left, so following one is never ambiguous even where a dozen cross.
   * The handle length grows with the gap so short hops stay tight and long ones sweep.
   */
  function path(from, to) {
    const reach = Math.max(40, Math.abs(to.x - from.x) * 0.5);

    return `M ${from.x} ${from.y} C ${from.x + reach} ${from.y}, ${to.x - reach} ${to.y}, ${to.x} ${to.y}`;
  }
</script>

<svg class="wires" aria-hidden="true">
  {#each wires as wire (`${wire.kind}:${wire.from}:${wire.to}:${wire.filterIndex ?? ''}`)}
    {@const g = geometry(wire)}
    {#if g}
      <!-- Flow is white and thick: it is the order, and it is the spine of the graph. Data wires
           take the colour of the fact they carry, so a room wire reads as a room everywhere. -->
      <path
        d={path(g.from, g.to)}
        class="wire"
        class:flow={wire.kind === 'flow'}
        style:stroke={wire.kind === 'flow' ? 'var(--ink)' : portColour(g.kind)}
      />
    {/if}
  {/each}

  {#if dragging}
    <!-- The wire being pulled. Dashed, because it is not connected to anything yet. -->
    <path d={path(dragging.from, dragging.to)} class="wire pulling" />
  {/if}
</svg>

<style>
  .wires {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    overflow: visible;
    /* Behind the nodes, and never in the way of a click meant for one. */
    pointer-events: none;
  }

  .wire {
    fill: none;
    stroke-width: 2.5;
    stroke-linecap: round;
  }

  .flow {
    stroke-width: 3.5;
    opacity: 0.9;
  }

  .pulling {
    stroke: var(--accent-strong);
    stroke-dasharray: 6 5;
    opacity: 0.9;
  }
</style>
