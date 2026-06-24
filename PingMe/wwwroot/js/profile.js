// Profile page — ported from Profile.razor (view card + edit modal + password modal + confirm)
const pmProfile = (() => {
    const $ = id => document.getElementById(id);
    const esc = s => (window.pmEscape ? pmEscape(s) : (s == null ? '' : String(s)));
    let _profile = null;
    let _selectedAvatarFile = null;
    let _avatarVersion = Date.now();

    function abs(url) {
        if (!url) return '';
        if (/^https?:/i.test(url)) return url + (url.includes('?') ? '&' : '?') + 'v=' + _avatarVersion;
        return (pmApi.backendUrl || '') + url + '?v=' + _avatarVersion;
    }
    const disp = (v, fb) => (v && String(v).trim()) ? v : (fb || '—');
    function fmtDob(iso) {
        if (!iso) return '—';
        const d = new Date(iso); if (isNaN(d)) return '—';
        return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    }
    function dobInput(iso) {
        if (!iso) return '';
        const d = new Date(iso); if (isNaN(d)) return '';
        return d.toISOString().slice(0, 10);
    }
    function initial(name) { return (name || '?').trim().charAt(0).toUpperCase() || '?'; }

    async function init() {
        _profile = await pmApi.get('/api/auth/me');
        renderView();
    }

    function avatarInnerLarge() {
        return _profile.avatarUrl
            ? `<img src="${esc(abs(_profile.avatarUrl))}" onerror="this.style.display='none'">`
            : esc(initial(_profile.displayName));
    }

    function renderView() {
        const root = $('pm-profile-root');
        if (!root) return;
        if (!_profile) { root.innerHTML = '<div class="mud-alert mud-alert-filled mud-alert-filled-error"><div class="mud-alert-message">Không tải được hồ sơ. Vui lòng đăng nhập lại.</div></div>'; return; }
        const p = _profile;
        const statusVal = p.isOnline
            ? '<span class="pm-status-online">● Đang online</span>'
            : `<span>${p.lastSeen ? new Date(p.lastSeen).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : 'Không rõ'}</span>`;

        root.innerHTML = `
        <div class="pm-profile-view-card">
            <div class="pm-profile-cover">
                <button type="button" class="pm-profile-edit-float" onclick="pmProfile.openEdit()" title="Chỉnh sửa hồ sơ">
                    <svg viewBox="0 0 24 24"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg>
                    <span>Chỉnh sửa</span>
                </button>
            </div>
            <div class="pm-profile-view-body">
                <div class="pm-profile-view-top">
                    <div class="pm-profile-avatar-wrap">
                        <button type="button" class="pm-profile-avatar-button" onclick="pmProfile.openEdit()" title="Bấm để chỉnh sửa ảnh đại diện">
                            <div class="pm-profile-avatar">${avatarInnerLarge()}</div>
                            <span class="pm-profile-avatar-camera"><svg viewBox="0 0 24 24"><path d="M12 12m-3.2 0a3.2 3.2 0 1 0 6.4 0a3.2 3.2 0 1 0-6.4 0M9 2L7.17 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-3.17L15 2H9zm3 15c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5z"/></svg></span>
                        </button>
                    </div>
                    <div class="pm-profile-title-block">
                        <div class="pm-profile-name">${esc(p.displayName)}</div>
                        <div class="pm-profile-email">${esc(p.email)}</div>
                        <div class="pm-profile-chips">
                            <span>${esc(disp(p.jobTitle, 'Chưa có chức vụ'))}</span>
                            <span>${esc(disp(p.department, 'Chưa có bộ phận'))}</span>
                        </div>
                    </div>
                </div>
                <hr class="pm-divider">
                <div class="pm-profile-info-grid">
                    <div class="pm-profile-info-item"><div class="label">Username</div><div class="value">${esc(disp(p.username))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Email</div><div class="value">${esc(disp(p.email))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Chức vụ</div><div class="value">${esc(disp(p.jobTitle))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Bộ phận / Team</div><div class="value">${esc(disp(p.department))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Vị trí làm việc</div><div class="value">${esc(disp(p.workLocation))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Số điện thoại</div><div class="value">${esc(disp(p.phoneNumber))}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Ngày sinh</div><div class="value">${fmtDob(p.dateOfBirth)}</div></div>
                    <div class="pm-profile-info-item"><div class="label">Trạng thái</div><div class="value">${statusVal}</div></div>
                </div>
                <div class="pm-profile-bio"><div class="label">Giới thiệu</div><p>${esc(disp(p.bio, 'Chưa có giới thiệu.'))}</p></div>
                <hr class="pm-divider">
                <div class="pm-profile-security-section">
                    <div class="pm-profile-security-header"><svg viewBox="0 0 24 24"><path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z"/></svg><span>Bảo mật tài khoản</span></div>
                    <div class="pm-profile-security-row">
                        <div>
                            <div class="pm-profile-security-title">Mật khẩu</div>
                            <div class="pm-profile-security-desc">Thay đổi mật khẩu đăng nhập của bạn</div>
                        </div>
                        <button type="button" class="pm-btn outlined" onclick="pmProfile.openPassword()"><svg viewBox="0 0 24 24"><path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z"/></svg> Đổi mật khẩu</button>
                    </div>
                </div>
            </div>
        </div>`;
    }

    // ── Edit modal ─────────────────────────────────────────────
    function openEdit() {
        const p = _profile;
        _selectedAvatarFile = null;
        $('pm-edit-displayname').value = p.displayName || '';
        $('pm-edit-email').value = p.email || '';
        $('pm-edit-jobtitle').value = p.jobTitle || '';
        $('pm-edit-department').value = p.department || '';
        $('pm-edit-worklocation').value = p.workLocation || '';
        $('pm-edit-phone').value = p.phoneNumber || '';
        $('pm-edit-dob').value = dobInput(p.dateOfBirth);
        $('pm-edit-username').value = p.username || '';
        $('pm-edit-bio').value = p.bio || '';
        $('pm-edit-avatar-inner').innerHTML = avatarInnerLarge();
        $('pm-edit-avatar-selected').textContent = '';
        $('pm-edit-modal').style.display = 'flex';
    }
    function closeEdit() { $('pm-edit-modal').style.display = 'none'; }

    function onAvatarSelected(input) {
        const f = input.files && input.files[0];
        if (!f) return;
        _selectedAvatarFile = f;
        $('pm-edit-avatar-selected').textContent = 'Đã chọn: ' + f.name;
        const reader = new FileReader();
        reader.onload = e => { $('pm-edit-avatar-inner').innerHTML = `<img src="${e.target.result}">`; };
        reader.readAsDataURL(f);
    }

    function openConfirmSave() {
        if (!($('pm-edit-displayname').value || '').trim()) { pmToast.error('Tên hiển thị không được trống'); return; }
        $('pm-confirm-modal').style.display = 'flex';
    }
    function closeConfirmSave() { $('pm-confirm-modal').style.display = 'none'; }

    async function save() {
        const displayName = ($('pm-edit-displayname').value || '').trim();
        if (!displayName) { pmToast.error('Tên hiển thị không được trống'); return; }

        // Upload avatar first if a new one was chosen
        if (_selectedAvatarFile) {
            const fd = new FormData();
            fd.append('file', _selectedAvatarFile);
            const ar = await pmApi.postForm('/api/users/me/avatar', fd);
            if (ar && ar.avatarUrl) _profile.avatarUrl = ar.avatarUrl;
        }

        const dobVal = $('pm-edit-dob').value;
        const body = {
            displayName,
            bio: ($('pm-edit-bio').value || '').trim(),
            jobTitle: ($('pm-edit-jobtitle').value || '').trim(),
            department: ($('pm-edit-department').value || '').trim(),
            workLocation: ($('pm-edit-worklocation').value || '').trim(),
            phoneNumber: ($('pm-edit-phone').value || '').trim(),
            dateOfBirth: dobVal ? new Date(dobVal).toISOString() : null
        };
        const r = await pmApi.put('/api/users/me', body);
        if (r !== null) {
            _avatarVersion = Date.now();
            _profile = await pmApi.get('/api/auth/me');
            closeConfirmSave();
            closeEdit();
            renderView();
            pmToast.success('Đã cập nhật hồ sơ');
        } else {
            pmToast.error('Cập nhật hồ sơ thất bại');
        }
    }

    // ── Password modal ─────────────────────────────────────────
    function openPassword() {
        $('pm-pw-cur').value = ''; $('pm-pw-new').value = ''; $('pm-pw-confirm').value = '';
        $('pm-password-modal').style.display = 'flex';
    }
    function closePassword() { $('pm-password-modal').style.display = 'none'; }

    async function changePassword() {
        const cur = $('pm-pw-cur').value;
        const nw = $('pm-pw-new').value;
        const cf = $('pm-pw-confirm').value;
        if (!cur || !nw) { pmToast.error('Vui lòng điền đầy đủ'); return; }
        if (nw.length < 6) { pmToast.error('Mật khẩu mới tối thiểu 6 ký tự'); return; }
        if (nw !== cf) { pmToast.error('Mật khẩu xác nhận không khớp'); return; }
        const r = await pmApi.post('/api/auth/change-password', { currentPassword: cur, newPassword: nw });
        if (r !== null) { closePassword(); pmToast.success('Đổi mật khẩu thành công'); }
    }

    document.addEventListener('DOMContentLoaded', init);

    return { init, openEdit, closeEdit, onAvatarSelected, openConfirmSave, closeConfirmSave, save, openPassword, closePassword, changePassword };
})();
