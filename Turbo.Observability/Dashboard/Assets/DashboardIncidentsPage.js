(function () {
    const {
        request,
        setGlobalStatus,
        statusAuthMessage,
        API_REFRESH_MS,
        decimalFormatter,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const incidentOverallEl = document.getElementById('incidentOverall');
    const incidentSignalsEl = document.getElementById('incidentSignals');
    const incidentTopGroupsRowsEl = document.getElementById('incidentTopGroupsRows');
    const incidentTopGroupsEmptyEl = document.getElementById('incidentTopGroupsEmpty');
    const refreshBtn = document.getElementById('refreshIncidents');

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

    function renderIncidentRows(rows) {
        if (!rows?.length) {
            incidentSignalsEl.innerHTML = '<p class="muted">No active incidents.</p>';
            return;
        }

        incidentSignalsEl.innerHTML = rows
            .map((row) => {
                const severity = row.severity || 'healthy';
                const title = row.title || 'Incident';
                const summary = row.summary || '';
                const code = row.code || '';
                const observed = Number.parseFloat(row.observed || 0);
                const threshold = Number.parseFloat(row.threshold || 0);
                const signalClass = severityClass(severity);

                return `
                    <article class="incident-signal ${signalClass}">
                        <p class="incident-signal-title">${title}
                            <span class="incident-badge ${signalClass}">${severity}</span>
                        </p>
                        <p class="incident-signal-code">${code}</p>
                        <p class="incident-signal-summary">${summary}</p>
                        <p class="incident-signal-meta">Observed: ${decimalFormatter.format(
                            observed
                        )} / Threshold: ${decimalFormatter.format(threshold)}</p>
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
                return `<tr>
                    <td><code>${group.fingerprint || 'n/a'}</code></td>
                    <td>${group.source || 'n/a'} / ${group.operation || 'n/a'}</td>
                    <td>${group.exceptionType || 'n/a'}</td>
                    <td>${Math.round(group.totalOccurrences || 0)}</td>
                    <td class="muted">${group.lastSeenAt || ''}</td>
                </tr>`;
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
            incidentOverallEl.textContent = `${normalizedOverall} (errors/min: ${decimalFormatter.format(
                data?.errorSpikesPerMinute || 0
            )} | login-failed/min: ${decimalFormatter.format(
                data?.loginFailedSpikesPerMinute || 0
            )})`;

            renderIncidentRows(data?.signals || []);
            renderTopErrorGroups(data?.topErrorGroups || []);
            setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
        } catch (err) {
            incidentOverallEl.className = 'incident-overall';
            incidentOverallEl.textContent = `Incident snapshot failed: ${err.message}`;
            incidentSignalsEl.innerHTML = '<p class="muted">No data.</p>';
            incidentTopGroupsRowsEl.innerHTML = '';
            incidentTopGroupsEmptyEl.style.display = 'block';
            incidentTopGroupsEmptyEl.textContent = 'Could not load top error groups.';
            setGlobalStatus(`Incident snapshot failed: ${err.message}`, 'var(--danger)');
        }
    }

    refreshBtn.addEventListener('click', refreshIncidents);
    void refreshIncidents();
    setInterval(() => {
        void refreshIncidents();
    }, API_REFRESH_MS);
})();
