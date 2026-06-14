(function () {
    const API_REFRESH_MS = 10000;
    const token = new URLSearchParams(location.search).get('token') || '';
    const cardsEl = document.getElementById('overviewCards');
    const statusEl = document.getElementById('globalStatus');
    const auditRowsEl = document.getElementById('auditRows');
    const auditLoadingEl = document.getElementById('auditLoading');
    const searchForm = document.getElementById('searchForm');
    const queryInput = document.getElementById('queryInput');
    const searchResult = document.getElementById('searchResult');
    const refreshAuditBtn = document.getElementById('refreshAudit');
    const refreshIncidentsBtn = document.getElementById('refreshIncidents');
    const incidentOverallEl = document.getElementById('incidentOverall');
    const incidentSignalsEl = document.getElementById('incidentSignals');
    const incidentTopGroupsRowsEl = document.getElementById('incidentTopGroupsRows');
    const incidentTopGroupsEmptyEl = document.getElementById('incidentTopGroupsEmpty');

    const numberFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });
    const decimalFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 });

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;');
    }

    function statusAuthMessage() {
        return token ? 'Connected' : 'Unauthorized: open this page with ?token=<your-token>';
    }

    function request(path) {
        return fetch(path, {
            headers: token ? { 'X-Admin-Token': token } : undefined,
        }).then((res) => {
            if (!res.ok) {
                throw new Error(`HTTP ${res.status}`);
            }

            return res.json();
        });
    }

    function buildCard(label, value, detail = '') {
        const card = document.createElement('article');
        card.className = 'metric-card';

        const metricLabel = document.createElement('p');
        metricLabel.className = 'metric-label';
        metricLabel.textContent = label;

        const metricValue = document.createElement('p');
        metricValue.className = 'metric-value';
        metricValue.textContent = value;

        card.appendChild(metricLabel);
        card.appendChild(metricValue);

        if (detail) {
            const metricDetail = document.createElement('p');
            metricDetail.className = 'metric-sub';
            metricDetail.textContent = detail;
            card.appendChild(metricDetail);
        }

        return card;
    }

    function setHealthClass(cardEl, status) {
        if (status === 'Critical') {
            cardEl.classList.add('metric-card--warn');
        }
    }

    function severityClass(severity) {
        if (!severity) {
            return '';
        }

        if (severity.toLowerCase() === 'critical') {
            return 'incident-signal--critical';
        }

        if (severity.toLowerCase() === 'degraded') {
            return 'incident-signal--degraded';
        }

        return '';
    }

    function renderOverviewCard(data) {
        const live = data.live || {};
        cardsEl.innerHTML = '';

        const topRooms = (live.topRooms || [])
            .map((entry) => `${entry.roomId}: ${Math.round(entry.packetsPerMinute)} p/m`)
            .join('\n');

        const topAbusers = (live.topAbusers || [])
            .map((entry) => `${entry.playerId}: ${Math.round(entry.packetsPerMinute)} p/m`)
            .join('\n');

        const categories = (data.auditLastHourByCategory || [])
            .map((item) => `${item.category}: ${item.count}`)
            .join(', ');

        const status = data.health || {};

        const cards = [
            { label: 'Active sessions', value: numberFormatter.format(data.activeSessions || 0) },
            { label: 'Active rooms', value: numberFormatter.format(data.activeRooms || 0) },
            { label: 'Managed memory', value: `${numberFormatter.format(data.managedMemoryMb || 0)} MB` },
            { label: 'Uptime', value: `${Math.floor((data.uptimeSeconds || 0) / 60)} min` },
            { label: 'Packets / second', value: decimalFormatter.format(live.packetsPerSecond || 0) },
            { label: 'Errors / minute', value: decimalFormatter.format(live.errorsPerMinute || 0) },
            {
                label: 'Latency p50 / p95',
                value: `${decimalFormatter.format(live.latencyP50Ms || 0)}ms / ${decimalFormatter.format(
                    live.latencyP95Ms || 0
                )}ms`,
            },
            { label: 'Health', value: data.status || 'Unknown', detail: categories || 'No category activity in last hour' },
            { label: 'DB status', value: String((status.database && status.database.status) || 'Unknown') },
            {
                label: 'Orleans status',
                value: String((status.orleans && status.orleans.status) || 'Unknown'),
            },
            { label: 'Top rooms', value: topRooms || 'none', detail: 'packets/minute' },
            { label: 'Top abusers', value: topAbusers || 'none', detail: 'packets/minute' },
            { label: 'Audit total', value: numberFormatter.format(data.totals?.audit || 0) },
            { label: 'Ledger total', value: numberFormatter.format(data.totals?.ledger || 0) },
            { label: 'Item events total', value: numberFormatter.format(data.totals?.items || 0) },
            {
                label: 'Performance logs total',
                value: numberFormatter.format(data.totals?.performance || 0),
                detail: 'client telemetry payload count',
            },
        ];

        cards.forEach((item) => {
            const card = buildCard(item.label, item.value, item.detail);

            if (item.label === 'Health') {
                setHealthClass(card, data.status);
            }

            cardsEl.appendChild(card);
        });
    }

    function renderAuditRows(rows) {
        if (!rows?.length) {
            auditRowsEl.innerHTML = '';
            return;
        }

        auditRowsEl.innerHTML = rows
            .map((row) => {
                const category = escapeHtml(row.category || 'unknown');
                const action = escapeHtml(row.action || 'n/a');
                const actor = escapeHtml(row.actorPlayerId ?? 'n/a');
                const target = escapeHtml(row.targetPlayerId ?? 'n/a');
                const result = escapeHtml(row.result || 'n/a');
                const occurredAt = escapeHtml(row.occurredAt || '');
                const cid = escapeHtml((row.correlationId || '').substring(0, 8));

                return `<tr>
                    <td>${occurredAt}</td>
                    <td><span class="tag">${category}</span></td>
                    <td>${action}</td>
                    <td>${actor}</td>
                    <td>${target}</td>
                    <td>${result}</td>
                    <td class="muted">${cid}</td>
                </tr>`;
            })
            .join('');
    }

    async function refreshOverview() {
        try {
            const data = await request('/api/overview');
            statusEl.textContent = `${statusAuthMessage()} - ${new Date().toLocaleTimeString()}`;
            statusEl.style.color = token ? 'var(--ok)' : 'var(--danger)';
            renderOverviewCard(data);
        } catch (err) {
            statusEl.textContent = `Request failed: ${err.message}`;
            statusEl.style.color = 'var(--danger)';
            cardsEl.innerHTML = '';
        }
    }

    async function refreshAudit() {
        try {
            const data = await request('/api/audit?limit=50');
            auditLoadingEl.style.display = 'none';
            renderAuditRows(data?.items || []);
        } catch (err) {
            auditLoadingEl.style.display = 'block';
            auditLoadingEl.textContent = `Audit load failed: ${err.message}`;
        }
    }

    function renderIncidentRows(rows) {
        if (!rows?.length) {
            incidentSignalsEl.innerHTML = '<p class="muted">No active incidents.</p>';
            return;
        }

        incidentSignalsEl.innerHTML = rows
            .map((row) => {
                const severity = escapeHtml(row.severity || 'healthy');
                const title = escapeHtml(row.title || 'Incident');
                const summary = escapeHtml(row.summary || '');
                const code = escapeHtml(row.code || '');
                const observed = Number.parseFloat(row.observed || 0);
                const threshold = Number.parseFloat(row.threshold || 0);

                return `
                    <article class="incident-signal ${severityClass(severity)}">
                        <p class="incident-signal-title">${title}
                            <span class="incident-badge ${severityClass(severity)}">${severity}</span>
                        </p>
                        <p class="incident-signal-code">${code}</p>
                        <p class="incident-signal-summary">${summary}</p>
                        <p class="incident-signal-meta">Observed: ${decimalFormatter.format(observed)} / Threshold: ${decimalFormatter.format(threshold)}</p>
                    </article>
                `;
            })
            .join('');
    }

    function renderTopErrorGroups(groups) {
        if (!groups?.length) {
            incidentTopGroupsRowsEl.innerHTML = '';
            incidentTopGroupsEmptyEl.style.display = 'block';
            return;
        }

        incidentTopGroupsEmptyEl.style.display = 'none';

        incidentTopGroupsRowsEl.innerHTML = groups
            .map((group) => {
                const fingerprint = escapeHtml(group.fingerprint || 'n/a');
                const source = escapeHtml(`${group.source || 'n/a'} / ${group.operation || 'n/a'}`);
                const type = escapeHtml(group.exceptionType || 'n/a');
                const occurrences = Number.parseFloat(group.totalOccurrences || 0);
                const lastSeen = escapeHtml(group.lastSeenAt || '');

                return `
                    <tr>
                        <td><code>${fingerprint}</code></td>
                        <td>${source}</td>
                        <td>${type}</td>
                        <td>${numberFormatter.format(occurrences)}</td>
                        <td class="muted">${lastSeen}</td>
                    </tr>
                `;
            })
            .join('');
    }

    async function refreshIncidents() {
        try {
            const data = await request('/api/incidents');

            const overall = (data?.overallSeverity || 'Healthy').toUpperCase();
            const normalizedOverall =
                overall === 'CRITICAL'
                    ? 'Critical'
                    : overall === 'DEGRADED'
                        ? 'Degraded'
                        : 'Healthy';

            incidentOverallEl.className = `incident-overall incident-overall--${normalizedOverall.toLowerCase()}`;
            incidentOverallEl.textContent =
                `${normalizedOverall} (errors/min: ${decimalFormatter.format(data?.errorSpikesPerMinute || 0)} | login-failed/min: ${decimalFormatter.format(data?.loginFailedSpikesPerMinute || 0)})`;

            renderIncidentRows(data?.signals || []);
            renderTopErrorGroups(data?.topErrorGroups || []);
        } catch (err) {
            incidentOverallEl.className = 'incident-overall';
            incidentOverallEl.textContent = `Incident snapshot failed: ${err.message}`;
            incidentSignalsEl.innerHTML = '<p class="muted">No data.</p>';
            incidentTopGroupsRowsEl.innerHTML = '';
            incidentTopGroupsEmptyEl.style.display = 'block';
            incidentTopGroupsEmptyEl.textContent = 'Could not load top error groups.';
        }
    }

    async function performSearch(event) {
        event.preventDefault();

        const query = (queryInput.value || '').trim();

        if (!query) {
            searchResult.textContent = 'Enter an id or a 32-char correlation id.';
            return;
        }

        searchResult.textContent = 'Searching...';

        try {
            const data = await request(`/api/search?q=${encodeURIComponent(query)}`);
            searchResult.textContent = JSON.stringify(data, null, 2);
        } catch (err) {
            searchResult.textContent = `Search failed: ${err.message}`;
        }
    }

    searchForm.addEventListener('submit', performSearch);
    refreshAuditBtn.addEventListener('click', refreshAudit);
    refreshIncidentsBtn.addEventListener('click', refreshIncidents);

    statusEl.textContent = statusAuthMessage();
    searchResult.textContent = token
        ? 'Use the form above to lookup details.'
        : 'Missing token; page is displayed read-only only.';

    setInterval(() => {
        refreshOverview();
        refreshAudit();
        refreshIncidents();
    }, API_REFRESH_MS);

    refreshAudit();
    refreshOverview();
    refreshIncidents();
})();
