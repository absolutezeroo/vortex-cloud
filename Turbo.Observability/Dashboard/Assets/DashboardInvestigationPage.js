(function () {
    const {
        request,
        escapeHtml,
        setGlobalStatus,
        statusAuthMessage,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const searchForm = document.getElementById('searchForm');
    const queryInput = document.getElementById('queryInput');
    const searchResult = document.getElementById('searchResult');
    const playerTimelineRows = document.getElementById('playerTimelineRows');
    const playerTimelineState = document.getElementById('playerTimelineState');
    const playerTimelineEmpty = document.getElementById('playerTimelineEmpty');

    function normalizeTimelineTime(value) {
        const parsed = Date.parse(value || '');
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function renderPlayerTimeline(data) {
        playerTimelineRows.innerHTML = '';

        const rows = [];
        if (!data || data.kind !== 'id') {
            playerTimelineState.textContent = 'Use a player id to build a structured timeline.';
            playerTimelineEmpty.style.display = 'block';
            playerTimelineEmpty.textContent = 'No player timeline loaded.';
            searchResult.style.display = 'block';
            searchResult.textContent = 'No player timeline found for this query.';
            return;
        }

        const playerId = String((queryInput.value || '').trim());

        if (Array.isArray(data.asActor)) {
            data.asActor.forEach((row) => {
                rows.push({
                    time: row.occurredAt,
                    sortTime: normalizeTimelineTime(row.occurredAt),
                    kind: 'audit',
                    actor: row.actorPlayerId ?? row.targetPlayerId ?? playerId,
                    detail: `${row.category || 'audit'} / ${row.action || 'event'}${
                        row.targetPlayerId ? ` (target ${row.targetPlayerId})` : ''
                    }`,
                });
            });
        }

        if (Array.isArray(data.ledger)) {
            data.ledger.forEach((row) => {
                rows.push({
                    time: row.occurredAt,
                    sortTime: normalizeTimelineTime(row.occurredAt),
                    kind: 'ledger',
                    actor: row.playerId ?? playerId,
                    detail: `${row.currency || 'currency'} ${Number(row.delta) >= 0 ? '+' : ''}${row.delta} -> ${row.balanceAfter ?? 'n/a'} (${row.reason || 'economy'})`,
                });
            });
        }

        if (Array.isArray(data.itemHistory)) {
            data.itemHistory.forEach((row) => {
                rows.push({
                    time: row.occurredAt,
                    sortTime: normalizeTimelineTime(row.occurredAt),
                    kind: 'item',
                    actor: playerId,
                    detail: `item event: ${row.eventType || 'unknown'}`,
                });
            });
        }

        if (!rows.length) {
            playerTimelineState.textContent = `Player timeline for ${playerId}: no events in current range.`;
            playerTimelineEmpty.style.display = 'block';
            playerTimelineEmpty.textContent = 'No events found for this player.';
            searchResult.style.display = 'none';
            return;
        }

        rows.sort((a, b) => b.sortTime - a.sortTime);
        playerTimelineRows.innerHTML = rows
            .map((row) => {
                const styleClass =
                    row.kind === 'audit'
                        ? 'timeline-audit'
                        : row.kind === 'ledger'
                            ? 'timeline-ledger'
                            : 'timeline-item';

                return `<tr>
                    <td>${escapeHtml(row.time || '')}</td>
                    <td><span class="${styleClass}">${escapeHtml(row.kind)}</span></td>
                    <td>${escapeHtml(row.actor || 'n/a')}</td>
                    <td>${escapeHtml(row.detail || 'event')}</td>
                </tr>`;
            })
            .join('');

        playerTimelineState.textContent = `Player timeline for ${playerId}: ${rows.length} events.`;
        playerTimelineEmpty.style.display = 'none';
        searchResult.style.display = 'none';
    }

    async function performSearch(event) {
        event.preventDefault();

        const query = (queryInput.value || '').trim();
        if (!query) {
            searchResult.textContent = 'Enter an id or a 32-char correlation id.';
            return;
        }

        searchResult.textContent = 'Searching...';
        searchResult.style.display = 'none';
        playerTimelineEmpty.style.display = 'none';
        playerTimelineRows.innerHTML = '';
        playerTimelineState.textContent = 'Searching...';

        try {
            const data = await request(`/api/search?q=${encodeURIComponent(query)}`);
            const isPlayerIdQuery = /^\d+$/.test(query);

            if (isPlayerIdQuery) {
                renderPlayerTimeline(data);
            } else {
                searchResult.style.display = 'block';
                searchResult.textContent = JSON.stringify(data, null, 2);
                playerTimelineState.textContent = 'Use a player id for timeline view.';
                playerTimelineEmpty.style.display = 'block';
                playerTimelineEmpty.textContent = 'No player timeline loaded.';
            }
        } catch (err) {
            searchResult.style.display = 'block';
            searchResult.textContent = `Search failed: ${err.message}`;
            playerTimelineRows.innerHTML = '';
            playerTimelineEmpty.style.display = 'block';
            playerTimelineEmpty.textContent = `Search failed: ${err.message}`;
            playerTimelineState.textContent = `Search failed: ${err.message}`;
        }
    }

    searchForm.addEventListener('submit', performSearch);
    setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
})();
