const pmDialogs = {
    // ==== Add Member Dialog ====
    _addMemberContext: null,
    async openAddMember(groupId, groupName) {
        this._addMemberContext = { groupId, friends: [] };
        document.getElementById('pm-add-member-group-name').textContent = groupName;
        document.getElementById('pm-add-member-group-name').style.display = groupName ? '' : 'none';
        document.getElementById('pm-add-member-dialog').style.display = 'flex';
        
        document.getElementById('pm-add-member-loading').style.display = 'flex';
        document.getElementById('pm-add-member-empty').style.display = 'none';
        document.getElementById('pm-add-member-content').style.display = 'none';
        
        try {
            const friends = await pmApi.get('/api/friends') || [];
            const group = await pmApi.get(`/api/groups/${groupId}`);
            const memberIds = new Set((group?.members || []).map(m => m.userId));
            this._addMemberContext.friends = friends.filter(f => !memberIds.has(f.userId || f.id));
            
            document.getElementById('pm-add-member-loading').style.display = 'none';
            if (this._addMemberContext.friends.length === 0) {
                document.getElementById('pm-add-member-empty').style.display = 'flex';
            } else {
                document.getElementById('pm-add-member-content').style.display = '';
                this.filterAddMember('');
            }
        } catch (e) {
            pmToast.error('Không tải được danh sách bạn bè');
            this.closeAddMember();
        }
    },
    closeAddMember() {
        document.getElementById('pm-add-member-dialog').style.display = 'none';
        this._addMemberContext = null;
    },
    filterAddMember(q) {
        if (!this._addMemberContext) return;
        const ql = q.toLowerCase();
        const filtered = this._addMemberContext.friends.filter(f => {
            const name = (f.displayName || f.username || '').toLowerCase();
            return name.includes(ql);
        });
        
        const list = document.getElementById('pm-add-member-list');
        list.innerHTML = filtered.map(f => {
            const name = pmEscape(f.displayName || f.username);
            const initial = name.charAt(0).toUpperCase();
            return `
                <button type="button" class="pm-member-picker-row" onclick="pmDialogs.selectAddMember(${f.userId || f.id})">
                    <div style="width:32px;height:32px;flex-shrink:0;border-radius:50%;display:flex;align-items:center;justify-content:center;background:var(--mud-palette-primary);color:white;font-size:0.875rem;font-weight:700;overflow:hidden;">
                        ${f.avatarUrl ? `<img src="${pmApi.backendUrl}${f.avatarUrl}" style="width:100%;height:100%;object-fit:cover;"/>` : `<span style="line-height:1;">${initial}</span>`}
                    </div>
                    <span style="font-size:0.875rem;line-height:1.4;">${name}</span>
                </button>
            `;
        }).join('');
    },
    selectAddMember(userId) {
        this._addMemberContext.selected = userId;
        document.getElementById('pm-add-member-btn').disabled = false;
        const rows = document.querySelectorAll('#pm-add-member-list .pm-member-picker-row');
        rows.forEach(r => {
            if (r.getAttribute('onclick').includes(userId)) r.classList.add('selected');
            else r.classList.remove('selected');
        });
    },
    async submitAddMember() {
        if (!this._addMemberContext || !this._addMemberContext.selected) return;
        const btn = document.getElementById('pm-add-member-btn');
        btn.disabled = true;
        try {
            await pmApi.post(`/api/groups/${this._addMemberContext.groupId}/members`, { userId: this._addMemberContext.selected });
            pmToast.success('Đã thêm thành viên');
            this.closeAddMember();
        } catch (e) {
            pmToast.error('Không thể thêm thành viên');
            btn.disabled = false;
        }
    },

    // ==== Edit Group Dialog ====
    _editGroupContext: null,
    openEditGroup(groupId, name, avatarUrl) {
        this._editGroupContext = { groupId, avatarFile: null };
        document.getElementById('pm-edit-group-name').value = name;
        const initial = (name || '?').charAt(0).toUpperCase();
        document.getElementById('pm-edit-group-avatar-initial').textContent = initial;
        
        const preview = document.getElementById('pm-edit-group-avatar-preview');
        if (avatarUrl) {
            preview.src = avatarUrl.startsWith('http') ? avatarUrl : pmApi.backendUrl + avatarUrl;
            preview.style.display = '';
        } else {
            preview.style.display = 'none';
        }
        
        document.getElementById('pm-edit-group-filename').style.display = 'none';
        document.getElementById('pm-edit-group-dialog').style.display = 'flex';
    },
    closeEditGroup() {
        document.getElementById('pm-edit-group-dialog').style.display = 'none';
        this._editGroupContext = null;
    },
    handleEditGroupAvatar(input) {
        if (input.files && input.files[0]) {
            const file = input.files[0];
            if (file.size > 5 * 1024 * 1024) {
                pmToast.error('Ảnh tối đa 5MB');
                return;
            }
            this._editGroupContext.avatarFile = file;
            document.getElementById('pm-edit-group-filename').textContent = file.name;
            document.getElementById('pm-edit-group-filename').style.display = '';
            
            const reader = new FileReader();
            reader.onload = e => {
                const preview = document.getElementById('pm-edit-group-avatar-preview');
                preview.src = e.target.result;
                preview.style.display = '';
            };
            reader.readAsDataURL(file);
        }
    },
    async submitEditGroup() {
        const name = document.getElementById('pm-edit-group-name').value.trim();
        if (!name) return;
        
        const btn = document.getElementById('pm-edit-group-btn');
        const lbl = document.getElementById('pm-edit-group-btn-label');
        const ldg = document.getElementById('pm-edit-group-btn-loading');
        
        btn.disabled = true;
        lbl.style.display = 'none';
        ldg.style.display = 'flex';
        
        try {
            await pmApi.put(`/api/groups/${this._editGroupContext.groupId}`, { name });
            if (this._editGroupContext.avatarFile) {
                const fd = new FormData();
                fd.append('file', this._editGroupContext.avatarFile);
                await pmApi.postForm(`/api/groups/${this._editGroupContext.groupId}/avatar`, fd);
            }
            pmToast.success('Đã cập nhật nhóm');
            this.closeEditGroup();
            // Refresh conversation list (sidebar) and update active chat header
            if (window.pmChat) {
                pmChat.loadConversations();
                const titleEl = document.getElementById('pm-chat-title');
                if (titleEl) titleEl.textContent = name;
            }
        } catch (e) {
            pmToast.error('Không thể cập nhật nhóm');
        } finally {
            btn.disabled = false;
            lbl.style.display = '';
            ldg.style.display = 'none';
        }
    },

    // ==== Edit History Dialog ====
    openEditHistory(historyData) {
        const list = document.getElementById('pm-edit-history-list');
        const empty = document.getElementById('pm-edit-history-empty');
        
        if (!historyData || historyData.length === 0) {
            list.style.display = 'none';
            empty.style.display = '';
        } else {
            empty.style.display = 'none';
            list.style.display = 'flex';
            list.innerHTML = historyData.map(h => {
                const dt = new Date(h.editedAt).toLocaleString('vi-VN');
                return `
                    <div style="padding:12px;border-left:3px solid #3B82F6;background:var(--mud-palette-surface);">
                        <div style="font-size:0.75rem;color:var(--mud-palette-text-secondary);margin-bottom:4px;">${dt}</div>
                        <div style="font-size:0.875rem;margin-bottom:4px;"><strong>Cũ:</strong> ${pmEscape(h.oldContent)}</div>
                        <div style="font-size:0.875rem;"><strong>Mới:</strong> ${pmEscape(h.newContent)}</div>
                    </div>
                `;
            }).join('');
        }
        document.getElementById('pm-edit-history-dialog').style.display = 'flex';
    },
    closeEditHistory() {
        document.getElementById('pm-edit-history-dialog').style.display = 'none';
    },

    // ==== Group Members Dialog ====
    _groupMembersContext: null,
    async openGroupMembers(groupId, groupName) {
        this._groupMembersContext = { groupId };
        document.getElementById('pm-gm-group-name').textContent = groupName;
        document.getElementById('pm-gm-group-name').style.display = groupName ? '' : 'none';
        document.getElementById('pm-group-members-dialog').style.display = 'flex';
        
        document.getElementById('pm-gm-loading').style.display = 'flex';
        document.getElementById('pm-gm-empty').style.display = 'none';
        document.getElementById('pm-gm-list').style.display = 'none';
        
        this.loadGroupMembers();
    },
    async loadGroupMembers() {
        try {
            const group = await pmApi.get(`/api/groups/${this._groupMembersContext.groupId}`);
            const members = group?.members || [];
            document.getElementById('pm-gm-loading').style.display = 'none';
            document.getElementById('pm-gm-count').textContent = members.length + ' thành viên';
            
            if (members.length === 0) {
                document.getElementById('pm-gm-empty').style.display = 'flex';
            } else {
                const list = document.getElementById('pm-gm-list');
                list.style.display = 'flex';
                
                // Get current user role
                const myId = pmChat ? pmChat.myId : 0; // fallback if needed
                const me = members.find(m => m.userId === myId);
                const isCurrentAdmin = me && me.role === 'Admin';
                const isCurrentCoAdmin = me && me.role === 'CoAdmin';
                
                // sort
                members.sort((a, b) => {
                    const rs = { 'Admin': 0, 'CoAdmin': 1, 'Member': 2 };
                    const r1 = rs[a.role] ?? 2, r2 = rs[b.role] ?? 2;
                    if (r1 !== r2) return r1 - r2;
                    return (a.displayName || '').localeCompare(b.displayName || '');
                });

                list.innerHTML = members.map(m => {
                    const isMe = m.userId === myId;
                    const initial = (m.displayName || '?').charAt(0).toUpperCase();
                    const avatar = m.avatarUrl ? `<img src="${pmApi.backendUrl}${m.avatarUrl}" style="width:100%;height:100%;border-radius:50%;object-fit:cover;"/>` : `<span>${initial}</span>`;
                    const date = new Date(m.joinedAt).toLocaleDateString('vi-VN');
                    
                    const canPromote = isCurrentAdmin && !isMe && m.role === 'Member';
                    const canDemote = isCurrentAdmin && !isMe && m.role === 'CoAdmin';
                    const canKick = (isCurrentAdmin || isCurrentCoAdmin) && !isMe && m.role !== 'Admin' && (isCurrentAdmin || m.role === 'Member');
                    
                    let actions = '';
                    if (canPromote) actions += `<button type="button" class="mud-button-root mud-button mud-button-outlined mud-button-outlined-primary" style="height:30px;padding:0 8px;font-size:0.75rem;" onclick="pmDialogs.changeRole(${m.userId}, 'CoAdmin')"><span class="mud-button-label">Lên CoAdmin</span></button>`;
                    if (canDemote) actions += `<button type="button" class="mud-button-root mud-button mud-button-outlined mud-button-outlined-warning" style="height:30px;padding:0 8px;font-size:0.75rem;" onclick="pmDialogs.changeRole(${m.userId}, 'Member')"><span class="mud-button-label">Xuống Member</span></button>`;
                    if (canKick) actions += `<button type="button" class="mud-button-root mud-button mud-button-text mud-button-text-error" style="height:30px;padding:0 8px;font-size:0.75rem;" onclick="pmDialogs.kickMember(${m.userId})"><span class="mud-button-label">Kick</span></button>`;
                    
                    const roleClass = m.role === 'Admin' ? 'admin' : (m.role === 'CoAdmin' ? 'coadmin' : 'member');
                    
                    return `
                        <div class="pm-gm-row">
                            <div class="mud-avatar mud-avatar-medium mud-avatar-filled mud-avatar-filled-primary pm-gm-avatar" style="width:40px;height:40px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:var(--mud-palette-primary);color:white;">
                                ${avatar}
                            </div>
                            <div class="pm-gm-info">
                                <div class="pm-gm-name-line">
                                    <span class="pm-gm-name">${pmEscape(m.displayName)}</span>
                                    ${isMe ? '<span class="pm-gm-me">Bạn</span>' : ''}
                                </div>
                                <div class="pm-gm-meta">Tham gia ${date}</div>
                            </div>
                            <div class="pm-gm-role-cell">
                                <span class="pm-gm-role ${roleClass}">${m.role}</span>
                            </div>
                            <div class="pm-gm-actions">${actions}</div>
                        </div>
                    `;
                }).join('');
            }
        } catch (e) {
            pmToast.error('Không tải được danh sách thành viên');
        }
    },
    async changeRole(userId, role) {
        try {
            await pmApi.patch(`/api/groups/${this._groupMembersContext.groupId}/members/${userId}/role`, { role });
            pmToast.success('Đã cập nhật quyền');
            this.loadGroupMembers();
        } catch(e) {
            pmToast.error('Không cập nhật được quyền');
        }
    },
    async kickMember(userId) {
        try {
            await pmApi.delete(`/api/groups/${this._groupMembersContext.groupId}/members/${userId}`);
            pmToast.success('Đã xóa thành viên khỏi nhóm');
            this.loadGroupMembers();
        } catch(e) {
            pmToast.error('Lỗi xóa thành viên');
        }
    },
    closeGroupMembers() {
        document.getElementById('pm-group-members-dialog').style.display = 'none';
        this._groupMembersContext = null;
    },

    // ==== Nickname Dialog ====
    _nicknameContext: null,
    openNickname(peerId, initialName) {
        this._nicknameContext = { peerId };
        document.getElementById('pm-nickname-input').value = initialName || '';
        document.getElementById('pm-nickname-dialog').style.display = 'flex';
    },
    closeNickname() {
        document.getElementById('pm-nickname-dialog').style.display = 'none';
        this._nicknameContext = null;
    },
    async submitNickname() {
        const nn = document.getElementById('pm-nickname-input').value.trim();
        try {
            await pmApi.put('/api/conversations/nickname', { targetUserId: this._nicknameContext.peerId, nickname: nn });
            pmToast.success('Đã cập nhật biệt danh');
            this.closeNickname();
            // Need to update the UI where the name is shown. If pmChat is globally available:
            if (window.pmChat && typeof window.pmChat.loadConversations === 'function') {
                window.pmChat.loadConversations();
            }
        } catch(e) {
            pmToast.error('Lỗi đổi biệt danh');
        }
    },

    // ==== Shared Files Dialog ====
    openSharedFiles(groupId, peerId, title) {
        document.getElementById('pm-shared-files-title').textContent = title || '';
        document.getElementById('pm-shared-files-title').style.display = title ? '' : 'none';
        document.getElementById('pm-shared-files-dialog').style.display = 'flex';
        
        document.getElementById('pm-shared-files-loading').style.display = 'flex';
        document.getElementById('pm-shared-files-empty').style.display = 'none';
        document.getElementById('pm-shared-files-list').style.display = 'none';
        
        let url = `/api/messages/attachments?type=all&limit=100`;
        if (groupId) url += `&groupId=${groupId}`;
        else if (peerId) url += `&peerId=${peerId}`;
        
        pmApi.get(url).then(files => {
            document.getElementById('pm-shared-files-loading').style.display = 'none';
            if (!files || files.length === 0) {
                document.getElementById('pm-shared-files-empty').style.display = 'flex';
            } else {
                const list = document.getElementById('pm-shared-files-list');
                list.style.display = 'flex';
                list.innerHTML = files.map(f => {
                    const dt = new Date(f.createdAt).toLocaleString('vi-VN');
                    const icon = f.isImage 
                        ? `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="24" height="24"><path d="M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z"/></svg>`
                        : `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="24" height="24"><path d="M6 2c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6H6zm7 7V3.5L18.5 9H13z"/></svg>`;
                    
                    let fileUrl = f.fileUrl;
                    if (fileUrl && !fileUrl.startsWith('http')) {
                        fileUrl = pmApi.backendUrl + (fileUrl.startsWith('/') ? '' : '/') + fileUrl;
                    }
                    
                    return `
                        <div class="pm-shared-row">
                            <div class="pm-shared-icon ${f.isImage ? 'image' : 'file'}">${icon}</div>
                            <div class="pm-shared-info">
                                <div class="pm-shared-name" title="${pmEscape(f.fileName)}">${pmEscape(f.fileName)}</div>
                                <div class="pm-shared-meta">${pmEscape(f.senderDisplayName)} · ${dt} · ${f.displaySize}</div>
                                <div style="font-size:0.7rem;color:var(--mud-palette-text-secondary);">${f.mimeType}</div>
                            </div>
                            <a href="${fileUrl}" target="_blank" rel="noopener noreferrer" class="pm-shared-open">Mở</a>
                        </div>
                    `;
                }).join('');
            }
        }).catch(e => {
            pmToast.error('Không tải được danh sách tệp');
            this.closeSharedFiles();
        });
    },
    closeSharedFiles() {
        document.getElementById('pm-shared-files-dialog').style.display = 'none';
    }
};
