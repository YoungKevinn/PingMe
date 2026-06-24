// PingMe Chat — SignalR + full chat UI
// Expects: pmApi, pmEscape, pmFmt, pmToast from api.js
// SignalR: @microsoft/signalr loaded before this file

const pmChat = (() => {
    // ── State ────────────────────────────────────────────────────
    let _conn      = null;      // SignalR connection
    let _myId      = 0;
    let _myName    = '';
    let _convs     = [];        // cached conversation list
    let _active    = null;      // { peerId?, groupId?, name, avatar }
    let _messages  = [];        // messages in current conversation
    let _replyId   = null;      // message id being replied to
    let _typing    = false;
    let _typingTimer = null;
    let _pinPanelOpen = false;
    let _moreMenuOpen = false;
    let _fileAttach = null;     // File object pending upload
    let _msgSearchOpen = false;
    let _searchTimer = null;
    let _loadingMore = false;
    let _hasMore = true;
    let _oldestMsgId = null;
    let _activeReactionMsgId = null;
    let _confirmCb = null;
    let _ttl = null;
    let _forwardMsgId = null;

    // Mentions state
    let _activeGroupMembers = [];
    let _mentionQuery = '';
    let _mentionStartIndex = -1;
    let _selectedMentionIndex = 0;
    let _mentionFilteredList = [];

    // ── Helpers ──────────────────────────────────────────────────
    const $  = id => document.getElementById(id);
    const qs = sel => document.querySelector(sel);

    function avatarHtml(name, url, size = 36) {
        const initials = (name || '?').split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
        const colors = ['#3B82F6','#8B5CF6','#EC4899','#EF4444','#F59E0B','#10B981','#06B6D4'];
        const color  = colors[(name || '').charCodeAt(0) % colors.length];
        if (url) {
            return `<div class="mud-avatar mud-avatar-filled mud-avatar-filled-primary" style="width:${size}px;height:${size}px;font-size:${Math.floor(size*0.4)}px;flex-shrink:0;background:${color};">
                        <img src="${pmEscape(url)}" style="width:100%;height:100%;border-radius:50%;object-fit:cover;" loading="lazy" onerror="this.style.display='none';this.nextElementSibling.style.display='inline';">
                        <span style="display:none;color:#fff;font-weight:700;">${initials}</span>
                    </div>`;
        }
        return `<div class="mud-avatar mud-avatar-filled mud-avatar-filled-primary" style="width:${size}px;height:${size}px;background:${color};color:#fff;font-size:${Math.floor(size*0.4)}px;font-weight:700;flex-shrink:0;">${initials}</div>`;
    }

    // ── SignalR connection ────────────────────────────────────────
    async function initSignalR() {
        const token = pmGetToken();
        if (!token) return;

        _conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat', { accessTokenFactory: () => token })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // ── Hub event handlers ──────────────────────────────────
        _conn.on('ReceiveMessage', msg => _onReceiveMessage(msg));
        _conn.on('UserTyping', data => _onUserTyping(data));
        _conn.on('UserStatusChanged', data => _onUserStatusChanged(data));
        _conn.on('MessageRead', data => _onMessageRead(data));
        _conn.on('MessageReactionUpdated', data => _onReactionUpdated(data));
        _conn.on('PollVoteUpdated', data => _onPollVoteUpdated(data));
        _conn.on('GroupDeleted', data => _onGroupDeleted(data));
        _conn.on('GroupMemberKicked', data => _onGroupMemberKicked(data));
        _conn.on('GroupMemberAdded', data => _onGroupMemberAdded(data));
        _conn.on('IncomingCall', data => _onIncomingCall(data));
        _conn.on('CallAnswered', data => _onCallAnswered(data));
        _conn.on('CallEnded', data => _onCallEnded(data));
        _conn.on('ReceiveWebRTCSignal', data => _onReceiveWebRTCSignal(data));
        _conn.on('UserMentioned', data => { pmToast.info('@' + (_myName || 'you') + ' được nhắc đến'); });

        _conn.onreconnecting(() => pmToast.warn('Đang kết nối lại...'));
        _conn.onreconnected(async () => {
            pmToast.success('Đã kết nối lại');
            if (_active) {
                const room = _active.groupId ? `group_${_active.groupId}` : `user_${_myId}`;
                await _conn.invoke('JoinRoom', room).catch(() => {});
            }
            await loadConversations();
        });
        _conn.onclose(() => pmToast.error('Mất kết nối. Tải lại trang để thử lại.'));

        try {
            await _conn.start();
            if (window.pmWebRTC) window.pmWebRTC.init(_conn);
            const initialRoom = `user_${_myId}`;
            await _conn.invoke('JoinRoom', initialRoom).catch(() => {});
        } catch (err) {
            console.error('SignalR error', err);
        }
    }

    // ── SignalR events ────────────────────────────────────────────
    function _onReceiveMessage(msg) {
        const isForActive = _active && (
            (msg.groupId && msg.groupId === _active.groupId) ||
            (!msg.groupId && (msg.senderId === _active.peerId || msg.receiverId === _active.peerId))
        );

        // Update conversation list preview
        const idx = _convs.findIndex(c =>
            msg.groupId ? c.groupId === msg.groupId
                        : (c.peerId === msg.senderId || c.peerId === msg.receiverId)
        );
        if (idx > -1) {
            _convs[idx].lastMessageContent = msg.content || (msg.attachments?.length ? '📎 File' : '');
            _convs[idx].lastMessageAt = msg.createdAt;
            if (!isForActive && msg.senderId !== _myId) _convs[idx].unreadCount = (_convs[idx].unreadCount || 0) + 1;
            // move to top
            const [c] = _convs.splice(idx, 1);
            _convs.unshift(c);
            renderConversations(_convs);
        } else {
            loadConversations();
        }

        if (!isForActive) return;

        _messages.push(msg);
        _appendMessage(msg, true);
        // auto-read
        if (_conn && msg.id) _conn.invoke('MarkAsRead', msg.id).catch(() => {});
    }

    function _onUserTyping(data) {
        if (!_active) return;
        const forUs = _active.groupId
            ? data.groupId === _active.groupId
            : data.senderId === _active.peerId;
        if (!forUs) return;

        const el = $('pm-typing-area');
        if (!el) return;
        if (data.isTyping) {
            el.innerHTML = `<span style="font-size:.78rem;color:var(--mud-palette-text-secondary);font-style:italic;">${pmEscape(data.senderDisplayName||'')} đang nhập...</span>`;
            el.style.display = 'block';
        } else {
            el.innerHTML = '';
            el.style.display = 'none';
        }
    }

    function _onUserStatusChanged(data) {
        // Update status dot in conversation list
        document.querySelectorAll(`[data-peer-id="${data.userId}"] .pm-status-dot`).forEach(dot => {
            dot.style.background = data.isOnline ? '#22C55E' : '#94A3B8';
        });
        // Update chat header
        if (_active?.peerId === data.userId) {
            const sub = $('pm-chat-subtitle');
            if (sub) sub.textContent = data.isOnline ? 'Đang hoạt động' : ('Hoạt động ' + pmFmt.time(data.lastSeen));
        }
    }

    function _onMessageRead(data) {
        const el = document.querySelector(`[data-msg-id="${data.messageId}"] .pm-read-receipt`);
        if (el) el.style.color = '#3B82F6';
    }

    function _onReactionUpdated(data) {
        const msgEl = document.querySelector(`[data-msg-id="${data.messageId}"]`);
        if (!msgEl) return;
        
        const reactions = data.reactions || [];
        
        // Update cached messages if exists
        const msgData = _messages.find(m => m.id === data.messageId);
        if (msgData) {
            msgData.reactions = reactions;
        }

        let bar = msgEl.querySelector('.pm-reactions-bar');
        if (reactions.length === 0) {
            if (bar) bar.remove();
            return;
        }

        if (!bar) { 
            bar = document.createElement('div'); 
            bar.className = 'pm-reactions-bar'; 
            bar.style.display = 'flex';
            bar.style.flexWrap = 'wrap';
            bar.style.gap = '4px';
            bar.style.marginTop = '4px';
            const body = msgEl.querySelector('.pm-msg-bubble');
            if (body) body.parentElement.insertBefore(bar, body.nextSibling);
        }
        bar.innerHTML = reactions.map(r =>
            `<button class="mud-chip mud-chip-filled mud-chip-size-small mud-ripple ${r.userIds.includes(_myId) ? 'mud-chip-color-primary' : ''}" style="padding:0 8px;"
                     onclick="pmChat.addReaction(${data.messageId}, '${pmEscape(r.emoji)}')"
                     title="${r.count} người">
                <span class="mud-chip-content">${pmEscape(r.emoji)} <span style="margin-left:4px;font-size:0.75rem;">${r.count}</span></span>
             </button>`
        ).join('');
    }

    function _onPollVoteUpdated(data) {
        const msgEl = document.querySelector(`[data-msg-id="${data.messageId}"]`);
        if (!msgEl) return;
        
        // Re-build entire poll HTML to keep it simple and consistent with new MudBlazor layout
        const msgData = _messages.find(m => m.id === data.messageId);
        if (msgData && data.poll) {
            msgData.poll = data.poll;
            // Need to re-render just the poll part
            const card = msgEl.querySelector('.pm-poll-card');
            if (card) {
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = _buildPollHtml(msgData);
                card.outerHTML = tempDiv.innerHTML;
            }
        }
    }

    function _onGroupDeleted(data) {
        if (_active?.groupId === data.groupId) {
            $('pm-chat-area').querySelector('.pm-chat-wrapper')?.remove();
            $('pm-empty-state')?.style?.setProperty('display', 'flex');
            pmToast.info('Nhóm đã bị xóa');
            _active = null;
        }
        loadConversations();
    }

    function _onGroupMemberKicked(data) {
        if (data.kickedUserId === _myId && _active?.groupId === data.groupId) {
            const banner = $('pm-kicked-banner');
            if (banner) { banner.style.display = 'flex'; }
        }
        if (_active?.groupId === data.groupId) loadMessages();
    }

    function _onGroupMemberAdded(data) {
        if (data.userId === _myId) loadConversations();
    }

    function _onIncomingCall(data) {
        pmToast.info((data.callerName || data.callerDisplayName || '?') + ' đang gọi cho bạn...');
        if (window.pmWebRTC?.handleIncoming) window.pmWebRTC.handleIncoming(data);
    }

    function _onCallAnswered(data) {
        if (window.pmWebRTC?.handleCallAnswered) window.pmWebRTC.handleCallAnswered(data);
    }

    function _onCallEnded(data) {
        if (window.pmWebRTC?.handleCallEnded) window.pmWebRTC.handleCallEnded(data);
    }

    function _onReceiveWebRTCSignal(data) {
        if (window.pmWebRTC?.handleWebRTCSignal) window.pmWebRTC.handleWebRTCSignal(data);
    }

    // ── Load conversations ────────────────────────────────────────
    async function loadConversations() {
        const data = await pmApi.get('/api/conversations');
        if (!data) return;
        _convs = data;
        renderConversations(data);
    }

    function renderConversations(convs) {
        const list = $('pm-conv-list');
        if (!list) return;

        const q = ($('pm-conv-search')?.value || '').toLowerCase();
        const filtered = q ? convs.filter(c =>
            (c.peerDisplayName || c.groupName || '').toLowerCase().includes(q) ||
            (c.lastMessageContent || '').toLowerCase().includes(q)
        ) : convs;

        if (!filtered.length) {
            list.innerHTML = `<div id="pm-conv-empty" style="padding:32px 16px;text-align:center;"><svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24" style="font-size:48px;opacity:.2;color:var(--mud-palette-primary);margin:0 auto;display:block;"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z"/></svg><p class="mud-typography mud-typography-subtitle1" style="margin-top:16px;">Chưa có hội thoại nào</p><p class="mud-typography mud-typography-body2" style="color:var(--mud-palette-text-secondary);">Bắt đầu cuộc trò chuyện mới</p></div>`;
            return;
        }

        list.innerHTML = `<div class="mud-list mud-list-padding">` + filtered.map(c => {
            const name    = pmEscape(c.peerDisplayName || c.groupName || '?');
            const preview = pmEscape(c.lastMessageContent || '');
            const time    = pmFmt.shortTime(c.lastMessageAt);
            const unread  = c.unreadCount > 0;
            const isActive = _active && (
                (c.groupId && c.groupId === _active.groupId) ||
                (!c.groupId && c.peerId === _active.peerId)
            );
            return `
            <div class="mud-list-item mud-list-item-gutters mud-list-item-clickable mud-ripple ${isActive ? 'mud-selected-item' : ''}"
                 data-peer-id="${c.peerId || ''}" data-group-id="${c.groupId || ''}"
                 onclick="pmChat.selectConversation(${c.peerId || 'null'}, ${c.groupId || 'null'}, '${name.replace(/'/g, "\\'")}')"
                 style="border-radius:8px; margin:2px 8px; ${isActive ? 'background:rgba(var(--mud-palette-primary-rgb), 0.1);' : ''}">
                <div class="mud-list-item-icon">
                    <div style="position:relative;flex-shrink:0;">
                        ${avatarHtml(c.peerDisplayName || c.groupName, c.peerAvatarUrl || c.groupAvatarUrl, 44)}
                        ${!c.groupId ? `<div class="pm-status-dot" style="position:absolute;bottom:0;right:0;width:12px;height:12px;border-radius:50%;border:2px solid var(--mud-palette-surface);background:${c.peerIsOnline ? '#22C55E' : '#94A3B8'};"></div>` : ''}
                    </div>
                </div>
                <div class="mud-list-item-text" style="flex:1;min-width:0;margin-left:12px;">
                    <div style="display:flex;justify-content:space-between;align-items:baseline;gap:6px;">
                        <span class="mud-typography mud-typography-body2" style="font-weight:${unread ? 700 : 500};white-space:nowrap;overflow:hidden;text-overflow:ellipsis; ${isActive ? 'color:var(--mud-palette-primary);' : 'color:var(--mud-palette-text-primary);'}">${name}</span>
                        <span class="mud-typography mud-typography-caption" style="color:var(--mud-palette-text-secondary);flex-shrink:0;">${time}</span>
                    </div>
                    <div style="display:flex;align-items:center;gap:4px;">
                        <p class="mud-typography mud-typography-body2" style="margin:0;font-size:.78rem;color:var(--mud-palette-text-secondary);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;flex:1;font-weight:${unread ? 600 : 400};">${preview}</p>
                        ${unread ? `<span class="mud-badge mud-badge-filled mud-badge-filled-error mud-elevation-0" style="position:static;transform:none;padding:0 6px;">${c.unreadCount > 99 ? '99+' : c.unreadCount}</span>` : ''}
                    </div>
                </div>
            </div>`;
        }).join('') + `</div>`;
    }

    function filterConvs() {
        renderConversations(_convs);
    }

    // ── Select conversation ───────────────────────────────────────
    async function selectConversation(peerId, groupId, name) {
        // Leave previous room
        if (_active && _conn) {
            const oldRoom = _active.groupId ? `group_${_active.groupId}` : `user_${_myId}`;
            await _conn.invoke('LeaveRoom', oldRoom).catch(() => {});
        }

        _active = { peerId: peerId || null, groupId: groupId || null, name };
        _messages = [];
        _replyId  = null;
        _hasMore  = true;
        _activeGroupMembers = [];
        _hideMentionDropdown();
        _oldestMsgId = null;
        _pinPanelOpen = false;

        // Show chat area, hide empty state
        const wrapper = $('pm-chat-wrapper');
        const empty   = $('pm-empty-state');
        if (wrapper) wrapper.style.display = 'flex';
        if (empty)   empty.style.display   = 'none';

        // Show voice/video call buttons only for direct (DM) conversations
        const callBtns = $('pm-call-btns');
        if (callBtns) callBtns.style.display = (peerId && !groupId) ? 'flex' : 'none';

        // Update header
        const titleEl = $('pm-chat-title');
        if (titleEl) titleEl.textContent = name;

        // Join new SignalR room
        if (_conn) {
            const room = groupId ? `group_${groupId}` : `user_${_myId}`;
            await _conn.invoke('JoinRoom', room).catch(() => {});
        }

        // Load messages
        await loadMessages();

        // Update conversation list highlight
        renderConversations(_convs);

        // Load peer/group subtitle
        await _loadChatHeader();

        // Reset kicked banner
        const banner = $('pm-kicked-banner');
        if (banner) banner.style.display = 'none';

        // Close pin panel, reply preview
        closePinPanel();
        cancelReply();
        document.getElementById('pm-reply-preview')?.style.setProperty('display', 'none');
    }

    async function _loadChatHeader() {
        const sub = $('pm-chat-subtitle');
        const titleEl = $('pm-chat-title');
        if (!sub) return;
        if (_active.groupId) {
            const g = await pmApi.get('/api/groups/' + _active.groupId);
            if (g) {
                _activeGroupMembers = g.members || [];
                if (!_active.name) {
                    _active.name = g.name;
                    if (titleEl) titleEl.textContent = g.name;
                }
                sub.textContent = (g.members?.length || 0) + ' thành viên';
                const avatarEl = $('pm-chat-avatar');
                if (avatarEl) avatarEl.innerHTML = avatarHtml(g.name, g.avatarUrl, 40);
                _active.avatar = g.avatarUrl;
            }
        } else if (_active.peerId) {
            const u = await pmApi.get('/api/users/' + _active.peerId);
            if (u) {
                if (!_active.name) {
                    _active.name = u.displayName || u.username;
                    if (titleEl) titleEl.textContent = _active.name;
                }
                sub.textContent = u.isOnline ? 'Đang hoạt động' : ('Hoạt động ' + pmFmt.time(u.lastSeen));
                const avatarEl = $('pm-chat-avatar');
                if (avatarEl) avatarEl.innerHTML = avatarHtml(u.displayName || u.username, u.avatarUrl, 40);
                _active.avatar = u.avatarUrl;
            }
        }
    }

    // ── Load messages ─────────────────────────────────────────────
    async function loadMessages() {
        if (!_active) return;
        const scroll = $('msgScroll');
        if (!scroll) return;

        scroll.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100%;"><div class="mud-progress-circular mud-progress-circular-indeterminate" style="width:36px;height:36px;"><svg viewBox="22 22 44 44"><circle class="mud-progress-circular-circle" cx="44" cy="44" r="20.2" fill="none" stroke-width="3.6"></circle></svg></div></div>`;

        // Read target message ID from URL parameters on first load
        const urlParams = new URLSearchParams(window.location.search);
        const targetMsgId = urlParams.get('messageId') ? parseInt(urlParams.get('messageId')) : null;

        let url = '';
        let isContextLoad = false;
        if (targetMsgId) {
            url = `/api/messages/${targetMsgId}/context?takeBefore=30&takeAfter=30`;
            isContextLoad = true;
        } else {
            if (_active.groupId) url = `/api/messages/group/${_active.groupId}?limit=30`;
            else url = `/api/messages/dm/${_active.peerId}?limit=30`;
        }

        let data = await pmApi.get(url);
        
        // Fallback if context load fails
        if (isContextLoad && (!data || !data.length)) {
            isContextLoad = false;
            if (_active.groupId) url = `/api/messages/group/${_active.groupId}?limit=30`;
            else url = `/api/messages/dm/${_active.peerId}?limit=30`;
            data = await pmApi.get(url);
        }

        if (!data) { scroll.innerHTML = ''; return; }

        _messages = Array.isArray(data) ? data : (data.messages || []);
        
        if (isContextLoad) {
            _hasMore = true; // Allow loading older messages
            _oldestMsgId = _messages[0]?.id || null;
        } else {
            _hasMore  = _messages.length >= 30;
            _oldestMsgId = _messages[0]?.id || null;
        }

        scroll.innerHTML = '';
        _messages.forEach(m => _appendMessage(m, false));

        if (isContextLoad && targetMsgId) {
            setTimeout(() => {
                const el = document.querySelector(`[data-msg-id="${targetMsgId}"]`);
                if (el) {
                    el.scrollIntoView({ behavior: 'auto', block: 'center' });
                    el.classList.add('pm-msg-highlight');
                    el.style.outline = '2px solid var(--mud-palette-primary)';
                    setTimeout(() => {
                        el.style.outline = '';
                        el.classList.remove('pm-msg-highlight');
                    }, 3000);
                }
            }, 200);

            // Clean URL query parameters without page reload
            const cleanUrl = window.location.pathname + window.location.search.replace(/[?&]messageId=\d+/, '').replace(/^&/, '?');
            window.history.replaceState({}, document.title, cleanUrl);
        } else {
            scroll.scrollTop = scroll.scrollHeight;
        }

        // Mark as read
        if (_messages.length && _conn) {
            const last = _messages[_messages.length - 1];
            _conn.invoke('MarkAsRead', last.id).catch(() => {});
        }
    }

    async function loadMoreMessages() {
        if (!_active || _loadingMore || !_hasMore || !_oldestMsgId) return;
        _loadingMore = true;
        const scroll = $('msgScroll');
        const prevH = scroll?.scrollHeight || 0;
        const prevTop = scroll?.scrollTop || 0;

        let url = '';
        if (_active.groupId) url = `/api/messages/group/${_active.groupId}?limit=30&before=${_oldestMsgId}`;
        else url = `/api/messages/dm/${_active.peerId}?limit=30&before=${_oldestMsgId}`;

        const data = await pmApi.get(url);
        _loadingMore = false;
        if (!data) return;

        const msgs = Array.isArray(data) ? data : (data.messages || []);
        if (!msgs.length) { _hasMore = false; return; }
        _hasMore = msgs.length >= 30;
        _oldestMsgId = msgs[0]?.id;

        msgs.reverse().forEach(m => {
            _messages.unshift(m);
            _prependMessage(m);
        });

        // Restore scroll position
        if (scroll) scroll.scrollTop = scroll.scrollHeight - prevH + prevTop;
    }

    // ── Render a single message ───────────────────────────────────
    function _appendMessage(msg, animate) {
        const scroll = $('msgScroll');
        if (!scroll) return;
        const el = _buildMessageEl(msg, animate);
        scroll.appendChild(el);
        if (animate) scroll.scrollTop = scroll.scrollHeight;
    }

    function _prependMessage(msg) {
        const scroll = $('msgScroll');
        if (!scroll) return;
        const el = _buildMessageEl(msg, false);
        scroll.insertBefore(el, scroll.firstChild);
    }

    function _buildMessageEl(msg, animate) {
        const isMine = msg.senderId === _myId;
        const wrap   = document.createElement('div');
        wrap.className  = 'pm-msg-wrap' + (isMine ? ' pm-msg-mine' : '');
        wrap.dataset.msgId = msg.id;
        if (animate) wrap.style.animation = 'pm-msg-in .2s ease';

        if (msg.isDeleted) {
            wrap.innerHTML = `<div class="mud-paper mud-elevation-0" style="opacity:.5;font-style:italic;font-size:.82rem;padding:6px 12px;background:rgba(0,0,0,0.05);border-radius:12px;width:max-content;margin:0 auto;">Tin nhắn đã bị thu hồi</div>`;
            return wrap;
        }

        const name   = isMine ? '' : `<span class="mud-typography mud-typography-caption" style="display:block;margin-bottom:2px;font-weight:600;color:var(--mud-palette-text-secondary);">${pmEscape(msg.senderDisplayName)}</span>`;
        const time   = `<span style="font-size:.72rem;opacity:.65;white-space:nowrap;word-break:normal;">${pmFmt.shortTime(msg.createdAt)}</span>`;
        const edited = msg.isEdited ? `<span style="font-size:.72rem;opacity:.6;margin-left:4px;cursor:pointer;text-decoration:underline dotted;white-space:nowrap;" onclick="pmChat.loadEditHistory(${msg.id})" title="Xem lịch sử chỉnh sửa">(đã sửa)</span>` : '';
        const read   = isMine ? `<span style="font-size:.7rem;margin-left:4px;color:${msg.readByUserIds?.length > 1 ? 'var(--mud-palette-primary)' : 'var(--mud-palette-text-secondary)'};">✓✓</span>` : '';

        // Reply preview
        let replyHtml = '';
        if (msg.replyToMessage) {
            const r = msg.replyToMessage;
            replyHtml = `<div class="mud-paper mud-elevation-0" onclick="pmChat.scrollToMessage(${r.id})" style="cursor:pointer;border-left:3px solid var(--mud-palette-primary);padding:4px 8px;margin-bottom:6px;border-radius:0 6px 6px 0;background:rgba(0,0,0,.06);">
                <span class="mud-typography mud-typography-caption" style="font-weight:700;color:var(--mud-palette-primary);">${pmEscape(r.senderDisplayName)}</span>
                <p class="mud-typography mud-typography-caption" style="margin:0;color:var(--mud-palette-text-secondary);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${pmEscape(r.content || (r.attachments?.length ? '📎 File' : ''))}</p>
            </div>`;
        }

        // Content
        let contentHtml = '';
        if (msg.messageType === 'System') {
            wrap.className = 'pm-msg-wrap pm-msg-system-wrap';
            wrap.innerHTML = `<div style="text-align:center;margin:12px 0;">
                <span class="mud-chip mud-chip-filled mud-chip-size-small" style="background:rgba(0,0,0,.04);color:var(--mud-palette-text-secondary);"><span class="mud-chip-content">${pmEscape(msg.content)}</span></span>
            </div>`;
            return wrap;
        } else if (msg.messageType === 'Call') {
            const isMissed = msg.content?.toLowerCase().includes('missed') || msg.content?.toLowerCase().includes('declined');
            const isVideo = msg.content?.toLowerCase().includes('video');
            const callColor = isMissed ? 'var(--mud-palette-error)' : 'var(--mud-palette-text-secondary)';
            const callIcon = isVideo ? (isMissed ? '📹' : '📹') : (isMissed ? '📞' : '📞'); // Using emojis for simplicity without SVG injection
            wrap.className = 'pm-msg-wrap pm-msg-system-wrap';
            wrap.innerHTML = `<div style="text-align:center;margin:12px 0;">
                <div class="mud-chip mud-chip-outlined mud-chip-size-medium" style="display:inline-flex;align-items:center;gap:8px;">
                    <span style="color:${callColor};font-size:1.2rem;">${callIcon}</span>
                    <span class="mud-chip-content">${pmEscape(msg.content)}</span>
                </div>
            </div>`;
            return wrap;
        } else if (msg.messageType === 'Vulnerability') {
            let payload = {};
            try { payload = JSON.parse(msg.content); } catch (e) {}
            const severity = payload.Severity || 'Medium';
            const endpoint = payload.AffectedEndpoint ? (payload.HttpMethod ? payload.HttpMethod.toUpperCase() + ' ' : '') + payload.AffectedEndpoint : '';
            contentHtml = `<div class="mud-paper mud-elevation-1 mud-card" style="border-left:4px solid ${severity === 'Critical' ? '#ef4444' : severity === 'High' ? '#f97316' : severity === 'Medium' ? '#eab308' : '#3b82f6'}; margin-top:8px;">
                <div class="mud-card-content" style="padding:12px;">
                    <div style="display:flex;gap:8px;align-items:center;margin-bottom:8px;">
                        <span class="mud-typography mud-typography-subtitle2">🎯 Pentest Finding</span>
                        ${payload.FindingId ? `<div class="mud-chip mud-chip-filled mud-chip-size-small"><span class="mud-chip-content">#${payload.FindingId}</span></div>` : ''}
                        <div class="mud-chip mud-chip-filled mud-chip-color-error mud-chip-size-small"><span class="mud-chip-content">${severity}</span></div>
                        <div class="mud-chip mud-chip-filled mud-chip-size-small"><span class="mud-chip-content">${payload.Status || 'Open'}</span></div>
                    </div>
                    <h6 class="mud-typography mud-typography-h6" style="font-size:1rem; margin-bottom:8px;">${pmEscape(payload.Title || 'Vulnerability finding')}</h6>
                    ${endpoint ? `<p class="mud-typography mud-typography-body2" style="font-family:monospace; background:rgba(0,0,0,.08); padding:4px 8px; border-radius:4px; margin:8px 0;">Endpoint: ${pmEscape(endpoint)}</p>` : ''}
                    ${payload.Payload ? `<pre style="background:#1e1e1e; color:#d4d4d4; padding:8px; border-radius:4px; font-size:.75rem; overflow-x:auto;">${pmEscape(payload.Payload)}</pre>` : ''}
                </div>
                <div class="mud-card-actions">
                    <button type="button" class="mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-size-small mud-ripple" onclick="window.location.href='/pentest?findingId=${payload.FindingId || ''}'"><span class="mud-button-label">Open in Finding Tracker</span></button>
                </div>
            </div>`;
        } else if (msg.messageType === 'Task') {
            let payload = {};
            try { payload = JSON.parse(msg.content); } catch (e) {}
            const priority = payload.Priority || 'Medium';
            const status = payload.Status || 'Open';
            contentHtml = `<div class="mud-paper mud-elevation-1 mud-card" style="margin-top:8px;">
                <div class="mud-card-content" style="padding:12px;">
                    <div style="display:flex;gap:8px;align-items:center;margin-bottom:8px;">
                        <span class="mud-typography mud-typography-subtitle2">✅ Task</span>
                        <div class="mud-chip mud-chip-filled mud-chip-size-small"><span class="mud-chip-content">${priority}</span></div>
                        <div class="mud-chip mud-chip-filled mud-chip-color-info mud-chip-size-small"><span class="mud-chip-content">${status}</span></div>
                    </div>
                    <h6 class="mud-typography mud-typography-h6" style="font-size:1rem; margin-bottom:8px;">${pmEscape(payload.Title || msg.content || 'Task')}</h6>
                    ${payload.AssignedToDisplayName ? `<p class="mud-typography mud-typography-body2" style="color:var(--mud-palette-text-secondary);">Assignee: ${pmEscape(payload.AssignedToDisplayName)}</p>` : ''}
                </div>
                <div class="mud-card-actions">
                    <button type="button" class="mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-size-small mud-ripple" onclick="window.location.href='/tasks?taskId=${payload.TaskId || ''}'"><span class="mud-button-label">Open Task Center</span></button>
                </div>
            </div>`;
        } else if (msg.messageType === 'Reminder') {
            let payload = {};
            try { payload = JSON.parse(msg.content); } catch (e) {}
            const status = payload.Status || 'Pending';
            contentHtml = `<div class="mud-paper mud-elevation-1 mud-card" style="margin-top:8px; border-left:4px solid var(--mud-palette-info);">
                <div class="mud-card-content" style="padding:12px;">
                    <div style="display:flex;gap:8px;align-items:center;margin-bottom:8px;">
                        <span class="mud-typography mud-typography-subtitle2" style="color:var(--mud-palette-info);">⏰ Lời nhắc</span>
                        <div class="mud-chip mud-chip-filled mud-chip-size-small"><span class="mud-chip-content">${status}</span></div>
                    </div>
                    <p class="mud-typography mud-typography-body1" style="font-weight:500;">${pmEscape(payload.Text || msg.content || '')}</p>
                </div>
            </div>`;
        } else if (msg.messageType === 'Snippet') {
            const snippet = msg.snippet;
            const lang = snippet?.language || 'plaintext';
            const code = snippet?.content || msg.content || '';
            const title = snippet?.title || 'Code snippet';
            const token = snippet?.shareToken || '';
            contentHtml = `<div class="mud-paper mud-elevation-1" style="margin-top:6px;border-radius:10px;overflow:hidden;background:#1e1e1e;">
                <div style="display:flex;align-items:center;justify-content:space-between;padding:8px 12px;background:#252526;border-bottom:1px solid rgba(255,255,255,.08);">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <svg viewBox="0 0 24 24" style="width:16px;height:16px;fill:#6e7681;flex-shrink:0;"><path d="M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z"/></svg>
                        <span style="font-size:.78rem;color:#cdd6f4;font-weight:600;">${pmEscape(title)}</span>
                        <div style="background:rgba(59,130,246,.3);color:#93c5fd;padding:1px 8px;border-radius:4px;font-size:.7rem;font-weight:700;font-family:monospace;">${pmEscape(lang)}</div>
                    </div>
                    ${token ? `<a href="/snippets?token=${pmEscape(token)}" target="_blank" style="color:#93c5fd;text-decoration:none;font-size:.75rem;padding:2px 8px;border:1px solid rgba(147,197,253,.3);border-radius:4px;">Mở →</a>` : ''}
                </div>
                <pre style="margin:0;padding:12px;overflow-x:auto;font-size:.78rem;line-height:1.6;max-height:220px;overflow-y:auto;"><code class="language-${pmEscape(lang)}" style="background:transparent;white-space:pre;">${pmEscape(code.slice(0, 2000))}</code></pre>
            </div>`;
            setTimeout(() => {
                const codeEl = document.querySelector(`[data-msg-id="${msg.id}"] code.language-${lang}`);
                if (codeEl && window.hljs) hljs.highlightElement(codeEl);
            }, 60);
        } else if (msg.messageType === 'Audio') {
            const audioUrl = msg.attachments?.[0]?.fileUrl || msg.content || '';
            contentHtml = `<div style="margin-top:4px;display:flex;align-items:center;gap:8px;">
                <svg viewBox="0 0 24 24" style="width:20px;height:20px;fill:${isMine ? 'rgba(255,255,255,.7)' : 'var(--mud-palette-text-secondary)'};flex-shrink:0;"><path d="M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z"/></svg>
                <audio controls style="max-width:240px;height:36px;outline:none;border-radius:18px;">
                    <source src="${pmEscape(audioUrl)}" type="audio/webm">
                    <source src="${pmEscape(audioUrl)}" type="audio/ogg">
                    <source src="${pmEscape(audioUrl)}" type="audio/mpeg">
                </audio>
            </div>`;
        } else if (msg.content) {
            const iocMatch = msg.content.match(/^\s*\/ioc(?:\s+([a-zA-Z0-9]+))?(?:\s+([\s\S]+))?\s*$/i);
            if (iocMatch) {
                const type = (iocMatch[1] || '').toLowerCase();
                const value = (iocMatch[2] || '').trim();
                let url = '', source = '', icon = '', iocType = type.toUpperCase();
                
                if (type === 'ip' || type === 'ipv4') { url = 'https://www.virustotal.com/gui/ip-address/' + value; source = 'VirusTotal'; icon = '🌐'; iocType = 'IP'; }
                else if (type === 'cve') { url = 'https://nvd.nist.gov/vuln/detail/' + value; source = 'NVD'; icon = '🛡️'; iocType = 'CVE'; }
                else if (type === 'md5' || type === 'sha256' || type === 'hash') { url = 'https://www.virustotal.com/gui/file/' + value; source = 'VirusTotal'; icon = '🧬'; iocType = 'HASH'; }
                else if (type === 'url') { url = value; source = 'Open'; icon = '🔗'; iocType = 'URL'; }
                
                if (url) {
                    contentHtml = `<div class="mud-paper mud-elevation-1 mud-card" style="background:var(--mud-palette-dark); color:var(--mud-palette-dark-text); margin-top:8px;">
                        <div class="mud-card-content" style="padding:12px;">
                            <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px;color:var(--mud-palette-text-secondary);">
                                <span>${icon}</span>
                                <span class="mud-typography mud-typography-subtitle2">${iocType}</span>
                                <div class="mud-chip mud-chip-outlined mud-chip-size-small" style="margin-left:auto;"><span class="mud-chip-content">${source}</span></div>
                            </div>
                            <a href="${pmEscape(url)}" target="_blank" rel="noopener noreferrer" class="mud-typography mud-typography-body1" style="color:var(--mud-palette-info-lighten); font-weight:bold; text-decoration:none; word-break:break-all;">
                                ${pmEscape(value)}
                            </a>
                        </div>
                    </div>`;
                } else {
                    contentHtml = `<div class="mud-alert mud-alert-filled mud-alert-filled-error" style="margin-top:8px;">
                        <div class="mud-alert-message">
                            <strong>⚠️ IOC Error</strong><br/>
                            Cú pháp /ioc không hợp lệ. Hỗ trợ: /ioc ip, /ioc cve, /ioc md5, /ioc sha256, /ioc url
                        </div>
                    </div>`;
                }
            } else {
                const linkified = pmEscape(msg.content).replace(
                    /(https?:\/\/[^\s<>"]+)/g,
                    '<a href="$1" target="_blank" rel="noopener noreferrer" style="color:var(--mud-palette-primary);">$1</a>'
                );
                let textHtml = linkified;
                textHtml = textHtml.replace(
                    /(^|\s)(@all)(\b)/gi,
                    '$1<span style="color:var(--mud-palette-primary);font-weight:700;background:rgba(59,130,246,.15);padding:1px 4px;border-radius:4px;">$2</span>'
                );
                textHtml = textHtml.replace(
                    /(^|\s)(@(?!(?:all\b))\w+)/g,
                    '$1<span style="color:var(--mud-palette-primary);font-weight:700;background:rgba(59,130,246,.15);padding:1px 4px;border-radius:4px;">$2</span>'
                );
                contentHtml = `<p class="pm-msg-text mud-typography mud-typography-body1" style="margin:0;">${textHtml}</p>`;
            }
        }

          // Attachments
        let attachHtml = '';
        if (msg.attachments?.length) {
            attachHtml = msg.attachments.map(a => {
                const isImg = a.mimeType?.startsWith('image/');
                if (isImg) return `<a href="${pmEscape(a.fileUrl)}" target="_blank"><img src="${pmEscape(a.fileUrl)}" style="max-width:260px;max-height:200px;border-radius:10px;display:block;margin-top:6px;" loading="lazy"></a>`;
                const size = a.fileSize > 1048576 ? (a.fileSize / 1048576).toFixed(1) + ' MB' : (a.fileSize > 1024 ? (a.fileSize / 1024).toFixed(0) + ' KB' : a.fileSize + ' B');
                return `<div class="mud-paper mud-elevation-1 mud-card" style="margin-top:6px; background:var(--mud-palette-surface);">
                    <div class="mud-card-content" style="padding:8px 12px; display:flex; align-items:center; gap:8px;">
                        <div class="mud-avatar mud-avatar-small mud-avatar-filled mud-avatar-filled-primary"><svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14,2 14,8 20,8"/></svg></div>
                        <div style="flex:1;">
                           <div style="font-size:.82rem;font-weight:500;">${pmEscape(a.fileName)}</div>
                           <div style="font-size:.7rem;opacity:.7;">${size}</div>
                        </div>
                        <a href="${pmEscape(a.fileUrl)}" download style="color:var(--mud-palette-primary);text-decoration:none;"><svg class="mud-icon-root mud-svg-icon" viewBox="0 0 24 24"><path d="M5 20h14v-2H5v2zM19 9h-4V3H9v6H5l7 7 7-7z"/></svg></a>
                    </div>
                </div>`;
            }).join('');
        }

        const pollHtml = msg.poll ? _buildPollHtml(msg) : '';
        const isPoll = !!msg.poll;
        const isCard = ['Vulnerability', 'Task', 'Reminder', 'Snippet'].includes(msg.messageType) || isPoll;

        // Reactions bar
        const reactions = msg.reactions || [];
        const reactBar = reactions.length ? `<div class="pm-reactions-bar" style="display:flex;flex-wrap:wrap;gap:4px;margin-top:4px;">${reactions.map(r =>
            `<button class="mud-chip mud-chip-filled mud-chip-size-small mud-ripple ${r.userIds.includes(_myId) ? 'mud-chip-color-primary' : ''}" style="padding:0 8px;"
                     onclick="pmChat.addReaction(${msg.id}, '${pmEscape(r.emoji)}')"
                     title="${r.count} người">
                <span class="mud-chip-content">${pmEscape(r.emoji)} <span style="margin-left:4px;font-size:0.75rem;">${r.count}</span></span>
             </button>`
        ).join('')}</div>` : '';

        // Message actions (on hover)
        const actions = `
        <div class="pm-msg-actions" style="display:flex; gap:4px; opacity:0; transition:opacity 0.2s; position:absolute; ${isMine ? 'right:100%; margin-right:8px;' : 'left:100%; margin-left:8px;'} top:50%; transform:translateY(-50%);">
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.showReactionPicker(${msg.id}, event)" title="Cảm xúc" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label">😀</span>
            </button>
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.startReply(${msg.id})" title="Trả lời" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M10 9V5l-7 7 7 7v-4.1c5 0 8.5 1.6 11 5.1-1-5-4-10-11-11z"/></svg></span>
            </button>
            ${isMine ? `
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.editMessage(${msg.id})" title="Sửa" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg></span>
            </button>
            <button class="mud-button-root mud-icon-button mud-icon-button-color-error mud-icon-button-size-small mud-ripple" onclick="pmChat.deleteMessage(${msg.id})" title="Thu hồi" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/></svg></span>
            </button>` : ''}
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.pinMessage(${msg.id}, ${!msg.isPinned})" title="${msg.isPinned ? 'Bỏ ghim' : 'Ghim'}" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24" fill="${msg.isPinned ? 'currentColor' : 'none'}"><path d="M16 12V4h1V2H7v2h1v8l-2 2v2h5.2v6h1.6v-6H18v-2l-2-2z"/></svg></span>
            </button>
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.forwardMessage(${msg.id})" title="Chuyển tiếp" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M12 8V4l8 8-8 8v-4H4V8z"/></svg></span>
            </button>
            <button class="mud-button-root mud-icon-button mud-icon-button-size-small mud-ripple" onclick="pmChat.saveMessage(${msg.id})" title="Lưu tin nhắn" style="background:var(--mud-palette-surface); box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                <span class="mud-icon-button-label"><svg class="mud-icon-root mud-svg-icon mud-icon-size-small" viewBox="0 0 24 24"><path d="M17 3H7c-1.1 0-2 .9-2 2v16l7-3 7 3V5c0-1.1-.9-2-2-2z"/></svg></span>
            </button>
        </div>`;

        wrap.innerHTML = `
        <div style="display:flex;align-items:flex-end;gap:8px;width:100%;${isMine ? 'flex-direction:row-reverse;' : ''}">
            ${!isMine ? `<div style="flex-shrink:0;align-self:flex-end;">${avatarHtml(msg.senderDisplayName, msg.senderAvatarUrl, 30)}</div>` : ''}
            <div style="max-width:68%;position:relative; ${isCard ? 'min-width: min(300px, 100%);' : ''}" onmouseenter="this.querySelector('.pm-msg-actions').style.opacity=1;" onmouseleave="this.querySelector('.pm-msg-actions').style.opacity=0;">
                ${!isMine ? name : ''}
                <div class="pm-msg-bubble" style="padding:8px 12px; border-radius:${isMine ? '16px 16px 4px 16px' : '16px 16px 16px 4px'}; word-break:break-word;overflow-wrap:break-word; ${isMine ? 'background:linear-gradient(135deg,#2563EB,#3B82F6);color:white;' : 'background:var(--mud-palette-surface);border:1px solid rgba(0,0,0,.07);'} box-shadow:0 1px 3px rgba(0,0,0,.08); display:block; ${isCard ? 'min-width: min(300px, 100%);' : ''}">
                    ${replyHtml}
                    ${contentHtml}
                    ${attachHtml}
                    ${pollHtml}
                    <div style="display:flex;align-items:center;justify-content:flex-end;gap:4px;margin-top:4px;word-break:normal;overflow-wrap:normal;white-space:nowrap;flex-shrink:0;">
                        ${time}${edited}${read}
                    </div>
                </div>
                ${reactBar}
                ${actions}
            </div>
        </div>`;

        return wrap;
    }

    function _buildPollHtml(msg) {
        const poll = msg.poll;
        const total = (poll.options || []).reduce((s, o) => s + (o.voteCount || 0), 0);
        const opts = (poll.options || []).map((o, i) => {
            const pct = total ? Math.round(o.voteCount / total * 100) : 0;
            const voted = poll.myVoteOptionIds?.includes(o.id) || o.voterIds?.includes(_myId) || o.votedByUserIds?.includes(_myId);
            return `<div class="mud-list-item mud-list-item-gutters mud-list-item-clickable mud-ripple" style="margin:4px 0; border:1px solid var(--mud-palette-divider); border-radius:4px; padding:8px 12px; ${voted ? 'background:rgba(59,130,246,.15); border-color:var(--mud-palette-primary);' : ''}" onclick="pmChat.votePoll(${msg.id}, ${poll.id}, ${o.id})">
                <div class="mud-list-item-text" style="width:100%;">
                    <div style="display:flex;justify-content:space-between;margin-bottom:4px; font-size:.85rem;">
                        <span>${pmEscape(o.text)}</span>
                        <span style="font-size:.75rem;color:var(--mud-palette-text-secondary);">${o.voteCount} (${pct}%)</span>
                    </div>
                    <div style="height:4px;background:rgba(0,0,0,.08);border-radius:2px;overflow:hidden;">
                        <div style="height:100%;width:${pct}%;background:var(--mud-palette-primary);transition:.3s;"></div>
                    </div>
                </div>
            </div>`;
        }).join('');
        return `<div class="mud-paper mud-elevation-1 mud-card pm-poll-card" style="min-width: 100%; width: 100%; margin-top:8px;">
            <div class="mud-card-content" style="padding:12px;">
                <p class="mud-typography mud-typography-subtitle2" style="margin-bottom:8px;">📊 ${pmEscape(poll.question)}</p>
                ${opts}
                <p class="mud-typography mud-typography-caption" style="color:var(--mud-palette-text-secondary); margin-top:8px; display:block;">${total} lượt bình chọn · ${poll.allowMultiple ? 'Nhiều lựa chọn' : 'Một lựa chọn'}</p>
            </div>
        </div>`;
    }

    // ── Send message ──────────────────────────────────────────────
    async function sendMessage() {
        if (!_active) return;
        const input = $('pm-msg-input');
        if (!input) return;

        const content = input.value.trim();
        const hasFile = _fileAttach;

        if (!content && !hasFile) return;

        // Slash commands
        if (content && !hasFile) {
            // /reminder <amount><m|h|d> <text>
            const reminderMatch = content.match(/^\/reminder\s+(\d+)([mhd])\s+([\s\S]+)$/i);
            if (reminderMatch) {
                const val = parseInt(reminderMatch[1]);
                const unit = reminderMatch[2].toLowerCase();
                const text = reminderMatch[3].trim();
                const msMap = { m: 60000, h: 3600000, d: 86400000 };
                const remindAt = new Date(Date.now() + val * (msMap[unit] || 60000)).toISOString();
                await pmApi.post('/api/reminders', {
                    text,
                    remindAtUtc: remindAt,
                    groupId: _active.groupId || undefined,
                    peerUserId: _active.peerId || undefined
                });
                input.value = '';
                cancelReply();
                _stopTyping();
                const unitLabel = { m: 'phút', h: 'giờ', d: 'ngày' }[unit] || 'phút';
                pmToast.success(`⏰ Đặt nhắc nhở sau ${val} ${unitLabel}`);
                return;
            }

            // /ioc command — save to DB before sending message
            const iocMatch = content.match(/^\s*\/ioc\s+([a-zA-Z0-9]+)\s+([\s\S]+?)\s*$/i);
            if (iocMatch && _active.groupId) {
                const iocType = (iocMatch[1] || '').toLowerCase().toUpperCase();
                const iocValue = (iocMatch[2] || '').trim();
                const r = await pmApi.post('/api/iocs/from-command', {
                    rawCommand: content,
                    groupId: _active.groupId
                });
                if (r) {
                    pmToast.success(`🛡️ Đã lưu IOC vào database`);
                } else {
                    pmToast.warn('⚠️ IOC không hợp lệ hoặc đã tồn tại — message vẫn được gửi');
                }
                // Tiếp tục gửi message bình thường để mọi người thấy trong chat
            }

            // /task <title> (requires group context)
            const taskMatch = content.match(/^\/task\s+([\s\S]+)$/i);
            if (taskMatch && _active.groupId) {
                const title = taskMatch[1].trim();
                const r = await pmApi.post('/api/tasks', {
                    title,
                    groupId: _active.groupId,
                    priority: 'Medium',
                    status: 'Open'
                });
                if (r) {
                    input.value = '';
                    cancelReply();
                    _stopTyping();
                    pmToast.success(`✅ Tạo task: "${title}" — vào /tasks để xem`);
                    return;
                } else {
                    pmToast.error('❌ Tạo task thất bại. Kiểm tra console.');
                    return;
                }
            } else if (taskMatch && !_active.groupId) {
                pmToast.warn('/task chỉ dùng được trong nhóm');
                return;
            }

            // /snippet — mở trang snippets
            if (content.toLowerCase().trim() === '/snippet') {
                const input = $('pm-msg-input');
                if (input) input.value = '';
                window.location.href = '/snippets';
                return;
            }

            // /vuln <severity> <title> — tạo Pentest Finding từ chat
            const vulnMatch = content.match(/^\s*\/vuln\s+(info|low|medium|high|critical)\s+([\s\S]+?)$/i);
            if (vulnMatch) {
                if (!_active.groupId) {
                    pmToast.warn('⚠️ /vuln chỉ dùng được trong nhóm');
                    return;
                }
                const vulnResult = await pmApi.post('/api/pentest/findings/from-command', {
                    rawCommand: content,
                    groupId: _active.groupId
                });
                if (vulnResult) {
                    pmToast.success(`🎯 Đã tạo Finding: ${vulnMatch[2].trim().substring(0, 40)}`);
                } else {
                    pmToast.warn('⚠️ /vuln không hợp lệ — cú pháp: /vuln high SQL Injection on login');
                }
                return;
            }
        }

        // File upload
        if (hasFile) {
            const fd = new FormData();
            fd.append('file', hasFile);
            if (_active.groupId) fd.append('groupId', _active.groupId);
            else fd.append('receiverId', _active.peerId);
            if (_replyId) fd.append('replyToMessageId', _replyId);
            if (content) fd.append('content', content);
            if (_ttl) fd.append('expiresInSeconds', _ttl);

            const r = await pmApi.postForm('/api/messages/upload', fd);
            if (!r) return;
            _fileAttach = null;
            _clearFilePreview();
        } else {
            const body = {
                content,
                receiverId: _active.peerId || undefined,
                groupId: _active.groupId || undefined,
                replyToMessageId: _replyId || undefined,
                expiresInSeconds: _ttl || undefined
            };
            const r = await pmApi.post('/api/messages', body);
            if (!r) return;
        }

        input.value = '';
        input.style.height = 'auto';
        cancelReply();
        _ttl = null;
        _updateTtlBtn();
        _stopTyping();
    }

    // ── Input handlers ────────────────────────────────────────────
    function handleInput(e) {
        const v = e.target.value;
        e.target.style.height = 'auto';
        e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px';

        if (v.length > 0) _startTyping(); else _stopTyping();

        // Mention autocomplete detection
        const cursor = e.target.selectionStart;
        const textBefore = v.slice(0, cursor);
        const lastAt = textBefore.lastIndexOf('@');
        const queryText = textBefore.slice(lastAt + 1);

        if (lastAt >= 0 && !/\s/.test(queryText)) {
            _showMentionDropdown(queryText, lastAt);
        } else {
            _hideMentionDropdown();
        }

        // Slash command popup (only when input starts with '/')
        if (v.startsWith('/')) {
            _handleSlashPopup(v);
        }
    }

    function handleKeydown(e) {
        const dropdown = $('pm-mention-dropdown');
        const isOpen = dropdown && dropdown.style.display === 'block';

        if (isOpen) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                _selectedMentionIndex = (_selectedMentionIndex + 1) % _mentionFilteredList.length;
                _showMentionDropdown(_mentionQuery, _mentionStartIndex);
                return;
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                _selectedMentionIndex = (_selectedMentionIndex - 1 + _mentionFilteredList.length) % _mentionFilteredList.length;
                _showMentionDropdown(_mentionQuery, _mentionStartIndex);
                return;
            }
            if (e.key === 'Enter') {
                e.preventDefault();
                insertMention(_selectedMentionIndex);
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                _hideMentionDropdown();
                return;
            }
        }

        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    }

    function _showMentionDropdown(query, startIndex) {
        if (!_active?.groupId) return;
        _mentionQuery = query.toLowerCase();
        _mentionStartIndex = startIndex;

        const candidates = [];
        if ('all'.startsWith(_mentionQuery)) {
            candidates.push({ isAll: true, username: 'all', displayName: 'Cả nhóm (All)' });
        }

        const matches = _activeGroupMembers.filter(m => 
            m.userId !== _myId && (
                (m.username && m.username.toLowerCase().startsWith(_mentionQuery)) ||
                (m.displayName && m.displayName.toLowerCase().startsWith(_mentionQuery))
            )
        );

        matches.forEach(m => {
            candidates.push({
                isAll: false,
                userId: m.userId,
                username: m.username,
                displayName: m.displayName,
                avatarUrl: m.avatarUrl
            });
        });

        _mentionFilteredList = candidates;

        if (candidates.length === 0) {
            _hideMentionDropdown();
            return;
        }

        const dropdown = $('pm-mention-dropdown');
        if (!dropdown) return;

        dropdown.style.display = 'block';
        _selectedMentionIndex = Math.min(_selectedMentionIndex, candidates.length - 1);
        if (_selectedMentionIndex < 0) _selectedMentionIndex = 0;

        dropdown.innerHTML = candidates.map((c, i) => {
            const activeStyle = i === _selectedMentionIndex ? 'background:var(--mud-palette-action-hover);' : '';
            const avatarHtml = c.isAll 
                ? `<div class="mud-avatar mud-avatar-small mud-avatar-filled mud-avatar-filled-primary" style="flex-shrink:0;">📢</div>`
                : `<div class="mud-avatar mud-avatar-small mud-avatar-filled" style="flex-shrink:0;background:var(--mud-palette-primary-hover);">
                    ${c.avatarUrl ? `<img src="${pmApi.backendUrl}${c.avatarUrl}" style="width:100%;height:100%;border-radius:50%;object-fit:cover;">` : c.displayName[0].toUpperCase()}
                   </div>`;

            return `
            <div class="pm-mention-item" style="display:flex; align-items:center; gap:8px; padding:6px 12px; cursor:pointer; ${activeStyle}"
                 onclick="pmChat.insertMention(${i})">
                ${avatarHtml}
                <div style="display:flex; flex-direction:column; min-width:0; line-height:1.2;">
                    <span style="font-size:0.85rem; font-weight:600; color:var(--mud-palette-text-primary); text-overflow:ellipsis; overflow:hidden; white-space:nowrap;">${pmEscape(c.displayName)}</span>
                    <span style="font-size:0.72rem; color:var(--mud-palette-text-secondary);">@${pmEscape(c.username)}</span>
                </div>
            </div>`;
        }).join('');
    }

    function _hideMentionDropdown() {
        const dropdown = $('pm-mention-dropdown');
        if (dropdown) dropdown.style.display = 'none';
        _mentionStartIndex = -1;
        _mentionFilteredList = [];
    }

    function insertMention(index) {
        const item = _mentionFilteredList[index];
        if (!item) return;

        const input = $('pm-msg-input');
        if (!input) return;

        const v = input.value;
        const before = v.slice(0, _mentionStartIndex);
        const after = v.slice(input.selectionStart);

        const mentionText = "@" + item.username + " ";
        input.value = before + mentionText + after;

        const newCursorPos = _mentionStartIndex + mentionText.length;
        input.selectionStart = input.selectionEnd = newCursorPos;

        _hideMentionDropdown();
        input.focus();
    }

    function _startTyping() {
        if (!_conn || _typing) return;
        _typing = true;
        _conn.invoke('StartTyping', _active?.groupId || null, _active?.peerId || null).catch(() => {});
        clearTimeout(_typingTimer);
        _typingTimer = setTimeout(_stopTyping, 3000);
    }

    function _stopTyping() {
        if (!_conn || !_typing) return;
        _typing = false;
        _conn.invoke('StopTyping', _active?.groupId || null, _active?.peerId || null).catch(() => {});
    }

    // ── Reply ─────────────────────────────────────────────────────
    function startReply(msgId) {
        const msg = _messages.find(m => m.id === msgId);
        if (!msg) return;
        _replyId = msgId;
        const preview = $('pm-reply-preview');
        if (preview) {
            preview.style.display = 'flex';
            preview.innerHTML = `
                <div style="flex:1;border-left:3px solid var(--mud-palette-primary);padding:4px 10px;font-size:.82rem;">
                    <span style="font-weight:700;color:var(--mud-palette-primary);">${pmEscape(msg.senderDisplayName)}</span>
                    <p style="margin:0;color:var(--mud-palette-text-secondary);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${pmEscape(msg.content || '📎 File')}</p>
                </div>
                <button onclick="pmChat.cancelReply()" style="padding:4px 8px;background:none;border:none;cursor:pointer;color:var(--mud-palette-text-secondary);font-size:1.2rem;">×</button>`;
        }
        $('pm-msg-input')?.focus();
    }

    function cancelReply() {
        _replyId = null;
        const preview = $('pm-reply-preview');
        if (preview) { preview.style.display = 'none'; preview.innerHTML = ''; }
    }

    // ── Reactions ─────────────────────────────────────────────────
    function showReactionPicker(msgId, e) {
        _activeReactionMsgId = msgId;
        const picker = $('pm-reaction-picker');
        if (!picker) return;
        picker.style.display = 'flex';
        const rect = e.target.getBoundingClientRect();
        picker.style.position = 'fixed';
        picker.style.left = Math.min(rect.left, window.innerWidth - 200) + 'px';
        picker.style.top  = (rect.top - 50) + 'px';

        // close on outside click
        setTimeout(() => {
            const close = (ev) => { if (!picker.contains(ev.target)) { picker.style.display = 'none'; document.removeEventListener('click', close); } };
            document.addEventListener('click', close);
        }, 50);
    }

    async function addReaction(msgId, emoji) {
        $('pm-reaction-picker')?.style.setProperty('display', 'none');
        const msg = _messages.find(m => m.id === msgId);
        const existing = msg?.reactions?.find(r => r.emoji === emoji);
        if (existing?.userIds?.includes(_myId)) {
            await pmApi.delete(`/api/messages/${msgId}/reactions?emoji=${encodeURIComponent(emoji)}`);
        } else {
            await pmApi.post(`/api/messages/${msgId}/reactions`, { emoji });
        }
    }

    async function addReactionActive(emoji) {
        if (!_activeReactionMsgId) return;
        await addReaction(_activeReactionMsgId, emoji);
    }

    // ── Pin ───────────────────────────────────────────────────────
    async function pinMessage(msgId, pin) {
        await pmApi.patch(`/api/messages/${msgId}/pin`, { isPinned: pin });
        if (pin) pmToast.success('Đã ghim tin nhắn');
        else pmToast.success('Đã bỏ ghim');
        // Update local
        const msg = _messages.find(m => m.id === msgId);
        if (msg) msg.isPinned = pin;
        const el = document.querySelector(`[data-msg-id="${msgId}"]`);
        if (el) {
            // refresh the pin button icon
            const pinBtn = el.querySelector('.pm-msg-actions button:last-child svg');
            if (pinBtn) pinBtn.setAttribute('fill', pin ? 'currentColor' : 'none');
            // refresh title
            const btn = el.querySelector('.pm-msg-actions button:last-child');
            if (btn) btn.title = pin ? 'Bỏ ghim' : 'Ghim';
        }
        if (_pinPanelOpen) await loadPins();
    }

    function togglePinPanel() {
        _pinPanelOpen = !_pinPanelOpen;
        const panel = $('pm-pin-panel');
        if (panel) panel.style.display = _pinPanelOpen ? 'flex' : 'none';
        if (_pinPanelOpen) loadPins();
    }

    function closePinPanel() {
        _pinPanelOpen = false;
        const panel = $('pm-pin-panel');
        if (panel) panel.style.display = 'none';
    }

    async function loadPins() {
        if (!_active) return;
        let url = '';
        if (_active.groupId) url = `/api/messages/pinned?groupId=${_active.groupId}`;
        else url = `/api/messages/pinned?peerId=${_active.peerId}`;

        const pins = await pmApi.get(url);
        const container = $('pm-pin-list');
        if (!container) return;
        if (!pins?.length) { container.innerHTML = '<p style="font-size:.82rem;color:var(--mud-palette-text-secondary);text-align:center;padding:16px;">Chưa có tin nhắn ghim</p>'; return; }
        container.innerHTML = pins.map(m => `
            <div class="pm-pin-item" onclick="pmChat.scrollToMessage(${m.id})">
                <div style="display:flex;align-items:center;gap:8px;margin-bottom:6px;">
                    ${avatarHtml(m.senderDisplayName, m.senderAvatarUrl, 24)}
                    <span style="font-size:.78rem;font-weight:700;color:var(--mud-palette-text-primary);">${pmEscape(m.senderDisplayName)}</span>
                    <span style="font-size:.72rem;color:var(--mud-palette-text-secondary);margin-left:auto;">${pmFmt.shortTime(m.createdAt)}</span>
                </div>
                <p style="margin:0;font-size:.82rem;line-height:1.4;color:var(--mud-palette-text-secondary);word-break:break-word;">${pmEscape(m.content || '📎')}</p>
            </div>
        `).join('');
    }

    async function scrollToMessage(msgId) {
        closePinPanel();
        let el = document.querySelector(`[data-msg-id="${msgId}"]`);
        if (!el) {
            // Message is not in DOM. Load context around this message!
            const scroll = $('msgScroll');
            if (scroll) {
                scroll.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100%;"><div class="mud-progress-circular mud-progress-circular-indeterminate" style="width:36px;height:36px;"><svg viewBox="22 22 44 44"><circle class="mud-progress-circular-circle" cx="44" cy="44" r="20.2" fill="none" stroke-width="3.6"></circle></svg></div></div>`;
                const data = await pmApi.get(`/api/messages/${msgId}/context?takeBefore=30&takeAfter=30`);
                if (data && data.length) {
                    _messages = data;
                    _hasMore = true; // Allow loading older messages
                    _oldestMsgId = _messages[0]?.id || null;
                    scroll.innerHTML = '';
                    _messages.forEach(m => _appendMessage(m, false));
                } else {
                    scroll.innerHTML = '';
                    pmToast.error('Không tìm thấy tin nhắn hoặc tin nhắn đã bị xóa.');
                    return;
                }
            }
            el = document.querySelector(`[data-msg-id="${msgId}"]`);
        }

        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            el.style.outline = '2px solid var(--mud-palette-primary)';
            setTimeout(() => el.style.outline = '', 2000);
        }
    }

    // ── Edit / Delete ─────────────────────────────────────────────
    async function editMessage(msgId) {
        const msg = _messages.find(m => m.id === msgId);
        if (!msg) return;
        const newContent = prompt('Chỉnh sửa tin nhắn:', msg.content || '');
        if (newContent == null || newContent === msg.content) return;
        const r = await pmApi.put(`/api/messages/${msgId}`, { content: newContent });
        if (r) {
            msg.content = newContent;
            msg.isEdited = true;
            const textEl = document.querySelector(`[data-msg-id="${msgId}"] .pm-msg-text`);
            if (textEl) textEl.textContent = newContent;
        }
    }

    function deleteMessage(msgId) {
        showConfirm('Thu hồi tin nhắn này?', async () => {
            await pmApi.delete(`/api/messages/${msgId}`);
            const el = document.querySelector(`[data-msg-id="${msgId}"]`);
            if (el) {
                const body = el.querySelector('.pm-msg-bubble');
                if (body) body.innerHTML = '<em style="opacity:.5;font-size:.82rem;">Tin nhắn đã bị thu hồi</em>';
            }
        });
    }

    // ── Poll vote ─────────────────────────────────────────────────
    async function votePoll(msgId, pollId, optionId) {
        await pmApi.post(`/api/polls/${pollId}/vote`, { optionIds: [optionId] });
    }

    // ── File attachment ───────────────────────────────────────────
    function openFileAttach() { $('pm-file-input')?.click(); }

    function handleFileSelect(e) {
        // Called as onchange="pmChat.handleFileSelect(this)" — `e` is the <input> element,
        // but may also be a real event; support both.
        const inputEl = (e && e.target) ? e.target : e;
        const file = inputEl?.files?.[0];
        if (!file) return;
        if (file.size > 50 * 1024 * 1024) { pmToast.error('File tối đa 50 MB'); inputEl.value = ''; return; }
        _fileAttach = file;
        const preview = $('pm-file-preview');
        if (preview) {
            preview.style.display = 'flex';
            preview.innerHTML = `
                <div style="display:flex;align-items:center;gap:8px;background:rgba(59,130,246,.1);border:1px solid rgba(59,130,246,.3);border-radius:8px;padding:6px 12px;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/></svg>
                    <span style="font-size:.82rem;">${pmEscape(file.name)}</span>
                    <button onclick="pmChat.clearFile()" style="background:none;border:none;cursor:pointer;color:var(--mud-palette-text-secondary);font-size:1rem;">×</button>
                </div>`;
        }
    }

    function clearFile() {
        _fileAttach = null;
        _clearFilePreview();
        const inp = $('pm-file-input');
        if (inp) inp.value = '';
    }

    function _clearFilePreview() {
        const p = $('pm-file-preview');
        if (p) { p.style.display = 'none'; p.innerHTML = ''; }
    }

    // ── Voice recording (MediaRecorder) ──────────────────────────
    let _isRecording   = false;
    let _mediaRecorder = null;
    let _recordChunks  = [];
    let _recordTimer   = null;
    let _recordStart   = 0;

    async function startRecording() {
        if (!_active) return;
        if (_isRecording) return;
        if (!navigator.mediaDevices?.getUserMedia) { pmToast.error('Trình duyệt không hỗ trợ ghi âm'); return; }
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            _mediaRecorder = new MediaRecorder(stream);
            _recordChunks = [];
            _mediaRecorder.ondataavailable = e => { if (e.data && e.data.size > 0) _recordChunks.push(e.data); };
            _mediaRecorder.start();
            _isRecording = true;
            _recordStart = Date.now();
            _showRecordingUI(true);
            _updateRecordTime();
            _recordTimer = setInterval(_updateRecordTime, 250);
        } catch (err) {
            pmToast.error('Không thể truy cập Microphone: ' + (err?.message || err));
        }
    }

    function _stopMicStream() {
        try { _mediaRecorder?.stream?.getTracks().forEach(t => t.stop()); } catch (e) {}
    }

    function cancelRecording() {
        if (!_isRecording) return;
        clearInterval(_recordTimer);
        try {
            if (_mediaRecorder && _mediaRecorder.state !== 'inactive') {
                _mediaRecorder.onstop = null;
                _mediaRecorder.stop();
            }
        } catch (e) {}
        _stopMicStream();
        _isRecording = false;
        _recordChunks = [];
        _showRecordingUI(false);
    }

    async function stopAndSendRecording() {
        if (!_isRecording || !_mediaRecorder) return;
        clearInterval(_recordTimer);
        const mr = _mediaRecorder;
        const blob = await new Promise(resolve => {
            mr.onstop = () => resolve(new Blob(_recordChunks, { type: mr.mimeType || 'audio/webm' }));
            if (mr.state !== 'inactive') mr.stop();
            else resolve(new Blob(_recordChunks, { type: 'audio/webm' }));
        });
        _stopMicStream();
        _isRecording = false;
        _showRecordingUI(false);

        if (!blob || blob.size === 0) { pmToast.error('Bản ghi rỗng'); return; }

        const fd = new FormData();
        const stamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 14);
        fd.append('file', blob, `voice_message_${stamp}.webm`);
        if (_active.groupId) fd.append('groupId', _active.groupId);
        else fd.append('receiverId', _active.peerId);
        if (_replyId) fd.append('replyToMessageId', _replyId);

        const r = await pmApi.postForm('/api/messages/upload', fd);
        if (r) { cancelReply(); }
    }

    function _updateRecordTime() {
        const el = $('pm-record-time');
        if (!el) return;
        const s = Math.floor((Date.now() - _recordStart) / 1000);
        const mm = String(Math.floor(s / 60)).padStart(2, '0');
        const ss = String(s % 60).padStart(2, '0');
        el.textContent = mm + ':' + ss;
    }

    function _showRecordingUI(on) {
        const normal = $('pm-input-normal');
        const bar    = $('pm-recording-bar');
        if (normal) normal.style.display = on ? 'none' : 'flex';
        if (bar)    bar.style.display    = on ? 'flex' : 'none';
    }

    // Routed from the send button: send the recording if recording, else the text message.
    function onSendClick() {
        if (_isRecording) stopAndSendRecording();
        else sendMessage();
    }

    // ── More menu ─────────────────────────────────────────────────
    function toggleMoreMenu() {
        const menu = $('pm-more-dropdown');
        if (!menu) return;

        _moreMenuOpen = !_moreMenuOpen;
        
        if (_moreMenuOpen) {
            _buildMoreMenu(menu);
            menu.style.display = 'block';
            setTimeout(() => {
                const close = (ev) => { if (!menu.contains(ev.target)) { menu.style.display = 'none'; _moreMenuOpen = false; document.removeEventListener('click', close); }};
                document.addEventListener('click', close);
            }, 50);
        } else {
            menu.style.display = 'none';
        }
    }

    function _menuBtn(label, svgPath, onclick, danger) {
        return `<button type="button" ${danger ? 'class="danger"' : ''} onclick="${onclick}">
            <span class="pm-menu-icon"><svg viewBox="0 0 24 24" style="width:20px;height:20px;fill:currentColor;"><path d="${svgPath}"/></svg></span>
            ${label}
        </button>`;
    }

    function _buildMoreMenu(menu) {
        let html = '';
        const escapedName = pmEscape(_active.name || '');

        if (_active.groupId) {
            html += _menuBtn('Quản lý nhóm', 'M3 13h2v-2H3v2zm0 4h2v-2H3v2zm0-8h2V7H3v2zm4 4h14v-2H7v2zm0 4h14v-2H7v2zM7 7v2h14V7H7z', `window.location.href='/groups/${_active.groupId}'`);
            html += _menuBtn('Tệp đã chia sẻ', 'M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10z', `pmDialogs.openSharedFiles(${_active.groupId},null,'${escapedName}')`);
            html += _menuBtn('Thêm thành viên', 'M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z', `pmDialogs.openAddMember(${_active.groupId},'${escapedName}')`);
            html += _menuBtn('Quản lý thành viên', 'M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z', `pmDialogs.openGroupMembers(${_active.groupId},'${escapedName}')`);
            html += _menuBtn('Đổi tên & ảnh nhóm', 'M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z', `pmDialogs.openEditGroup(${_active.groupId},'${escapedName}','${_active.avatar||''}')`);
            html += '<div class="pm-chat-divider"></div>';
            html += _menuBtn('Xóa đoạn chat', 'M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z', `pmChat.clearConversation()`, true);
            html += _menuBtn('Rời nhóm', 'M10.09 15.59L11.5 17l5-5-5-5-1.41 1.41L12.67 11H3v2h9.67l-2.58 2.59zM19 3H5c-1.11 0-2 .9-2 2v4h2V5h14v14H5v-4H3v4c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z', `pmChat.leaveGroup(${_active.groupId})`, true);
        } else if (_active.peerId) {
            html += _menuBtn('Tệp đã chia sẻ', 'M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10z', `pmDialogs.openSharedFiles(null,${_active.peerId},'${escapedName}')`);
            html += _menuBtn('Đặt biệt danh', 'M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z', `pmDialogs.openNickname(${_active.peerId},'${escapedName}')`);
            html += '<div class="pm-chat-divider"></div>';
            html += _menuBtn('Xóa đoạn chat', 'M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z', `pmChat.clearConversation()`, true);
            html += _menuBtn('Chặn người dùng', 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM4 12c0-4.42 3.58-8 8-8 1.85 0 3.55.63 4.9 1.68L5.68 16.9C4.63 15.55 4 13.85 4 12zm8 8c-1.85 0-3.55-.63-4.9-1.68L18.32 7.1C19.37 8.45 20 10.15 20 12c0 4.42-3.58 8-8 8z', `pmChat.blockUser(${_active.peerId})`, true);
        }

        menu.innerHTML = html;
    }

    // ── Emoji ─────────────────────────────────────────────────────
    function toggleEmoji(event) {
        const picker = $('pm-emoji-picker');
        if (!picker) return;
        const visible = picker.style.display !== 'none';
        if (visible) { picker.style.display = 'none'; return; }
        const btn = (event?.target || event?.srcElement)?.closest('button');
        if (btn) {
            const rect = btn.getBoundingClientRect();
            picker.style.left = Math.max(0, rect.left) + 'px';
            picker.style.bottom = (window.innerHeight - rect.top + 8) + 'px';
            picker.style.top = 'auto';
        }
        picker.style.display = 'block';
        setTimeout(() => {
            const close = (e) => { if (!picker.contains(e.target) && !e.target.closest('button[onclick*="toggleEmoji"]')) { picker.style.display = 'none'; document.removeEventListener('click', close); }};
            document.addEventListener('click', close);
        }, 50);
    }

    function insertEmoji(emoji) {
        const input = $('pm-msg-input');
        if (!input) return;
        const start = input.selectionStart || 0;
        const end   = input.selectionEnd   || 0;
        input.value = input.value.slice(0, start) + emoji + input.value.slice(end);
        input.setSelectionRange(start + emoji.length, start + emoji.length);
        input.focus();
        const picker = $('pm-emoji-picker');
        if (picker) picker.style.display = 'none';
    }

    // ── Poll creation ─────────────────────────────────────────────
    function openPollModal() {
        const modal = $('pm-poll-modal');
        if (!modal) return;
        modal.querySelector('#pm-poll-question').value = '';
        const opts = modal.querySelector('#pm-poll-options');
        opts.innerHTML = `
            <div class="pm-poll-option-row" style="display:flex;gap:8px;margin-bottom:8px;">
                <input type="text" placeholder="Lựa chọn 1" style="flex:1;padding:8px 12px;border:1.5px solid var(--mud-palette-lines-inputs);border-radius:8px;background:transparent;color:var(--mud-palette-text-primary);font-size:.9rem;outline:none;">
                <button type="button" onclick="this.parentElement.remove()" style="background:none;border:none;cursor:pointer;color:var(--mud-palette-text-secondary);font-size:1.2rem;">✕</button>
            </div>
            <div class="pm-poll-option-row" style="display:flex;gap:8px;margin-bottom:8px;">
                <input type="text" placeholder="Lựa chọn 2" style="flex:1;padding:8px 12px;border:1.5px solid var(--mud-palette-lines-inputs);border-radius:8px;background:transparent;color:var(--mud-palette-text-primary);font-size:.9rem;outline:none;">
                <button type="button" onclick="this.parentElement.remove()" style="background:none;border:none;cursor:pointer;color:var(--mud-palette-text-secondary);font-size:1.2rem;">✕</button>
            </div>`;
        modal.querySelector('#pm-poll-multiple').checked = false;
        modal.style.display = 'flex';
    }

    function closePollModal() {
        const modal = $('pm-poll-modal');
        if (modal) modal.style.display = 'none';
    }

    function addPollOption() {
        const opts = $('pm-poll-options');
        if (!opts) return;
        const count = opts.querySelectorAll('.pm-poll-option-row').length + 1;
        if (count > 8) { pmToast.warn('Tối đa 8 lựa chọn'); return; }
        const row = document.createElement('div');
        row.className = 'pm-poll-option-row';
        row.style.cssText = 'display:flex;gap:8px;margin-bottom:8px;';
        row.innerHTML = `
            <input type="text" placeholder="Lựa chọn ${count}" style="flex:1;padding:8px 12px;border:1.5px solid var(--mud-palette-lines-inputs);border-radius:8px;background:transparent;color:var(--mud-palette-text-primary);font-size:.9rem;outline:none;">
            <button type="button" onclick="this.parentElement.remove()" style="background:none;border:none;cursor:pointer;color:var(--mud-palette-text-secondary);font-size:1.2rem;">✕</button>`;
        opts.appendChild(row);
    }

    async function submitPoll() {
        if (!_active) return;
        const question = $('pm-poll-question')?.value?.trim();
        if (!question) { pmToast.error('Nhập câu hỏi cho bình chọn'); return; }
        const opts = [...document.querySelectorAll('#pm-poll-options input')].map(i => i.value.trim()).filter(Boolean);
        if (opts.length < 2) { pmToast.error('Cần ít nhất 2 lựa chọn'); return; }
        const isMultiple = $('pm-poll-multiple')?.checked || false;

        const body = {
            groupId: _active.groupId || null,
            receiverId: _active.peerId || null,
            question,
            options: opts,
            allowMultiple: isMultiple
        };
        const r = await pmApi.post('/api/polls', body);
        if (r) { closePollModal(); pmToast.success('Đã tạo bình chọn'); }
    }

    // ── In-chat search ────────────────────────────────────────────
    function toggleMsgSearch() {
        _msgSearchOpen = !_msgSearchOpen;
        const bar = $('pm-msg-search-bar');
        if (bar) {
            bar.style.display = _msgSearchOpen ? 'flex' : 'none';
            if (_msgSearchOpen) bar.querySelector('input')?.focus();
        }
    }

    function searchMessages(q) {
        clearTimeout(_searchTimer);
        if (!q) { document.querySelectorAll('.pm-msg-highlight').forEach(e => e.classList.remove('pm-msg-highlight')); return; }
        _searchTimer = setTimeout(() => {
            document.querySelectorAll('[data-msg-id]').forEach(el => {
                const text = el.querySelector('.pm-msg-text')?.textContent?.toLowerCase() || '';
                el.classList.toggle('pm-msg-highlight', text.includes(q.toLowerCase()));
            });
            const first = document.querySelector('.pm-msg-highlight');
            if (first) first.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }, 300);
    }

    // ── Call ──────────────────────────────────────────────────────
    async function initiateCall(isVideo) {
        if (!_active?.peerId || !_conn) return;
        if (window.pmWebRTC?.startCall) {
            window.pmWebRTC.startCall(_active.peerId, isVideo);
        } else {
            pmToast.error('WebRTC không khả dụng');
        }
    }

    // ── Confirm dialog ────────────────────────────────────────────
    function showConfirm(message, cb, title, okText) {
        _confirmCb = cb;
        const overlay = $('pm-confirm-overlay');
        const textEl  = $('pm-confirm-text');
        const titleEl = $('pm-confirm-title');
        const okEl    = $('pm-confirm-ok-text');
        if (textEl)  textEl.textContent  = message || '';
        if (titleEl) titleEl.textContent = title  || 'Xác nhận';
        if (okEl)    okEl.textContent     = okText || 'Xác nhận';
        if (overlay) overlay.style.display = 'flex';
    }

    function confirmOk() {
        $('pm-confirm-overlay')?.style.setProperty('display', 'none');
        _confirmCb?.();
        _confirmCb = null;
    }

    function confirmCancel() {
        $('pm-confirm-overlay')?.style.setProperty('display', 'none');
        _confirmCb = null;
    }

    // ── Actions ───────────────────────────────────────────────────
    function leaveGroup(groupId) {
        showConfirm('Bạn có chắc chắn muốn rời nhóm này?', async () => {
            try {
                await pmApi.delete(`/api/groups/${groupId}/leave`);
                pmToast.success('Đã rời nhóm');
                window.location.href = '/chat';
            } catch (e) {
                pmToast.error('Không thể rời nhóm');
            }
        });
    }

    function clearConversation() {
        if (!_active) return;
        const isGroup = !!_active.groupId;
        const msg = isGroup
            ? 'Bạn có chắc chắn muốn xóa đoạn chat nhóm này không? Lịch sử sẽ chỉ bị xóa khỏi thiết bị của bạn.'
            : 'Bạn có chắc chắn muốn xóa cuộc trò chuyện này không? Toàn bộ tin nhắn sẽ bị xóa vĩnh viễn ở cả 2 bên.';
        showConfirm(msg, async () => {
            const q = isGroup ? `groupId=${_active.groupId}` : `peerId=${_active.peerId}`;
            const r = await pmApi.delete(`/api/conversations/clear?${q}`);
            if (r !== null) {
                pmToast.success('Đã xóa đoạn chat');
                _messages = [];
                const scroll = $('msgScroll');
                if (scroll) scroll.innerHTML = '';
                await loadConversations();
            } else {
                pmToast.error('Lỗi xóa đoạn chat');
            }
        }, 'Xóa đoạn chat?', 'Xóa');
    }

    function blockUser(peerId) {
        showConfirm('Bạn có chắc chắn muốn chặn người dùng này?', async () => {
            try {
                await pmApi.post('/api/blocks/' + peerId);
                pmToast.success('Đã chặn người dùng');
                window.location.href = '/chat';
            } catch (e) {
                pmToast.error('Không thể chặn người dùng');
            }
        });
    }

    function getPartnerName(peerId) {
        if (_active?.peerId === peerId) return _active.name;
        const c = _convs.find(x => x.peerId === peerId);
        if (c) return c.peerDisplayName || c.groupName;
        return 'Người dùng';
    }

    function getPartnerAvatar(peerId) {
        if (_active?.peerId === peerId) return _active.avatar || '';
        const c = _convs.find(x => x.peerId === peerId);
        if (c) return c.peerAvatarUrl || '';
        return '';
    }

    function showEditHistory(el) {
        try {
            const historyStr = el.getAttribute('data-history');
            if (historyStr && window.pmDialogs) {
                const h = JSON.parse(historyStr);
                pmDialogs.openEditHistory(h);
            }
        } catch (e) { console.error('Error parsing history', e); }
    }

    // ── Edit history ─────────────────────────────────────────────
    async function loadEditHistory(msgId) {
        const history = await pmApi.get(`/api/messages/${msgId}/history`);
        if (!history || !history.length) { pmToast.info('Không có lịch sử chỉnh sửa'); return; }
        if (window.pmDialogs?.openEditHistory) {
            pmDialogs.openEditHistory(history);
        } else {
            // Fallback: show as alert
            const lines = history.map((h, i) => `[${i + 1}] ${h.content} (${new Date(h.editedAt).toLocaleString()})`).join('\n');
            alert('Lịch sử chỉnh sửa:\n' + lines);
        }
    }

    // ── Forward message ──────────────────────────────────────────
    function forwardMessage(msgId) {
        _forwardMsgId = msgId;
        const modal = document.getElementById('pm-forward-modal');
        if (!modal) return;
        const list = document.getElementById('pm-forward-list');
        if (list) {
            list.innerHTML = _convs.length === 0
                ? '<p style="text-align:center;padding:24px;color:var(--mud-palette-text-secondary);">Không có cuộc trò chuyện nào</p>'
                : _convs.map(c => {
                    const cName   = c.peerDisplayName || c.groupName || '?';
                    const cAvatar = c.peerAvatarUrl || c.groupAvatarUrl || '';
                    return `
                    <div onclick="pmChat.submitForward('${c.peerId || ''}','${c.groupId || ''}')"
                         style="display:flex;align-items:center;gap:12px;padding:10px 16px;cursor:pointer;border-radius:10px;transition:.15s;"
                         onmouseover="this.style.background='var(--mud-palette-action-hover)'" onmouseout="this.style.background='transparent'">
                        ${avatarHtml(cName, cAvatar, 38)}
                        <div>
                            <div style="font-weight:700;font-size:.9rem;">${pmEscape(cName)}</div>
                            <div style="font-size:.75rem;color:var(--mud-palette-text-secondary);">${c.groupId ? 'Nhóm' : 'Tin nhắn trực tiếp'}</div>
                        </div>
                    </div>`; }).join('');
        }
        modal.style.display = 'flex';
    }

    async function submitForward(peerIdStr, groupIdStr) {
        if (!_forwardMsgId) return;
        const peerId  = peerIdStr  ? parseInt(peerIdStr)  : null;
        const groupId = groupIdStr ? parseInt(groupIdStr) : null;
        
        let url = `/api/messages/${_forwardMsgId}/forward`;
        const params = [];
        if (peerId) params.push(`receiverId=${peerId}`);
        if (groupId) params.push(`groupId=${groupId}`);
        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        const r = await pmApi.post(url, {});
        if (r) {
            pmToast.success('Đã chuyển tiếp tin nhắn');
            closeForwardModal();
        }
    }

    function closeForwardModal() {
        _forwardMsgId = null;
        const modal = document.getElementById('pm-forward-modal');
        if (modal) modal.style.display = 'none';
    }

    // ── Save message ─────────────────────────────────────────────
    async function saveMessage(msgId) {
        const r = await pmApi.post(`/api/saved-messages/${msgId}`, {});
        if (r !== null) pmToast.success('Đã lưu vào mục Đã lưu');
        else pmToast.error('Không thể lưu tin nhắn');
    }

    // ── TTL helpers ──────────────────────────────────────────────
    function toggleTtlMenu(e) {
        e.stopPropagation();
        const menu = document.getElementById('pm-ttl-menu');
        if (!menu) return;
        const isVisible = menu.style.display !== 'none';
        menu.style.display = isVisible ? 'none' : 'block';
        if (!isVisible) {
            setTimeout(() => document.addEventListener('click', _closeTtlMenu, { once: true }), 10);
        }
    }

    function _closeTtlMenu() {
        const menu = document.getElementById('pm-ttl-menu');
        if (menu) menu.style.display = 'none';
    }

    function setTtl(seconds) {
        _ttl = seconds || null;
        _updateTtlBtn();
        _closeTtlMenu();
    }

    function _updateTtlBtn() {
        const btn = document.getElementById('pm-ttl-btn');
        if (!btn) return;
        if (_ttl) {
            btn.style.color = 'var(--mud-palette-warning)';
            btn.title = `Tự xóa sau ${_ttlLabel(_ttl)} (click để hủy)`;
        } else {
            btn.style.color = 'var(--mud-palette-text-secondary)';
            btn.title = 'Tin nhắn tự xóa';
        }
    }

    function _ttlLabel(s) {
        if (s < 3600) return `${s / 60} phút`;
        if (s < 86400) return `${s / 3600} giờ`;
        return `${s / 86400} ngày`;
    }

    // ── Infinite scroll ───────────────────────────────────────────
    function _setupScrollHandler() {
        const scroll = $('msgScroll');
        if (!scroll) return;
        scroll.addEventListener('scroll', () => {
            if (scroll.scrollTop < 80 && !_loadingMore && _hasMore) {
                loadMoreMessages();
            }
        });
    }

    // ── New DM dialog ─────────────────────────────────────────────
    let _newDmModal = null;
    function openNewDm() {
        if (_newDmModal) { _newDmModal.style.display = 'flex'; return; }
        _newDmModal = document.createElement('div');
        _newDmModal.style.cssText = `
            position:fixed;inset:0;z-index:99999;background:rgba(2,6,23,.6);
            backdrop-filter:blur(4px);display:flex;align-items:center;
            justify-content:center;padding:24px;
        `;
        _newDmModal.innerHTML = `
            <div onclick="event.stopPropagation()" style="
                width:min(440px,100%);background:var(--mud-palette-surface);
                border:1px solid var(--mud-palette-divider);border-radius:20px;
                padding:28px;box-shadow:0 8px 40px rgba(0,0,0,.35);">
                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:20px;">
                    <div>
                        <h6 style="margin:0;font-size:1.1rem;font-weight:800;">Tin nhắn mới</h6>
                        <p style="margin:4px 0 0;font-size:.85rem;color:var(--mud-palette-text-secondary);">
                            Tìm người dùng để bắt đầu trò chuyện.
                        </p>
                    </div>
                    <button onclick="pmChat.closeNewDm()" style="
                        background:none;border:none;cursor:pointer;font-size:1.5rem;
                        color:var(--mud-palette-text-secondary);line-height:1;padding:4px;">×</button>
                </div>
                <div style="position:relative;margin-bottom:12px;">
                    <span style="position:absolute;left:12px;top:50%;transform:translateY(-50%);
                                 display:flex;color:var(--mud-palette-text-secondary);pointer-events:none;">
                        <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor">
                            <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
                        </svg>
                    </span>
                    <input id="pm-newdm-input" type="text" placeholder="Tên, username, email..."
                           style="width:100%;height:42px;padding:0 12px 0 38px;
                                  border:1px solid var(--mud-palette-divider);border-radius:10px;
                                  background:var(--mud-palette-background);
                                  color:var(--mud-palette-text-primary);outline:none;
                                  font-size:.9rem;box-sizing:border-box;"
                           oninput="pmChat._searchDmUsers(this.value)" />
                </div>
                <div id="pm-newdm-results" style="max-height:300px;overflow-y:auto;"></div>
            </div>`;
        _newDmModal.addEventListener('click', closeNewDm);
        document.body.appendChild(_newDmModal);
    }

    function closeNewDm() {
        if (_newDmModal) _newDmModal.style.display = 'none';
    }

    let _dmSearchTimer = null;
    async function _searchDmUsers(q) {
        clearTimeout(_dmSearchTimer);
        const results = document.getElementById('pm-newdm-results');
        if (!results) return;
        if (!q.trim()) { results.innerHTML = ''; return; }
        results.innerHTML = '<p style="text-align:center;padding:16px;color:var(--mud-palette-text-secondary);font-size:.85rem;">Đang tìm...</p>';
        _dmSearchTimer = setTimeout(async () => {
            const data = await pmApi.get(`/api/search?q=${encodeURIComponent(q)}&limit=20`);
            const users = (data?.users || []).filter(u => u.id !== _myId);
            if (!users.length) {
                results.innerHTML = '<p style="text-align:center;padding:16px;color:var(--mud-palette-text-secondary);font-size:.85rem;">Không tìm thấy người dùng.</p>';
                return;
            }
            results.innerHTML = users.map(u => {
                const avatar = u.avatarUrl
                    ? `<img src="${pmEscape(u.avatarUrl)}" style="width:100%;height:100%;object-fit:cover;border-radius:50%;" />`
                    : `<span style="font-weight:700;">${pmEscape((u.displayName||'?')[0].toUpperCase())}</span>`;
                return `<button type="button"
                    onclick="pmChat._startDm(${u.id})"
                    style="width:100%;background:none;border:none;cursor:pointer;padding:10px 12px;
                           border-radius:10px;display:flex;align-items:center;gap:12px;text-align:left;
                           transition:background .12s;"
                    onmouseenter="this.style.background='rgba(var(--mud-palette-primary-rgb),.08)'"
                    onmouseleave="this.style.background='none'">
                    <div style="width:36px;height:36px;flex-shrink:0;border-radius:50%;
                                background:var(--mud-palette-primary);display:flex;
                                align-items:center;justify-content:center;
                                color:white;overflow:hidden;">${avatar}</div>
                    <div style="min-width:0;">
                        <div style="font-weight:700;font-size:.9rem;">${pmEscape(u.displayName)}</div>
                        <div style="font-size:.78rem;color:var(--mud-palette-text-secondary);">@${pmEscape(u.username)}</div>
                    </div>
                </button>`;
            }).join('');
        }, 300);
    }

    function _startDm(userId) {
        closeNewDm();
        window.location.href = `/chat/${userId}`;
    }

    // ── Slash command popup ───────────────────────────────────────
    const SLASH_COMMANDS = [
        { icon: '🛡️', cmd: '/ioc cve',    desc: 'IOC CVE',         example: '/ioc cve CVE-2025-70149' },
        { icon: '🌐', cmd: '/ioc ip',     desc: 'IOC IP',          example: '/ioc ip 8.8.8.8' },
        { icon: '🧬', cmd: '/ioc md5',    desc: 'IOC MD5 hash',    example: '/ioc md5 <hash>' },
        { icon: '🔗', cmd: '/ioc url',    desc: 'IOC URL',         example: '/ioc url https://...' },
        { icon: '🎯', cmd: '/vuln',       desc: 'Pentest Finding',  example: '/vuln high SQL Injection on /login' },
        { icon: '✅', cmd: '/task',       desc: 'Tạo task nhóm',   example: '/task Kiểm tra server' },
        { icon: '⏰', cmd: '/reminder',   desc: 'Đặt nhắc nhở',   example: '/reminder 30m Họp nhóm' },
        { icon: '💻', cmd: '/snippet',    desc: 'Mở Code Snippets', example: '/snippet' },
    ];

    function _handleSlashPopup(val) {
        const dropdown = $('pm-mention-dropdown');
        if (!dropdown) return;
        if (val.includes(' ')) {
            dropdown.style.display = 'none';
            return;
        }
        const q = val.slice(1).toLowerCase();
        const matched = SLASH_COMMANDS.filter(c =>
            c.cmd.slice(1).startsWith(q) || c.desc.toLowerCase().includes(q)
        );
        if (!matched.length) { dropdown.style.display = 'none'; return; }
        dropdown.style.display = 'block';
        dropdown.innerHTML = matched.map(c => `
            <div onclick="pmChat._insertSlash('${c.cmd.replace(/'/g, "\\'")}')"
                 style="padding:8px 12px;cursor:pointer;display:flex;align-items:center;gap:10px;border-radius:6px;"
                 onmouseenter="this.style.background='rgba(var(--mud-palette-primary-rgb),.08)'"
                 onmouseleave="this.style.background='none'">
                <span style="font-size:1.2rem;width:24px;text-align:center;">${c.icon}</span>
                <div style="min-width:0;">
                    <div style="font-weight:700;font-size:.85rem;color:var(--mud-palette-primary);">${pmEscape(c.cmd)}</div>
                    <div style="font-size:.75rem;color:var(--mud-palette-text-secondary);">${pmEscape(c.example)}</div>
                </div>
            </div>`).join('');
    }

    function _insertSlash(cmd) {
        const input = $('pm-msg-input');
        if (!input) return;
        input.value = cmd + ' ';
        input.focus();
        const dropdown = $('pm-mention-dropdown');
        if (dropdown) dropdown.style.display = 'none';
    }

    // ── Init ──────────────────────────────────────────────────────
    async function init(opts = {}) {
        _myId   = opts.myId   || 0;
        _myName = opts.myName || '';

        await initSignalR();
        await loadConversations();
        _setupScrollHandler();

        // Auto-open if URL params
        if (opts.peerId)   await selectConversation(opts.peerId,   null,          opts.peerName   || '');
        if (opts.groupId)  await selectConversation(null, opts.groupId,            opts.groupName  || '');
    }

    // ── Public API ────────────────────────────────────────────────
    return {
        init,
        loadConversations,
        renderConversations,
        filterConvs,
        selectConversation,
        loadMessages,
        sendMessage,
        handleInput,
        handleKeydown,
        insertMention,
        startReply,
        cancelReply,
        showReactionPicker,
        addReaction,
        addReactionActive,
        pinMessage,
        togglePinPanel,
        closePinPanel,
        scrollToMessage,
        loadPins,
        editMessage,
        deleteMessage,
        votePoll,
        openFileAttach,
        handleFileSelect,
        clearFile,
        startRecording,
        cancelRecording,
        stopAndSendRecording,
        onSendClick,
        toggleMoreMenu,
        toggleEmoji,
        insertEmoji,
        openPollModal,
        closePollModal,
        addPollOption,
        submitPoll,
        toggleMsgSearch,
        searchMessages,
        initiateCall,
        showConfirm,
        confirmOk,
        confirmCancel,
        leaveGroup,
        blockUser,
        clearConversation,
        getPartnerName,
        getPartnerAvatar,
        showEditHistory,
        loadEditHistory,
        forwardMessage,
        submitForward,
        closeForwardModal,
        saveMessage,
        toggleTtlMenu,
        setTtl,
        openNewDm,
        closeNewDm,
        _searchDmUsers,
        _startDm,
        _handleSlashPopup,
        _insertSlash,
        get myId() { return _myId; }
    };
})();
