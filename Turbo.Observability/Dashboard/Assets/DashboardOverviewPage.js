(function () {
    const {
        request,
        buildCard,
        numberFormatter,
        decimalFormatter,
        setGlobalStatus,
        statusAuthMessage,
        API_REFRESH_MS,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const cardsEl = document.getElementById('overviewCards');
    const refreshBtn = document.getElementById('refreshOverview');

    function setHealthClass(cardEl, status) {
        cardEl.classList.remove('metric-card--warn', 'metric-card--critical');

        if (status === 'Critical') {
            cardEl.classList.add('metric-card--critical');
            return;
        }

        if (status === 'Degraded') {
            cardEl.classList.add('metric-card--warn');
        }
    }

    function renderOverviewCard(data) {
        const live = data?.live || {};
        cardsEl.innerHTML = '';

        const topRooms = (live.topRooms || [])
            .map((entry) => `${entry.roomId}: ${Math.round(entry.packetsPerMinute)} p/m`)
            .join('\n');

        const topAbusers = (live.topAbusers || [])
            .map((entry) => `${entry.playerId}: ${Math.round(entry.packetsPerMinute)} p/m`)
            .join('\n');

        const categories = (data?.auditLastHourByCategory || [])
            .map((item) => `${item.category}: ${item.count}`)
            .join(', ');

        const status = data?.health || {};

        const cards = [
            { label: 'Active sessions', value: numberFormatter.format(data?.activeSessions || 0) },
            { label: 'Active rooms', value: numberFormatter.format(data?.activeRooms || 0) },
            {
                label: 'Managed memory',
                value: `${numberFormatter.format(data?.managedMemoryMb || 0)} MB`,
            },
            { label: 'Uptime', value: `${Math.floor((data?.uptimeSeconds || 0) / 60)} min` },
            {
                label: 'Packets / second',
                value: decimalFormatter.format(live.packetsPerSecond || 0),
            },
            {
                label: 'Errors / minute',
                value: decimalFormatter.format(live.errorsPerMinute || 0),
            },
            {
                label: 'Latency p50 / p95',
                value: `${decimalFormatter.format(live.latencyP50Ms || 0)}ms / ${decimalFormatter.format(
                    live.latencyP95Ms || 0
                )}ms`,
            },
            {
                label: 'Health',
                value: data?.status || 'Unknown',
                detail: categories || 'No category activity in last hour',
            },
            { label: 'DB status', value: String((status.database && status.database.status) || 'Unknown') },
            {
                label: 'Orleans status',
                value: String((status.orleans && status.orleans.status) || 'Unknown'),
            },
            { label: 'Top rooms', value: topRooms || 'none', detail: 'packets/minute' },
            { label: 'Top abusers', value: topAbusers || 'none', detail: 'packets/minute' },
            { label: 'Audit total', value: numberFormatter.format(data?.totals?.audit || 0) },
            { label: 'Ledger total', value: numberFormatter.format(data?.totals?.ledger || 0) },
            { label: 'Item events total', value: numberFormatter.format(data?.totals?.items || 0) },
            {
                label: 'Performance logs total',
                value: numberFormatter.format(data?.totals?.performance || 0),
                detail: 'client telemetry payload count',
            },
        ];

        cards.forEach((item) => {
            const card = buildCard(item.label, item.value, item.detail);
            if (item.label === 'Health') {
                setHealthClass(card, data?.status);
            }

            cardsEl.appendChild(card);
        });
    }

    async function refreshOverview() {
        try {
            const data = await request('/api/overview');
            const now = new Date().toLocaleTimeString();
            const health = data?.status || 'Unknown';
            const color =
                health === 'Critical'
                    ? 'var(--danger)'
                    : health === 'Degraded'
                        ? '#ffca76'
                        : 'var(--ok)';
            setGlobalStatus(`${statusAuthMessage()} - ${now} - ${health}`, color);
            renderOverviewCard(data);
        } catch (err) {
            setGlobalStatus(`Request failed: ${err.message}`, 'var(--danger)');
            cardsEl.innerHTML = '';
        }
    }

    refreshBtn?.addEventListener('click', () => {
        void refreshOverview();
    });

    setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
    void refreshOverview();
    setInterval(() => {
        void refreshOverview();
    }, API_REFRESH_MS);
})();
