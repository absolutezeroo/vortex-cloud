(function () {
    const {
        request,
        buildCard,
        setGlobalStatus,
        statusAuthMessage,
        API_REFRESH_MS,
        decimalFormatter,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const packetCardsEl = document.getElementById('packetCards');
    const packetOverviewEl = document.getElementById('packetOverview');
    const packetTopOperationsRowsEl = document.getElementById('packetTopOperationsRows');
    const packetTopFailedRowsEl = document.getElementById('packetTopFailedRows');
    const refreshBtn = document.getElementById('refreshPackets');

    async function refreshPacketStats() {
        try {
            const data = await request('/api/packet-stats');
            packetCardsEl.innerHTML = '';
            packetCardsEl.appendChild(
                buildCard('Packets / second', decimalFormatter.format(data?.packetsPerSecond || 0))
            );
            packetCardsEl.appendChild(
                buildCard('Errors / minute', decimalFormatter.format(data?.errorsPerMinute || 0))
            );
            packetCardsEl.appendChild(
                buildCard('Latency p50', `${decimalFormatter.format(data?.latencyP50Ms || 0)} ms`)
            );
            packetCardsEl.appendChild(
                buildCard('Latency p95', `${decimalFormatter.format(data?.latencyP95Ms || 0)} ms`)
            );

            packetOverviewEl.textContent = `Captured ${data?.topOperations?.length || 0} operation buckets.`;

            const topOperations = data?.topOperations || [];
            const topFailed = data?.topFailedOperations || [];

            if (!topOperations.length) {
                packetTopOperationsRowsEl.innerHTML =
                    '<tr><td colspan="2" class="muted">No active packet operations.</td></tr>';
            } else {
                packetTopOperationsRowsEl.innerHTML = topOperations
                    .map((operation) => {
                        return `<tr>
                            <td><code>${operation.operation || 'n/a'}</code></td>
                            <td>${decimalFormatter.format(operation.packetsPerMinute || 0)}</td>
                        </tr>`;
                    })
                    .join('');
            }

            if (!topFailed.length) {
                packetTopFailedRowsEl.innerHTML =
                    '<tr><td colspan="2" class="muted">No failed packet operations.</td></tr>';
            } else {
                packetTopFailedRowsEl.innerHTML = topFailed
                    .map((operation) => {
                        return `<tr>
                            <td><code>${operation.operation || 'n/a'}</code></td>
                            <td>${decimalFormatter.format(operation.packetsPerMinute || 0)}</td>
                        </tr>`;
                    })
                    .join('');
            }
        } catch (err) {
            packetOverviewEl.textContent = `Packet stats failed: ${err.message}`;
            packetTopOperationsRowsEl.innerHTML =
                '<tr><td colspan="2" class="muted">Unable to load packet data.</td></tr>';
            packetTopFailedRowsEl.innerHTML =
                '<tr><td colspan="2" class="muted">Unable to load packet data.</td></tr>';
            setGlobalStatus(`Packet stats failed: ${err.message}`, 'var(--danger)');
        }
    }

    refreshBtn.addEventListener('click', refreshPacketStats);
    setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
    void refreshPacketStats();
    setInterval(() => {
        void refreshPacketStats();
    }, API_REFRESH_MS);
})();
