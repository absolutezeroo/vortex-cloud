(function () {
    const {
        request,
        escapeHtml,
        setGlobalStatus,
        statusAuthMessage,
        API_REFRESH_MS,
    } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const auditRowsEl = document.getElementById('auditRows');
    const auditLoadingEl = document.getElementById('auditLoading');
    const refreshBtn = document.getElementById('refreshAudit');

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

    async function refreshAudit() {
        try {
            const data = await request('/api/audit?limit=50');
            auditLoadingEl.style.display = 'none';
            renderAuditRows(data?.items || []);
            setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
        } catch (err) {
            auditLoadingEl.style.display = 'block';
            auditLoadingEl.textContent = `Audit load failed: ${err.message}`;
            setGlobalStatus(`Audit load failed: ${err.message}`, 'var(--danger)');
        }
    }

    refreshBtn.addEventListener('click', refreshAudit);
    void refreshAudit();
    setInterval(() => {
        void refreshAudit();
    }, API_REFRESH_MS);
})();
