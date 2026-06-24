const pmSearch = {
    _keyword: '', _type: 'all', _page: 1, _total: 0, _pageSize: 20,
    _timer: null, _result: null,

    onKeyword(v) {
        this._keyword = v;
        clearTimeout(this._timer);
        this._timer = setTimeout(() => this.doSearch(), 400);
    },

    setType(t) {
        this._type = t;
        this._page = 1;
        document.querySelectorAll('.pm-search-types button').forEach(b => b.classList.remove('active'));
        const btn = document.getElementById('type-' + t);
        if (btn) btn.classList.add('active');
        this.doSearch();
    },

    clearFilters() {
        this._keyword = ''; this._type = 'all'; this._page = 1;
        document.getElementById('pm-search-input').value = '';
        document.getElementById('filter-from').value = '';
        document.getElementById('filter-to').value = '';
        document.getElementById('filter-sender').value = '';
        document.getElementById('filter-group').value = '';
        document.getElementById('filter-peer').value = '';
        document.getElementById('filter-severity').value = '';
        this.setType('all');
    },

    async doSearch() {
        const kw = this._keyword.trim();
        const container = document.getElementById('pm-search-results');
        
        const hasFilters = kw || 
                           document.getElementById('filter-from').value ||
                           document.getElementById('filter-to').value ||
                           document.getElementById('filter-sender').value ||
                           document.getElementById('filter-group').value ||
                           document.getElementById('filter-peer').value ||
                           document.getElementById('filter-severity').value;
                           
        if (!hasFilters) {
            container.innerHTML = `<div class="pm-search-empty">
                <svg class="mud-icon-root mud-svg-icon mud-icon-size-large" viewBox="0 0 24 24"><path d="M21 15.46l-5.27-.61-2.52 2.52c-2.83-1.44-5.15-3.75-6.59-6.59l2.53-2.53L8.54 3H3.03C2.45 13.18 10.82 21.55 21 20.97v-5.51z"/></svg>
                <span>Nhập từ khóa để tìm kiếm...</span>
            </div>`;
            return;
        }

        container.innerHTML = `<div class="pm-search-loading">
            <div class="mud-progress-circular mud-progress-circular-indeterminate mud-progress-circular-color-primary" style="height: 40px; width: 40px;"><svg class="mud-progress-circular-svg" viewBox="22 22 44 44"><circle class="mud-progress-circular-circle" cx="44" cy="44" r="20.2" fill="none" stroke-width="3.6"></circle></svg></div>
            <span>Đang tìm kiếm...</span>
        </div>`;

        const params = new URLSearchParams({ keyword: kw, page: this._page, pageSize: this._pageSize });
        if (this._type !== 'all') params.set('type', this._type);
        const from = document.getElementById('filter-from').value;
        const to = document.getElementById('filter-to').value;
        const sender = document.getElementById('filter-sender').value;
        const group = document.getElementById('filter-group').value;
        const peer = document.getElementById('filter-peer').value;
        const severity = document.getElementById('filter-severity').value;
        
        if (from) params.set('fromDate', from);
        if (to) params.set('toDate', to);
        if (sender) params.set('senderId', sender);
        if (group) params.set('groupId', group);
        if (peer) params.set('peerUserId', peer);
        if (severity) params.set('severity', severity);

        try {
            const r = await pmApi.get('/api/search/global?' + params.toString());
            this._result = r;
            this._total = r?.total || 0;
            this.render(r, false);
        } catch (e) {
            container.innerHTML = '<div class="pm-search-empty"><span>Đã xảy ra lỗi khi tìm kiếm.</span></div>';
        }
    },

    async loadMore() {
        this._page++;
        const params = new URLSearchParams({ keyword: this._keyword.trim(), page: this._page, pageSize: this._pageSize });
        if (this._type !== 'all') params.set('type', this._type);
        const from = document.getElementById('filter-from').value;
        const to = document.getElementById('filter-to').value;
        const sender = document.getElementById('filter-sender').value;
        const group = document.getElementById('filter-group').value;
        const peer = document.getElementById('filter-peer').value;
        const severity = document.getElementById('filter-severity').value;
        
        if (from) params.set('fromDate', from);
        if (to) params.set('toDate', to);
        if (sender) params.set('senderId', sender);
        if (group) params.set('groupId', group);
        if (peer) params.set('peerUserId', peer);
        if (severity) params.set('severity', severity);

        const r = await pmApi.get('/api/search/global?' + params.toString());
        if (r?.items) {
            this._result.items = [...(this._result.items || []), ...r.items];
            this._result.total = r.total;
            this.render(this._result, false);
        }
    },

    _icons: {
        message: '<path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z"/>',
        user: '<path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>',
        group: '<path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/>',
        finding: '<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z"/>',
        ioc: '<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>',
        snippet: '<path d="M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z"/>',
        task: '<path d="M19 3H14.82C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14l-5-5 1.41-1.41L14 14.17l7.59-7.59L23 8l-9 9z"/>',
        attachment: '<path d="M16.5 6v11.5c0 2.21-1.79 4-4 4s-4-1.79-4-4V5c0-1.38 1.12-2.5 2.5-2.5s2.5 1.12 2.5 2.5v10.5c0 .55-.45 1-1 1s-1-.45-1-1V6H10v9.5c0 1.38 1.12 2.5 2.5 2.5s2.5-1.12 2.5-2.5V5c0-2.21-1.79-4-4-4S7 2.79 7 5v12.5c0 3.04 2.46 5.5 5.5 5.5s5.5-2.46 5.5-5.5V6h-1.5z"/>'
    },

    _typeLabels: { message:'Tin nhắn', user:'Người dùng', group:'Nhóm', finding:'Finding', ioc:'IOC', snippet:'Snippet', task:'Công việc', attachment:'File' },
    _sevClass: { Critical:'pm-severity-critical pm-severity-chip', High:'pm-severity-high pm-severity-chip', Medium:'pm-severity-medium pm-severity-chip', Low:'pm-severity-low pm-severity-chip', Info:'pm-severity-info pm-severity-chip' },

    render(r, append) {
        const container = document.getElementById('pm-search-results');
        const items = r?.items || [];

        // Update type button counts
        if (r?.typeCounts) {
            Object.entries(r.typeCounts).forEach(([type, count]) => {
                const btn = document.getElementById('type-' + type);
                if (btn) {
                    const existing = btn.querySelector('span');
                    if (existing) existing.textContent = count;
                    else btn.innerHTML += `<span>${count}</span>`;
                }
            });
        }

        if (!items.length) {
            container.innerHTML = `<div class="pm-search-empty">
                <svg class="mud-icon-root mud-svg-icon mud-icon-size-large" viewBox="0 0 24 24"><path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zM9.5 14C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
                <span>Không có kết quả nào</span>
            </div>`;
            return;
        }

        const iconPath = (type) => this._icons[type] || this._icons.message;
        const sevChip = (s) => s ? `<span class="${this._sevClass[s] || 'pm-severity-chip pm-severity-info'}">${pmEscape(s)}</span>` : '';

        let html = '<div class="pm-search-result-list">';
        items.forEach(item => {
            const typeLabel = this._typeLabels[item.type] || item.type;
            const time = item.createdAt ? new Date(item.createdAt).toLocaleString('vi-VN', {day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}) : '';
            const avatarHtml = item.avatarUrl
                ? `<div class="mud-avatar mud-avatar-small mud-avatar-filled mud-avatar-filled-primary"><img src="${pmApi.backendUrl}${item.avatarUrl}" class="mud-image" onerror="this.style.display='none'"></div>` : '';

            const contextParts = [];
            if (item.senderName) contextParts.push(`Từ: ${pmEscape(item.senderName)}`);
            if (item.groupName) contextParts.push(`Nhóm: ${pmEscape(item.groupName)}`);
            if (item.peerDisplayName) contextParts.push(`DM với: ${pmEscape(item.peerDisplayName)}`);
            const context = contextParts.join(' · ');

            const openUrl = (() => {
                if (item.type === 'user') return `/chat?peerId=${item.id}`;
                if (item.type === 'group') return `/chat?groupId=${item.id}`;
                if (item.type === 'finding') return `/pentest`;
                if (item.type === 'ioc') return `/ioc`;
                if (item.type === 'snippet') return `/snippets`;
                if (item.type === 'task') return `/tasks`;
                if (item.groupId) return `/chat?groupId=${item.groupId}&messageId=${item.messageId || item.id}`;
                if (item.peerUserId) return `/chat?peerId=${item.peerUserId}&messageId=${item.messageId || item.id}`;
                return '#';
            })();

            const fileBtn = item.type === 'attachment' && item.fileUrl
                ? `<button type="button" onclick="window.open('${pmApi.backendUrl}${item.fileUrl}', '_blank')" class="mud-button-root mud-button mud-button-outlined mud-button-outlined-primary mud-button-size-small mud-ripple"><span class="mud-button-label"><span class="mud-icon-root mud-svg-icon mud-button-icon-start"><svg viewBox="0 0 24 24"><path d="M19 19H5V5h7V3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2v-7h-2v7zM14 3v2h3.59l-9.83 9.83 1.41 1.41L19 6.41V10h2V3h-7z"/></svg></span>Mở file</span></button>` : '';

            const openBtnText = item.type === 'message' || item.type === 'attachment' ? 'Xem tin nhắn' : 'Mở';
            
            const payloadPreviewHtml = item.type === 'finding' && item.payloadPreview
                ? `<pre class="pm-finding-search-payload">${pmEscape(item.payloadPreview)}</pre>` : '';
                
            const findingMetaHtml = item.type === 'finding' && item.endpoint
                ? `<div class="pm-finding-search-meta">Endpoint: ${pmEscape(item.endpoint)}</div>` : '';

            const snippetHtml = item.snippet && !(item.type === 'finding' && item.snippet === item.payloadPreview)
                ? `<p class="pm-result-snippet">${pmEscape(item.snippet)}</p>` : '';

            html += `
            <article class="pm-search-result-card type-${item.type}">
                <div class="pm-search-result-icon">
                    <svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24">${iconPath(item.type)}</svg>
                </div>
                <div class="pm-search-result-main">
                    <div class="pm-result-topline">
                        <span class="pm-result-type">${typeLabel}</span>
                        ${time ? `<span class="pm-result-time">${time}</span>` : ''}
                    </div>
                    <div class="pm-result-title-row">
                        ${avatarHtml}
                        <h3>${pmEscape(item.title || item.name || '')}</h3>
                        ${item.language ? `<span class="pm-meta-chip">${pmEscape(item.language)}</span>` : ''}
                        ${sevChip(item.severity)}
                        ${item.status ? `<span class="pm-status-chip">${pmEscape(item.status)}</span>` : ''}
                    </div>
                    ${context ? `<div class="pm-result-context">${context}</div>` : ''}
                    ${findingMetaHtml}
                    ${payloadPreviewHtml}
                    ${snippetHtml}
                    ${item.fileName ? `<div class="pm-result-file"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M16.5 6v11.5c0 2.21-1.79 4-4 4s-4-1.79-4-4V5c0-1.38 1.12-2.5 2.5-2.5s2.5 1.12 2.5 2.5v10.5c0 .55-.45 1-1 1s-1-.45-1-1V6H10v9.5c0 1.38 1.12 2.5 2.5 2.5s2.5-1.12 2.5-2.5V5c0-2.21-1.79-4-4-4S7 2.79 7 5v12.5c0 3.04 2.46 5.5 5.5 5.5s5.5-2.46 5.5-5.5V6h-1.5z"/></svg><span>${pmEscape(item.fileName)}</span>${item.fileContentType ? `<small>${pmEscape(item.fileContentType)}</small>` : ''}</div>` : ''}
                </div>
                <div class="pm-result-actions">
                    ${fileBtn}
                    <button type="button" onclick="window.location.href='${openUrl}'" class="mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-size-small mud-ripple"><span class="mud-button-label"><span class="mud-icon-root mud-svg-icon mud-button-icon-start"><svg viewBox="0 0 24 24"><path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6z"/></svg></span>${openBtnText}</span></button>
                </div>
            </article>`;
        });
        html += '</div>';

        if (r.total > this._page * this._pageSize) {
            html += `<div class="pm-search-more">
                <button type="button" class="mud-button-root mud-button mud-button-outlined mud-button-outlined-primary mud-ripple" onclick="pmSearch.loadMore()">
                    <span class="mud-button-label">Tải thêm</span>
                </button>
            </div>`;
        }

        container.innerHTML = html;
    }
};

document.addEventListener('DOMContentLoaded', async () => {
    // Populate filters from users and groups
    try {
        const users = await pmApi.get('/api/users');
        const groups = await pmApi.get('/api/groups');
        
        const senderSelect = document.getElementById('filter-sender');
        const peerSelect = document.getElementById('filter-peer');
        const groupSelect = document.getElementById('filter-group');

        if (users?.length) {
            users.forEach(u => {
                senderSelect.add(new Option(u.displayName || u.username, u.id));
                peerSelect.add(new Option(u.displayName || u.username, u.id));
            });
        }
        
        if (groups?.length) {
            groups.forEach(g => {
                groupSelect.add(new Option(g.name, g.id));
            });
        }
    } catch(e) {}
});
