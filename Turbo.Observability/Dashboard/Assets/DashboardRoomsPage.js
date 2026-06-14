(function () {
    const { request, setGlobalStatus, statusAuthMessage } = window.TurboDashboard || {};

    if (!window.TurboDashboard) {
        return;
    }

    const roomTimelineForm = document.getElementById('roomTimelineForm');
    const roomTimelineInput = document.getElementById('roomTimelineInput');
    const roomTimelineMeta = document.getElementById('roomTimelineMeta');
    const roomTimelineRows = document.getElementById('roomTimelineRows');
    const roomTimelineEmpty = document.getElementById('roomTimelineEmpty');
    const refreshBtn = document.getElementById('refreshRoomTimeline');

    function renderRoomTimeline(data) {
        if (!data) {
            roomTimelineMeta.textContent = 'Room not found.';
            roomTimelineRows.innerHTML = '';
            roomTimelineEmpty.style.display = 'block';
            roomTimelineEmpty.textContent = 'No room timeline available.';
            return;
        }

        roomTimelineMeta.textContent = [
            `Room: ${data.room?.roomName || data.room?.name || 'unknown'} (#${data.room?.roomId || data.room?.id})`,
            `players: ${data.room?.roomUsersNow ?? data.room?.usersNow}/${data.room?.roomPlayersMax ?? data.room?.playersMax}`,
            `model: ${data.room?.roomModelName || data.room?.modelName || data.room?.roomModel || 'unknown'}`,
            `owner: ${data.room?.roomOwnerId}`,
        ]
            .filter(Boolean)
            .join(' | ');

        const rows = data.timeline || [];
        roomTimelineRows.innerHTML = rows
            .map((row) => {
                const eventType = row.eventType || row.EventType || 'entry';
                const actor = row.playerName || row.playerId || 'n/a';
                const target = row.targetPlayerName
                    ? `${row.targetPlayerName} (${row.targetPlayerId ?? 'n/a'})`
                    : row.targetPlayerId
                        ? `${row.targetPlayerId}`
                        : '';
                const message = row.message || '';
                const eventClass = eventType === 'chat' ? 'event-chat' : 'event-entry';

                return `<tr>
                    <td>${row.createdAt || row.occurredAt || ''}</td>
                    <td><span class="${eventClass}">${eventType}</span></td>
                    <td>${actor}</td>
                    <td>${target}</td>
                    <td>${message}</td>
                </tr>`;
            })
            .join('');

        roomTimelineEmpty.style.display = rows?.length ? 'none' : 'block';
        roomTimelineEmpty.textContent = rows?.length ? '' : 'No timeline rows in the selected range.';
    }

    async function loadRoomTimeline(event) {
        if (event) {
            event.preventDefault();
        }

        const roomId = (roomTimelineInput.value || '').trim();
        if (!roomId) {
            roomTimelineMeta.textContent = 'Enter a room id.';
            return;
        }

        try {
            const data = await request(`/api/room/${encodeURIComponent(roomId)}?limit=80`);
            renderRoomTimeline(data);
        } catch (err) {
            roomTimelineMeta.textContent = `Room timeline failed: ${err.message}`;
            roomTimelineRows.innerHTML = '';
            roomTimelineEmpty.style.display = 'block';
            roomTimelineEmpty.textContent = 'Unable to load room timeline.';
        }
    }

    roomTimelineForm.addEventListener('submit', loadRoomTimeline);
    refreshBtn.addEventListener('click', loadRoomTimeline);
    setGlobalStatus(statusAuthMessage(), window.TurboDashboard.token ? 'var(--ok)' : 'var(--danger)');
    renderRoomTimeline(null);
})();
