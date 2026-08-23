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
  import TableFilter from '../components/TableFilter.svelte';
  import { filterRows } from '../lib/tableView.js';
  import { t, translate } from '../lib/i18n.js';
  import { get } from 'svelte/store';
  import { Lock, Trash2 } from '@lucide/svelte';

  const MAX_LINES = 2000;

  let command = $state('');
  let lastRun = $state('');
  let connection = $state('connecting');

  // One stream for everything the operator sees: the server's own log lines, the commands they
  // typed, and what those printed. A real terminal does not separate them, and reading a command's
  // effect means seeing it land next to the log it caused.
  let lines = $state([]);
  let viewport = $state(null);

  let canFollow = $derived(hasDashboardCapability($identity, CAPABILITIES.serverConsoleRead));
  let canUseConsole = $derived(hasDashboardCapability($identity, CAPABILITIES.opsServerConsole));

  const commands = createResource(
    () => ['console', 'commands'],
    () => apiGet('/api/v1/operations/console/commands'),
    { enabled: () => canUseConsole },
  );

  // ~50 commands in one flat list: filtering is faster than reading it.
  let commandQuery = $state('');
  let commandRows = $derived(commands.data ?? []);
  let commandView = $derived(filterRows(commandRows, commandQuery));

  const ops = createWriteOps(() => {
    const result = get(ops).results.run;

    push('echo', `> ${lastRun}`);

    for (const line of result?.output ?? []) {
      push('out', line);
    }

    command = '';
  });

  function push(kind, text) {
    // Scrolled-to-bottom is read before the row is added, so following the tail keeps working while
    // reading back through the buffer does not get yanked away by every new line.
    const following = atBottom();

    lines.push({ kind, text });

    if (lines.length > MAX_LINES) {
      lines.splice(0, lines.length - MAX_LINES);
    }

    if (following) {
      requestAnimationFrame(() => {
        if (viewport) viewport.scrollTop = viewport.scrollHeight;
      });
    }
  }

  function atBottom() {
    if (!viewport) return true;
    return viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight < 40;
  }

  $effect(() => {
    if (!canFollow) {
      return;
    }

    const source = new EventSource('/api/v1/operations/console/stream');

    source.onopen = () => {
      connection = 'live';
    };
    source.onmessage = (event) => push('log', JSON.parse(event.data));
    source.onerror = () => {
      // EventSource reconnects on its own; this only reports the gap.
      connection = 'lost';
    };

    return () => source.close();
  });

  // The verb typed so far, so the page can say what the command needs before it is committed to.
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

<section class="panel">
  <div class="panel-head">
    <h2>{$t('console.liveTitle')}</h2>
    {#if canFollow}
      <div class="console-status">
        <span class="dot {connection}"></span>
        <span class="muted">{$t(`console.connection.${connection}`)}</span>
        <button type="button" class="ghost" onclick={() => (lines = [])} title={$t('console.clear')}>
          <Trash2 size={14} strokeWidth={2} aria-hidden="true" />
        </button>
      </div>
    {/if}
  </div>

  {#if !canFollow}
    <AccessDeniedNotice message={$t('console.followAccessDenied')} />
  {:else}
    <div class="console-viewport" bind:this={viewport}>
      {#each lines as line}
        <div class="console-line {line.kind}">{line.text}</div>
      {/each}
      {#if !lines.length}
        <div class="console-line muted">{$t('console.waiting')}</div>
      {/if}
    </div>
  {/if}

  {#if canUseConsole}
    <form
      class="console-input"
      onsubmit={(e) => {
        e.preventDefault();
        stageRun();
      }}
    >
      <span class="prompt">&gt;</span>
      <input autocomplete="off" spellcheck="false"
        id="console-command"
        bind:value={command}
        placeholder={$t('console.placeholder')}
        aria-label={$t('console.command')}
      />
      <button type="submit" disabled={$ops.busyKeys.run || !command.trim() || (matched && !matched.allowed)}>
        {$t('common.run')}
      </button>
    </form>

    {#if command.trim() && !matched}
      <p class="empty-state danger" role="alert">{$t('console.unknownCommand')}</p>
    {:else if matched && !matched.allowed}
      <p class="empty-state danger" role="alert">
        {$t('console.commandDenied', { capability: matched.requiredCapability })}
      </p>
    {:else if matched}
      <p class="empty-state">{matched.description}</p>
    {/if}

    {#if $ops.errors.run}<p class="empty-state danger" role="alert">{$ops.errors.run}</p>{/if}
    {#if $ops.results.run}
      <OpResult result={$ops.results.run} onCopy={copy} copyLabel={$t('common.copy')} />
    {/if}
  {/if}
</section>

{#if canUseConsole}
  <section class="panel">
    <div class="panel-head"><h2>{$t('console.availableTitle')}</h2></div>

    {#if commands.loading && !commands.data}
      <p class="empty-state">{$t('common.loading')}</p>
    {:else if commands.error}
      <p class="empty-state danger" role="alert">{$t('console.commandsUnavailable')}</p>
    {:else}
      <TableFilter bind:query={commandQuery} shown={commandView.length} total={commandRows.length} />

      <table class="data-table">
        <thead>
          <tr>
            <th>{$t('console.usage')}</th>
            <th>{$t('console.requires')}</th>
          </tr>
        </thead>
        <tbody>
          {#each commandView as entry}
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
{/if}

<ConfirmStagedModal {ops} eyebrow={$t('console.confirmEyebrow')} />

<style>
  .console-status {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: currentColor;
    opacity: 0.5;
  }

  .dot.live {
    background: #3ddc97;
    opacity: 1;
  }

  .dot.lost {
    background: #ff6b6b;
    opacity: 1;
  }

  .console-viewport {
    background: #070b18;
    border: 1px solid rgba(127, 140, 180, 0.25);
    border-radius: 8px;
    padding: 12px;
    height: 46vh;
    min-height: 260px;
    overflow: auto;
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
    font-size: 12.5px;
    line-height: 1.55;
    color: #e6ebff;
  }

  .console-line {
    white-space: pre-wrap;
    word-break: break-word;
  }

  /* The operator's own input, echoed so a command reads as a turn in the conversation. */
  .console-line.echo {
    color: #f2b544;
  }

  .console-line.out {
    color: #9ad8ff;
  }

  .console-input {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
  }

  .console-input .prompt {
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
    opacity: 0.6;
  }

  .console-input input {
    flex: 1;
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
  }

  .ghost {
    background: none;
    border: none;
    cursor: pointer;
    padding: 2px 4px;
    display: inline-flex;
    align-items: center;
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
