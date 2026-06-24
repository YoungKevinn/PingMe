// groups.js - PingMe MVC
const pmGroups = {
    _members: [],
    _selectedMembers: [],
    _searchResults: [],

    async init() {
        await this.loadGroups();
    },

    async loadGroups() {
        const container = document.getElementById('pm-groups-list');
        if (!container) return;
        container.innerHTML = '<div style="text-align:center;padding:40px;"><div class="mud-progress-circular mud-progress-circular-indeterminate mud-progress-circular-color-primary" style="width:40px;height:40px;margin:0 auto;"><svg viewBox="22 22 44 44"><circle class="mud-progress-circular-circle" cx="44" cy="44" r="20.2" fill="none" stroke-width="3.6"></circle></svg></div></div>';

        try {
            const groups = await pmApi.get('/api/groups') || [];
            if (!groups.length) {
                container.innerHTML = '<div class="mud-alert mud-alert-filled mud-alert-filled-info" style="margin:8px 0;"><div class="mud-alert-message">Bạn chưa có nhóm nào. Hãy tạo nhóm mới!</div></div>';
                return;
            }
            this.renderGroups(groups, container);
        } catch (e) {
            container.innerHTML = '<div class="mud-alert mud-alert-filled mud-alert-filled-error"><div class="mud-alert-message">Không thể tải danh sách nhóm.</div></div>';
        }
    },

    renderGroups(groups, container) {
        let html = '<div class="mud-grid mud-grid-spacing-xs-3">';
        groups.forEach(g => {
            const name = pmEscape(g.name || 'Nhóm');
            const desc = pmEscape(g.description || 'Không có mô tả');
            const memberCount = g.members?.length || g.memberCount || 0;
            const avatarHtml = g.avatarUrl
                ? `<img class="mud-image object-fill object-center mud-elevation-0" src="${pmApi.backendUrl}${g.avatarUrl}" onerror="this.style.display='none'">`
                : `<svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24"><path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg>`;

            html += `
            <div class="mud-grid-item mud-grid-item-xs-12 mud-grid-item-sm-6 mud-grid-item-md-4">
                <div class="mud-paper mud-paper-outlined mud-card">
                    <div class="mud-card-header">
                        <div class="mud-card-header-avatar">
                            <div class="mud-avatar mud-avatar-medium mud-avatar-filled mud-avatar-filled-primary">
                                ${avatarHtml}
                            </div>
                        </div>
                        <div class="mud-card-header-content">
                            <h6 class="mud-typography mud-typography-subtitle1 font-weight-bold">${name}</h6>
                            <span class="mud-typography mud-typography-caption mud-secondary-text">${memberCount} thành viên</span>
                        </div>
                    </div>
                    <div class="mud-card-content">
                        <p class="mud-typography mud-typography-body2 mud-secondary-text">${desc}</p>
                    </div>
                    <div class="mud-card-actions">
                        <button type="button" class="mud-button-root mud-button mud-button-text mud-button-text-default mud-button-size-small mud-ripple" onclick="location.href='/chat/group/${g.id}'">
                            <span class="mud-button-label">
                                <span class="mud-icon-root mud-svg-icon mud-button-icon-start"><svg viewBox="0 0 24 24"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z"/></svg></span>
                                Nhắn tin
                            </span>
                        </button>
                        <button type="button" class="mud-button-root mud-button mud-button-text mud-button-text-default mud-button-size-small mud-ripple" onclick="location.href='/groups/${g.id}'">
                            <span class="mud-button-label">
                                <span class="mud-icon-root mud-svg-icon mud-button-icon-start"><svg viewBox="0 0 24 24"><path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58a.49.49 0 00.12-.61l-1.92-3.32a.488.488 0 00-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.484.484 0 00-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96a.488.488 0 00-.59.22L2.74 8.87a.49.49 0 00.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58a.49.49 0 00-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/></svg></span>
                                Quản lý
                            </span>
                        </button>
                    </div>
                </div>
            </div>`;
        });
        html += '</div>';
        container.innerHTML = html;
    },

    openCreate() {
        this._selectedMembers = [];
        this._searchResults = [];
        const el = document.getElementById('pm-create-group-overlay');
        if (el) {
            el.style.display = 'flex';
            document.getElementById('new-group-name').value = '';
            const descEl = document.getElementById('new-group-desc');
            if (descEl) descEl.value = '';
            this.renderSelectedMembers();
            const results = document.getElementById('pm-member-search-results');
            if (results) {
                results.innerHTML = '';
                results.style.display = 'none';
            }
        }
    },

    closeCreate() {
        const el = document.getElementById('pm-create-group-overlay');
        if (el) el.style.display = 'none';
    },

    async searchMembers() {
        const q = document.getElementById('member-search-input')?.value?.trim();
        if (!q) return;
        const results = await pmApi.get(`/api/search?q=${encodeURIComponent(q)}&type=user`) || {};
        this._searchResults = results.users || results.items || (Array.isArray(results) ? results : []);
        const container = document.getElementById('pm-member-search-results');
        if (!container) return;
        if (!this._searchResults.length) {
            container.innerHTML = '<span class="mud-typography mud-typography-body2 mud-secondary-text">Không tìm thấy người dùng.</span>';
            container.style.display = 'block';
            return;
        }
        const html = this._searchResults.map(u => {
            const name = pmEscape(u.displayName || u.username);
            const already = this._selectedMembers.some(m => m.id === u.id);
            if (already) return '';

            return `
            <div style="display:flex; align-items:center; justify-content:space-between; padding:8px 12px; margin-bottom:6px; border-radius:8px; background:rgba(255,255,255,0.02); border:1px solid var(--mud-palette-divider);">
                <div style="display:flex; align-items:center; gap:10px; min-width:0; flex:1;">
                    <div class="mud-avatar mud-avatar-small mud-avatar-filled mud-avatar-filled-secondary" style="flex-shrink:0;">
                        ${u.avatarUrl ? `<img class="mud-image object-fill object-center mud-elevation-0" src="${pmApi.backendUrl}${u.avatarUrl}" onerror="this.style.display='none'">` : name[0]}
                    </div>
                    <div style="min-width:0; flex:1; display:flex; flex-direction:column; line-height:1.2;">
                        <span style="font-size:0.875rem; font-weight:600; color:var(--mud-palette-text-primary); text-overflow:ellipsis; overflow:hidden; white-space:nowrap;">${name}</span>
                        <span style="font-size:0.75rem; color:var(--mud-palette-text-secondary); text-overflow:ellipsis; overflow:hidden; white-space:nowrap;">@${pmEscape(u.username)}</span>
                    </div>
                </div>
                <button type="button" class="mud-button-root mud-button mud-button-outlined mud-button-outlined-primary mud-button-size-small mud-ripple" 
                        style="flex-shrink:0;"
                        onclick="pmGroups.toggleMember(${u.id}, '${name}', '${pmEscape(u.username)}', '${u.avatarUrl || ''}')">
                    <span class="mud-button-label">Chọn</span>
                </button>
            </div>`;
        }).filter(Boolean).join('');

        if (!html) {
            container.style.display = 'none';
        } else {
            container.innerHTML = html;
            container.style.display = 'block';
        }
    },

    toggleMember(id, name, username, avatarUrl) {
        const idx = this._selectedMembers.findIndex(m => m.id === id);
        if (idx >= 0) {
            this._selectedMembers.splice(idx, 1);
        } else {
            this._selectedMembers.push({ id, displayName: name, username, avatarUrl });
        }
        this.renderSelectedMembers();
        if (this._searchResults.length) this.searchMembers();
    },

    renderSelectedMembers() {
        const container = document.getElementById('pm-selected-members');
        const list = document.getElementById('pm-selected-members-list');
        const count = document.getElementById('pm-selected-count');
        if (!container || !list) return;

        if (!this._selectedMembers.length) {
            container.style.display = 'none';
            return;
        }
        container.style.display = '';
        if (count) count.textContent = this._selectedMembers.length;
        list.innerHTML = this._selectedMembers.map(m => `
            <div style="display:flex; align-items:center; justify-content:space-between; padding:8px 12px; margin-bottom:6px; border-radius:8px; background:rgba(255,255,255,0.02); border:1px solid var(--mud-palette-divider);">
                <div style="display:flex; align-items:center; gap:10px; min-width:0; flex:1;">
                    <div class="mud-avatar mud-avatar-small mud-avatar-filled mud-avatar-filled-primary" style="flex-shrink:0;">
                        ${m.avatarUrl ? `<img class="mud-image object-fill object-center mud-elevation-0" src="${pmApi.backendUrl}${m.avatarUrl}">` : m.displayName[0]}
                    </div>
                    <div style="min-width:0; flex:1; display:flex; flex-direction:column; line-height:1.2;">
                        <span style="font-size:0.875rem; font-weight:600; color:var(--mud-palette-text-primary); text-overflow:ellipsis; overflow:hidden; white-space:nowrap;">${pmEscape(m.displayName)}</span>
                        <span style="font-size:0.75rem; color:var(--mud-palette-text-secondary); text-overflow:ellipsis; overflow:hidden; white-space:nowrap;">@${pmEscape(m.username)}</span>
                    </div>
                </div>
                <button type="button" class="mud-button-root mud-icon-button mud-icon-button-color-error mud-icon-button-size-small mud-ripple" 
                        style="padding:4px; flex-shrink:0;"
                        onclick="pmGroups.toggleMember(${m.id},'${pmEscape(m.displayName)}','${pmEscape(m.username)}','${m.avatarUrl||''}')" title="Xóa">
                    <span class="mud-icon-button-label">
                        <svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24" style="width:18px; height:18px;"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
                    </span>
                </button>
            </div>`).join('');
    },

    async createGroup() {
        const name = document.getElementById('new-group-name')?.value?.trim();
        if (!name) { pmToast.error('Vui lòng nhập tên nhóm'); return; }
        const desc = document.getElementById('new-group-desc')?.value?.trim() || '';
        const memberIds = this._selectedMembers.map(m => m.id);

        const btn = document.getElementById('pm-create-btn');
        if (btn) { btn.disabled = true; btn.querySelector('.mud-button-label').textContent = 'Đang tạo...'; }

        try {
            const body = { name, description: desc, memberIds };
            const r = await pmApi.post('/api/groups', body);
            if (r) {
                pmToast.success('Đã tạo nhóm thành công');
                this.closeCreate();
                await this.loadGroups();
            }
        } finally {
            if (btn) { btn.disabled = false; btn.querySelector('.mud-button-label').textContent = 'Tạo nhóm'; }
        }
    }
};

document.addEventListener('DOMContentLoaded', () => pmGroups.init());
