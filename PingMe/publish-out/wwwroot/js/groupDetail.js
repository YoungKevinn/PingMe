// Group detail / management page — ported from GroupDetail.razor
(function () {
    const cfg = window.__gd || { groupId: 0, myId: 0 };
    const GID = cfg.groupId;
    const MY = cfg.myId;

    let _group = null;
    let _activeTab = 'overview';
    let _editing = false;
    let _mediaFilter = 'all';
    let _attachments = [];
    let _searchResults = [];

    const $ = id => document.getElementById(id);
    const esc = s => (window.pmEscape ? pmEscape(s) : (s == null ? '' : String(s)));

    function abs(url) {
        if (!url) return '';
        if (/^https?:/i.test(url)) return url;
        return (pmApi.backendUrl || '') + url;
    }
    function initial(name) { return (name || '?').trim().charAt(0).toUpperCase() || '?'; }
    function fmtSize(b) {
        if (b > 1048576) return (b / 1048576).toFixed(1) + ' MB';
        if (b > 1024) return (b / 1024).toFixed(0) + ' KB';
        return (b || 0) + ' B';
    }
    function fmtDate(iso) {
        const d = new Date(iso); if (isNaN(d)) return '';
        return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    }
    function fmtDateTime(iso) {
        const d = new Date(iso); if (isNaN(d)) return '';
        return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }) + ' ' +
               d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }
    function roleVi(r) { return r === 'Admin' ? '👑 Admin' : r === 'CoAdmin' ? '⭐ Co-Admin' : 'Thành viên'; }

    function myRole() {
        const m = (_group?.members || []).find(x => x.userId === MY);
        return m ? m.role : null;
    }
    const isAdmin     = () => ['Admin', 'CoAdmin'].includes(myRole());
    const isRealAdmin = () => myRole() === 'Admin';
    const isCreator   = () => _group && _group.createdByUserId === MY;

    async function load() {
        _group = await pmApi.get('/api/groups/' + GID);
        render();
    }

    async function setTab(tab) {
        _activeTab = tab;
        renderTabs();
        renderBody();
        if (tab === 'media') await loadAttachments();
    }

    async function loadAttachments() {
        const body = $('gd-body');
        _attachments = await pmApi.get(`/api/messages/attachments?groupId=${GID}&type=${_mediaFilter}&limit=120`) || [];
        renderBody();
    }

    async function setMediaFilter(f) { _mediaFilter = f; await loadAttachments(); }

    // ── Actions ───────────────────────────────────────────────
    function startEdit() { _editing = true; render(); }
    function cancelEdit() { _editing = false; render(); }

    async function save() {
        const name = ($('gd-edit-name')?.value || '').trim();
        const desc = ($('gd-edit-desc')?.value || '').trim();
        if (!name) { pmToast.warn('Tên nhóm không được rỗng.'); return; }
        const r = await pmApi.put('/api/groups/' + GID, { name, description: desc });
        if (r !== null) { pmToast.success('Cập nhật nhóm thành công!'); _editing = false; await load(); }
        else pmToast.error('Cập nhật nhóm thất bại.');
    }

    async function searchAddMembers() {
        const term = $('gd-add-search')?.value || '';
        const res = await pmApi.get('/api/search?q=' + encodeURIComponent(term) + '&limit=50');
        const memberIds = new Set((_group?.members || []).map(x => x.userId));
        _searchResults = ((res && res.users) || []).filter(u => !memberIds.has(u.id));
        renderBody();
    }

    async function addMember(userId) {
        const r = await pmApi.post('/api/groups/' + GID + '/members', { userId });
        if (r !== null) { pmToast.success('Thêm thành viên thành công!'); _searchResults = []; await load(); }
        else pmToast.error('Thêm thành viên thất bại.');
    }

    async function kick(userId) {
        const r = await pmApi.delete('/api/groups/' + GID + '/members/' + userId);
        if (r !== null) { pmToast.success('Đã xóa thành viên khỏi nhóm.'); await load(); }
        else pmToast.error('Xóa thành viên thất bại.');
    }

    async function updateRole(userId, role) {
        const r = await pmApi.patch('/api/groups/' + GID + '/members/' + userId + '/role', { role });
        if (r !== null) { pmToast.success('Đã cập nhật quyền thành viên.'); await load(); }
        else pmToast.error('Cập nhật quyền thất bại.');
    }

    async function leave() {
        const r = await pmApi.post('/api/groups/' + GID + '/leave', {});
        if (r !== null) { pmToast.success('Đã rời nhóm.'); window.location.href = '/groups'; }
        else pmToast.error('Rời nhóm thất bại.');
    }

    async function del() {
        const r = await pmApi.delete('/api/groups/' + GID);
        if (r !== null) { pmToast.success('Đã giải tán nhóm.'); window.location.href = '/groups'; }
        else pmToast.error('Giải tán nhóm thất bại.');
    }

    // ── Render ─────────────────────────────────────────────────
    function render() {
        const root = $('gd-root');
        if (!root) return;
        if (!_group) { root.innerHTML = '<div class="mud-alert mud-alert-filled mud-alert-filled-error">Không tìm thấy nhóm</div>'; return; }

        const g = _group;
        const memberCount = (g.members || []).length;
        const avatarInner = g.avatarUrl
            ? `<img src="${esc(abs(g.avatarUrl))}" onerror="this.style.display='none'">`
            : `<svg viewBox="0 0 24 24" style="width:36px;height:36px;fill:currentColor;"><path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg>`;

        const titleBlock = _editing
            ? `<div class="pm-edit-name"><input id="gd-edit-name" placeholder="Tên nhóm" value="${esc(g.name)}"></div>`
            : `<h1>${esc(g.name)}</h1>`;

        let heroActions = `<a href="/chat/group/${GID}" class="pm-gd-btn filled"><svg viewBox="0 0 24 24"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z"/></svg> Nhắn tin</a>`;
        if (isAdmin()) {
            heroActions += _editing
                ? `<button class="pm-gd-iconbtn success" title="Lưu" onclick="gdPage.save()"><svg viewBox="0 0 24 24"><path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"/></svg></button>
                   <button class="pm-gd-iconbtn" title="Hủy" onclick="gdPage.cancelEdit()"><svg viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg></button>`
                : `<button class="pm-gd-iconbtn" title="Chỉnh sửa nhóm" onclick="gdPage.startEdit()"><svg viewBox="0 0 24 24"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg></button>`;
        }

        root.innerHTML = `
        <div class="pm-group-hero">
            <div class="pm-hero-gradient"></div>
            <div class="pm-hero-content">
                <a href="/groups" class="pm-gd-iconbtn pm-back-btn" title="Quay lại"><svg viewBox="0 0 24 24"><path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/></svg></a>
                <div class="pm-group-avatar-wrap"><div class="pm-group-avatar">${avatarInner}</div></div>
                <div class="pm-group-title-block">
                    ${titleBlock}
                    <div class="pm-group-subtitle">
                        <span>${memberCount} thành viên</span><span>•</span>
                        <span>Tạo ngày ${fmtDate(g.createdAt)}</span>
                    </div>
                </div>
                <div class="pm-hero-actions">${heroActions}</div>
            </div>
        </div>
        <div class="pm-tabs" id="gd-tabs"></div>
        <div id="gd-body"></div>`;

        renderTabs();
        renderBody();
    }

    function renderTabs() {
        const el = $('gd-tabs');
        if (!el) return;
        const tabs = [['overview', 'Tổng quan'], ['members', 'Thành viên'], ['media', 'Ảnh & tệp'], ['danger', 'Vùng nguy hiểm']];
        el.innerHTML = tabs.map(([k, label]) =>
            `<button type="button" class="pm-tab-btn ${_activeTab === k ? 'active' : ''}" onclick="gdPage.setTab('${k}')">${label}</button>`
        ).join('');
    }

    function renderBody() {
        const body = $('gd-body');
        if (!body || !_group) return;
        if (_activeTab === 'overview') body.innerHTML = renderOverview();
        else if (_activeTab === 'members') body.innerHTML = renderMembers();
        else if (_activeTab === 'media') body.innerHTML = renderMedia();
        else if (_activeTab === 'danger') body.innerHTML = renderDanger();
    }

    function renderOverview() {
        const g = _group;
        const members = g.members || [];
        const descBlock = _editing
            ? `<textarea id="gd-edit-desc" class="pm-edit-desc" placeholder="Nhập mô tả nhóm...">${esc(g.description || '')}</textarea>`
            : `<p class="pm-description">${esc(g.description) || 'Nhóm này chưa có mô tả.'}</p>`;
        const online = members.filter(m => m.isOnline).length;
        const admins = members.filter(m => m.role === 'Admin' || m.role === 'CoAdmin').length;
        return `
        <div class="pm-content-grid">
            <div class="pm-panel">
                <div class="pm-panel-title"><svg viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"/></svg><span>Thông tin nhóm</span></div>
                ${descBlock}
            </div>
            <div class="pm-panel">
                <div class="pm-panel-title"><svg viewBox="0 0 24 24"><path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z"/></svg><span>Tổng quan nhanh</span></div>
                <div class="pm-stats-grid">
                    <div class="pm-stat-card"><div class="pm-stat-number">${members.length}</div><div class="pm-stat-label">Thành viên</div></div>
                    <div class="pm-stat-card"><div class="pm-stat-number">${online}</div><div class="pm-stat-label">Đang online</div></div>
                    <div class="pm-stat-card"><div class="pm-stat-number">${admins}</div><div class="pm-stat-label">Quản trị</div></div>
                </div>
            </div>
        </div>`;
    }

    function memberAvatar(name, url) {
        return `<div class="pm-mini-avatar">${url ? `<img src="${esc(abs(url))}" onerror="this.style.display='none'">` : ''}<span style="${url ? 'position:absolute;' : ''}">${esc(initial(name))}</span></div>`;
    }

    function renderMembers() {
        const g = _group;
        const canAdd = isAdmin();
        let addBox = '';
        if (canAdd) {
            addBox = `
            <div class="pm-add-member-box">
                <input id="gd-add-search" placeholder="Tìm theo tên, username hoặc để trống để xem danh bạ" onkeydown="if(event.key==='Enter')gdPage.searchAddMembers()">
                <button class="pm-gd-btn filled" onclick="gdPage.searchAddMembers()"><svg viewBox="0 0 24 24"><path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5z"/></svg> Tìm</button>
            </div>`;
            if (_searchResults.length) {
                addBox += `<div class="pm-search-results">` + _searchResults.map(u => `
                    <div class="pm-user-row">
                        ${memberAvatar(u.displayName, u.avatarUrl)}
                        <div class="pm-user-main">
                            <div class="pm-user-name">${esc(u.displayName)}</div>
                            <div class="pm-user-meta">@${esc(u.username)}${u.jobTitle ? ' • ' + esc(u.jobTitle) : ''}</div>
                        </div>
                        <button class="pm-gd-btn outlined-pri" onclick="gdPage.addMember(${u.id})"><svg viewBox="0 0 24 24"><path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg> Thêm</button>
                    </div>`).join('') + `</div>`;
            }
        } else {
            addBox = `<div class="mud-alert mud-alert-filled mud-alert-filled-info" style="margin-top:8px;"><div class="mud-alert-message">Chỉ Admin hoặc Co-Admin mới có thể thêm thành viên.</div></div>`;
        }

        const sorted = [...(g.members || [])].sort((a, b) => {
            const rank = r => r === 'Admin' ? 2 : r === 'CoAdmin' ? 1 : 0;
            if (rank(b.role) !== rank(a.role)) return rank(b.role) - rank(a.role);
            return (a.displayName || '').localeCompare(b.displayName || '');
        });

        const list = sorted.map(m => {
            const canManage = isRealAdmin() && m.role !== 'Admin';
            const ctrl = canManage ? `
                <select class="pm-role-select" onchange="gdPage.updateRole(${m.userId}, this.value)">
                    <option value="Member" ${m.role === 'Member' ? 'selected' : ''}>Member</option>
                    <option value="CoAdmin" ${m.role === 'CoAdmin' ? 'selected' : ''}>CoAdmin</option>
                </select>
                <button class="pm-gd-iconbtn error" title="Xóa khỏi nhóm" onclick="gdPage.kick(${m.userId})"><svg viewBox="0 0 24 24"><path d="M14 8c0-2.21-1.79-4-4-4S6 5.79 6 8s1.79 4 4 4 4-1.79 4-4zm3 2v2h6v-2h-6zM2 18v2h16v-2c0-2.66-5.33-4-8-4s-8 1.34-8 4z"/></svg></button>` : '';
            return `
            <div class="pm-member-card">
                ${memberAvatar(m.displayName, m.avatarUrl)}
                <div class="pm-member-info">
                    <div class="pm-member-name">${esc(m.displayName)}</div>
                    <div class="pm-member-meta">
                        <span>${roleVi(m.role)}</span>
                        ${m.isOnline ? '<span class="pm-online-dot">● Online</span>' : '<span class="pm-offline-dot">● Ngoại tuyến</span>'}
                    </div>
                </div>
                ${ctrl}
            </div>`;
        }).join('');

        return `
        <div class="pm-panel">
            <div class="pm-panel-title"><svg viewBox="0 0 24 24"><path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg><span>Thêm thành viên</span></div>
            ${addBox}
        </div>
        <div class="pm-section-heading"><span>Danh sách thành viên</span><small>${(g.members || []).length} người</small></div>
        <div class="pm-member-list">${list}</div>`;
    }

    function renderMedia() {
        const imgs = _attachments.filter(a => a.isImage);
        const files = _attachments.filter(a => !a.isImage);
        let inner;
        if (!_attachments.length) {
            inner = `<div class="pm-empty-media"><svg viewBox="0 0 24 24"><path d="M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM14 13v4h-4v-4H7l5-5 5 5h-3z"/></svg><p>Chưa có ảnh hoặc tệp nào trong nhóm.</p><small>Các file gửi trong chat nhóm sẽ xuất hiện ở đây.</small></div>`;
        } else {
            let html = '';
            if (imgs.length && (_mediaFilter === 'all' || _mediaFilter === 'images')) {
                html += `<div class="pm-section-heading"><span>Ảnh</span><small>${imgs.length} ảnh</small></div><div class="pm-image-grid">` +
                    imgs.map(i => `<a href="${esc(abs(i.fileUrl))}" target="_blank" rel="noopener" class="pm-image-tile"><img src="${esc(abs(i.fileUrl))}" alt="${esc(i.fileName)}"><div class="pm-image-overlay"><span>${esc(i.fileName)}</span></div></a>`).join('') + `</div>`;
            }
            if (files.length && (_mediaFilter === 'all' || _mediaFilter === 'files')) {
                html += `<div class="pm-section-heading"><span>Tệp</span><small>${files.length} tệp</small></div><div class="pm-file-list">` +
                    files.map(f => `
                    <div class="pm-file-row">
                        <div class="pm-file-icon"><svg viewBox="0 0 24 24"><path d="M6 2c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6H6zm7 7V3.5L18.5 9H13z"/></svg></div>
                        <div class="pm-file-main">
                            <div class="pm-file-name">${esc(f.fileName)}</div>
                            <div class="pm-file-meta">${fmtSize(f.fileSize)} • ${esc(f.mimeType)} • ${fmtDateTime(f.createdAt)}</div>
                            <div class="pm-file-sender">Gửi bởi ${esc(f.senderDisplayName)}</div>
                        </div>
                        <div class="pm-file-actions">
                            <a href="${esc(abs(f.fileUrl))}" target="_blank" rel="noopener" class="pm-link-btn">Xem</a>
                            <a href="${esc(abs(f.fileUrl))}" download class="pm-link-btn primary">Tải xuống</a>
                        </div>
                    </div>`).join('') + `</div>`;
            }
            inner = html;
        }
        return `
        <div class="pm-panel">
            <div class="pm-media-header">
                <div class="pm-panel-title"><svg viewBox="0 0 24 24"><path d="M16.5 6v11.5c0 2.21-1.79 4-4 4s-4-1.79-4-4V5c0-1.38 1.12-2.5 2.5-2.5S13.5 3.62 13.5 5v10.5c0 .55-.45 1-1 1s-1-.45-1-1V6H10v9.5c0 1.38 1.12 2.5 2.5 2.5s2.5-1.12 2.5-2.5V5c0-2.21-1.79-4-4-4S7 2.79 7 5v12.5c0 3.04 2.46 5.5 5.5 5.5s5.5-2.46 5.5-5.5V6h-1.5z"/></svg><span>Ảnh & tệp đã upload</span></div>
                <div class="pm-media-filter">
                    <button class="pm-filter-btn ${_mediaFilter === 'all' ? 'active' : ''}" onclick="gdPage.setMediaFilter('all')">Tất cả</button>
                    <button class="pm-filter-btn ${_mediaFilter === 'images' ? 'active' : ''}" onclick="gdPage.setMediaFilter('images')">Ảnh</button>
                    <button class="pm-filter-btn ${_mediaFilter === 'files' ? 'active' : ''}" onclick="gdPage.setMediaFilter('files')">Tệp</button>
                </div>
            </div>
            ${inner}
        </div>`;
    }

    function renderDanger() {
        let actions = `<button class="pm-gd-btn outlined-warn" onclick="gdPage.confirmLeave()"><svg viewBox="0 0 24 24"><path d="M10.09 15.59L11.5 17l5-5-5-5-1.41 1.41L12.67 11H3v2h9.67l-2.58 2.59zM19 3H5c-1.11 0-2 .9-2 2v4h2V5h14v14H5v-4H3v4c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z"/></svg> Rời nhóm</button>`;
        if (isCreator()) {
            actions += `<button class="pm-gd-btn outlined-err" onclick="gdPage.confirmDelete()"><svg viewBox="0 0 24 24"><path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/></svg> Giải tán nhóm</button>`;
        }
        return `
        <div class="pm-danger-panel">
            <div class="pm-panel-title danger"><svg viewBox="0 0 24 24"><path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"/></svg><span>Vùng nguy hiểm</span></div>
            <p class="pm-danger-desc">Các thao tác bên dưới có thể ảnh hưởng trực tiếp đến quyền truy cập nhóm.</p>
            <div class="pm-danger-actions">${actions}</div>
        </div>`;
    }

    function confirmLeave() { if (confirm('Bạn có chắc chắn muốn rời nhóm này?')) leave(); }
    function confirmDelete() { if (confirm('Giải tán nhóm sẽ xóa toàn bộ nhóm. Bạn có chắc chắn?')) del(); }

    window.gdPage = {
        setTab, setMediaFilter, startEdit, cancelEdit, save,
        searchAddMembers, addMember, kick, updateRole,
        confirmLeave, confirmDelete, leave, del
    };

    document.addEventListener('DOMContentLoaded', load);
})();
