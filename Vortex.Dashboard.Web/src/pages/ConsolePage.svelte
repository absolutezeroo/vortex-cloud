<script>
  import ConfirmStagedModal from '../components/ConfirmStagedModal.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import OpResult from '../components/OpResult.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { createResource } from '../lib/resource.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { apiGet } from '../lib/api.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';
  import { get } from 'svelte/store';
  import { Terminal, Lock } from '@lucide/svelte';

  let command = $state('');

  // Every line the operator ran this session, with what it printed. Kept on the page rather than
  // fetched: the server records the act in the audit trail, not a transcript to read back.
  let transcript = $state([]);

  const commands = createResource(
    () => ['console', 'commands'],
    () => apiGet('/api/v1/operations/console/commands'),
  );

  let lastRun = $state('');

  // createWriteOps calls this with no arguments, so the response is read back off its own store.
  const ops = createWriteOps(() => {
    const result = get(ops).results.run;

    transcript = [...transcript, { command: lastRun, lines: result?.output ?? [] }];
    command = '';
  });

  let canUseConsole = $derived(hasDashboardCapability($identity, CAPABILITIES.opsServerConsole));

  // The verb the operator has typed so far, so the page can show what that command needs before
  // they commit to it.
  let typedVerb = $derived(command.trim().split(/\s+/)[0]?.toLowerCase() ?? '');
  let matched = $derived(commands.data?.find((c) => c.name === typedVerb));

  function stageRun() {
    const line = command.trim();

    if (!line) {
      return;
    }

    if (matched && !matched.allowed) {
      ops.fail('run', translate('console.commandDenied', { capability: matched.requiredCapability }));
      return;
    }

    lastRun = line;

    ops.ask(
      '/api/v1/operations/console/run',
      { command: line },
      translate('console.runTitle'),
      translate('console.runSummary', { command: line }),
      {
        key: 'run',
        valid: Boolean(matched),
        invalidMessage: translate('console.unknownCommand'),
      },
    );
  }

  function fill(usage) {
    // The usage string carries <placeholders>; drop them so the operator types over a bare verb
    // rather than deleting angle brackets.
    command = usage.replace(/<[^>]*>/g, '').trim() + ' ';
  }

  async function copy(value) {
    try {
      await navigator.clipboard.writeText(value || '');
    } catch {
      // Clipboard is best-effort.
    }
  }
</script>

<section class="panel">
  <PageHeader title={$t('console.title')} description={$t('console.description')} />
</section>

{#if !canUseConsole}
  <section class="panel">
    <AccessDeniedNotice message={$t('console.accessDenied')} />
  </section>
{:else}
  <div class="op-grid">
    <section class="panel">
      <div class="panel-head"><h2>{$t('console.runTitle')}</h2></div>

      <div class="op-field">
        <label for="console-command">{$t('console.command')}</label>
        <input
          id="console-command"
          bind:value={command}
          placeholder="mystery-key bob gold"
          autocomplete="off"
          spellcheck="false"
        />
      </div>

      {#if command.trim() && !matched}
        <p class="empty-state danger">{$t('console.unknownCommand')}</p>
      {:else if matched && !matched.allowed}
        <p class="empty-state danger">
          {$t('console.commandDenied', { capability: matched.requiredCapability })}
        </p>
      {:else if matched}
        <p class="empty-state">{matched.description}</p>
      {/if}

      <div class="op-actions">
        <button
          type="button"
          onclick={stageRun}
          disabled={$ops.busyKeys.run || !command.trim() || (matched && !matched.allowed)}
        >
          {$t('common.run')}
        </button>
      </div>

      {#if $ops.errors.run}<p class="empty-state danger">{$ops.errors.run}</p>{/if}
      {#if $ops.results.run}
        <OpResult result={$ops.results.run} onCopy={copy} copyLabel={$t('common.copy')} />
      {/if}

      {#if transcript.length}
        <div class="console-transcript">
          {#each transcript as entry}
            <div class="console-entry">
              <div class="console-echo"><Terminal size={13} strokeWidth={2} aria-hidden="true" /> {entry.command}</div>
              {#each entry.lines as line}
                <div class="console-line">{line}</div>
              {/each}
              {#if !entry.lines.length}
                <div class="console-line muted">{$t('console.noOutput')}</div>
              {/if}
            </div>
          {/each}
        </div>
      {/if}
    </section>

    <section class="panel">
      <div class="panel-head"><h2>{$t('console.availableTitle')}</h2></div>

      {#if commands.loading && !commands.data}
        <p class="empty-state">{$t('common.loading')}</p>
      {:else if commands.error}
        <p class="empty-state danger">{$t('console.commandsUnavailable')}</p>
      {:else}
        <table class="data-table">
          <thead>
            <tr>
              <th>{$t('console.usage')}</th>
              <th>{$t('console.requires')}</th>
            </tr>
          </thead>
          <tbody>
            {#each commands.data ?? [] as entry}
              <tr class:denied={!entry.allowed}>
                <td>
                  <button type="button" class="linklike" onclick={() => fill(entry.usage)} disabled={!entry.allowed}>
                    {entry.usage}
                  </button>
                  <div class="muted">{entry.description}</div>
                </td>
                <td>
                  {#if entry.requiredCapability}
                    <span class="cap">
                      {#if !entry.allowed}<Lock size={12} strokeWidth={2} aria-hidden="true" />{/if}
                      {entry.requiredCapability}
                    </span>
                  {:else}
                    <span class="muted">{$t('console.noExtraCapability')}</span>
                  {/if}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      {/if}
    </section>
  </div>
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('console.confirmEyebrow')} />

<style>
  .console-transcript {
    margin-top: 14px;
    border-radius: 8px;
    overflow: auto;
    max-height: 320px;
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
    font-size: 12.5px;
  }

  .console-entry + .console-entry {
    margin-top: 10px;
  }

  .console-echo {
    display: flex;
    align-items: center;
    gap: 6px;
    opacity: 0.75;
    padding-bottom: 2px;
  }

  .console-line {
    white-space: pre-wrap;
    word-break: break-word;
    padding-left: 19px;
  }

  .muted {
    opacity: 0.65;
  }

  .cap {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
    font-size: 12px;
  }

  tr.denied {
    opacity: 0.55;
  }

  .linklike {
    background: none;
    border: none;
    padding: 0;
    font: inherit;
    cursor: pointer;
    text-align: left;
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  .linklike:disabled {
    cursor: not-allowed;
    text-decoration: none;
  }
</style>
