<script>
  // The expanded view of one audit row: why it happened, and what it did to the data.
  //
  // The "what it did" half is new. Until the change tracker started recording it, a delete audited
  // `{ offerId: 12 }` and the row itself was already gone -- there was no copy of it anywhere, which
  // is why "I deleted the wrong offer" had no answer. For a delete this panel is that copy.
  import { t } from '../lib/i18n.js';
  import { parseAuditData, describeTarget, fieldTransitions, deletedFields } from '../lib/auditData.js';

  export let data = '';

  $: parsed = parseAuditData(data);
  $: detailPairs = parsed.detail
    ? Object.entries(parsed.detail).filter(([, v]) => v !== null && v !== undefined && v !== '')
    : [];
</script>

<div class="audit-detail">
  {#if parsed.reason}
    <p class="audit-reason">{parsed.reason}</p>
  {/if}

  {#each parsed.changes as change}
    <div class="audit-change">
      <p class="audit-change-head">
        <code>{describeTarget(change)}</code>
        <span class={`status-badge ${change.operation === 'delete' ? 'status-badge--danger' : ''}`}>
          {change.operation === 'delete' ? $t('audit.opDelete') : $t('audit.opUpdate')}
        </span>
      </p>

      {#if change.operation === 'delete'}
        <!-- The whole row, because it is the only remaining copy of it. -->
        <dl class="audit-fields">
          {#each deletedFields(change) as field}
            <dt>{field.key}</dt>
            <dd>{field.value}</dd>
          {/each}
        </dl>
      {:else}
        <ul class="audit-transitions">
          {#each fieldTransitions(change) as line}
            <li>{line}</li>
          {/each}
        </ul>
      {/if}
    </div>
  {/each}

  {#if parsed.changes.length === 0}
    <p class="muted small">{$t('audit.noEntityChanges')}</p>
  {/if}

  {#if detailPairs.length}
    <dl class="audit-fields audit-request">
      {#each detailPairs as [key, value]}
        <dt>{key}</dt>
        <dd>{typeof value === 'object' ? JSON.stringify(value) : String(value)}</dd>
      {/each}
    </dl>
  {/if}

  {#if parsed.actor}
    <p class="muted small">{$t('audit.byActor', { actor: parsed.actor })}</p>
  {/if}
</div>

<style>
  .audit-detail {
    display: grid;
    gap: 10px;
    padding: 12px 14px;
    font-size: 0.86rem;
  }

  .audit-reason {
    margin: 0;
    font-weight: 600;
  }

  .audit-change {
    border: 1px solid var(--border, rgba(128, 128, 128, 0.3));
    border-radius: 8px;
    padding: 8px 10px;
  }

  .audit-change-head {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 0 0 6px;
  }

  .audit-transitions {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    gap: 3px;
  }

  .audit-fields {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 2fr);
    gap: 2px 12px;
    margin: 0;
  }

  .audit-fields dt {
    opacity: 0.7;
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .audit-fields dd {
    margin: 0;
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .audit-request {
    opacity: 0.75;
  }

  .small {
    font-size: 0.8rem;
  }
</style>
