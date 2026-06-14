(function () {
    const {
        request,
        setGlobalStatus,
        statusAuthMessage,
        API_REFRESH_MS,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const economyForm = document.getElementById('economyForm');
    const economyPlayerInput = document.getElementById('economyPlayerId');
    const economyRowsEl = document.getElementById('economyRows');
    const economyStateEl = document.getElementById('economyState');
    const refreshBtn = document.getElementById('refreshEconomy');

    function renderEconomyRows(rows) {
        economyRowsEl.innerHTML = '';

        if (!rows?.length) {
            economyStateEl.textContent = 'No economy rows in the selected range.';
            return;
        }

        economyStateEl.textContent = `Showing ${rows.length} rows.`;
        economyRowsEl.innerHTML = rows
            .map((row) => {
                return `<tr>
                    <td>${row.occurredAt || ''}</td>
                    <td>${row.playerId}</td>
                    <td>${row.currency}</td>
                    <td>${row.delta}</td>
                    <td>${row.balanceAfter ?? 'n/a'}</td>
                    <td>${row.reason || 'n/a'}</td>
                </tr>`;
            })
            .join('');
    }

    async function refreshEconomy(event) {
        if (event) {
            event.preventDefault();
        }

        const player = (economyPlayerInput.value || '').trim();
        const params = new URLSearchParams();
        params.set('limit', '60');

        if (player) {
            params.set('player', player);
        }

        economyStateEl.textContent = 'Loading economy rows...';
        try {
            const data = await request(`/api/economy?${params}`);
            renderEconomyRows(data?.items || []);
        } catch (err) {
            economyStateEl.textContent = `Economy load failed: ${err.message}`;
            economyRowsEl.innerHTML = '';
        }
    }

    economyForm.addEventListener('submit', refreshEconomy);
    refreshBtn.addEventListener('click', refreshEconomy);
    setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
    renderEconomyRows([]);
    void refreshEconomy();
    setInterval(() => {
        void refreshEconomy();
    }, API_REFRESH_MS * 3);
})();
