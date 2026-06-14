(function () {
    const API_REFRESH_MS = 10000;
    const token = new URLSearchParams(location.search).get('token') || '';
    const path = window.location.pathname || '/';
    const normalizedPath = path === '/' ? '/overview' : path.replace(/\/+$/, '');

    const statusEl = document.getElementById('globalStatus');
    const viewEl = document.getElementById('globalView');

    const sections = [
        { route: '/overview', label: 'Overview', section: 'overview', key: 'overview' },
        { route: '/investigation', label: 'Investigation', section: 'investigation', key: 'investigation' },
        { route: '/economy', label: 'Economy', section: 'economy', key: 'economy' },
        { route: '/rooms', label: 'Room inspector', section: 'rooms', key: 'rooms' },
        { route: '/packets', label: 'Packet center', section: 'packets', key: 'packets' },
        { route: '/incidents', label: 'Incident center', section: 'incidents', key: 'incidents' },
        { route: '/audit', label: 'Audit feed', section: 'audit', key: 'audit' },
    ];

    const numberFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });
    const decimalFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 });

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function statusAuthMessage() {
        return token ? 'Connected' : 'Unauthorized: open this page with ?token=<your-token>';
    }

    function setGlobalStatus(message, color = 'var(--muted)') {
        if (!statusEl) return;

        statusEl.textContent = message;
        statusEl.style.color = color;
    }

    function resolveSectionByRoute(route) {
        return sections.find((section) => section.route === route) || sections[0];
    }

    function sectionFromCurrentPage() {
        const bodySection = (document.body && document.body.dataset && document.body.dataset.section) || '';
        return sections.find((section) => section.section === bodySection) || resolveSectionByRoute(normalizedPath);
    }

    function withToken(pathValue) {
        if (!pathValue || !token) {
            return pathValue || '';
        }

        const separator = pathValue.includes('?') ? '&' : '?';
        return `${pathValue}${separator}token=${encodeURIComponent(token)}`;
    }

    function renderNavigation() {
        const nav = document.querySelector('[data-dashboard-nav]');
        if (!nav) {
            return;
        }

        const current = sectionFromCurrentPage();
        nav.innerHTML = '';

        sections.forEach((section) => {
            const isActive = section.section === current.section;
            const link = document.createElement('a');
            link.className = `section-link ${isActive ? 'is-active' : ''}`;
            link.href = withToken(section.route);
            link.textContent = section.label;
            link.setAttribute('aria-current', isActive ? 'page' : 'false');
            nav.appendChild(link);
        });
    }

    function setViewLabel() {
        if (!viewEl) {
            return;
        }

        const current = sectionFromCurrentPage();
        viewEl.textContent = `Current view: ${current.label}`;
    }

    function request(pathValue) {
        return fetch(pathValue, {
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

    setGlobalStatus(statusAuthMessage(), token ? 'var(--ok)' : 'var(--danger)');
    renderNavigation();
    setViewLabel();

    window.TurboDashboard = {
        API_REFRESH_MS,
        token,
        numberFormatter,
        decimalFormatter,
        escapeHtml,
        request,
        buildCard,
        setGlobalStatus,
        statusAuthMessage,
    };
})();
