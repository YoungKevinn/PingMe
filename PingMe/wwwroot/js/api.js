// PingMe API helpers — reads JWT injected by server (window.__pmJwt)

function pmGetToken() {
    // Primary: JWT injected by Razor layout from Cookie claims (most reliable)
    if (window.__pmJwt) return window.__pmJwt;
    // Fallback: read pm_token cookie
    const match = document.cookie.match(/(?:^|;\s*)pm_token=([^;]*)/);
    return match ? decodeURIComponent(match[1]) : null;
}

function pmEscape(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

const pmFmt = {
    time(iso) {
        if (!iso) return '';
        const d = new Date(iso);
        if (isNaN(d)) return '';
        const now = new Date();
        const diff = now - d;
        if (diff < 60000) return 'vừa xong';
        if (diff < 3600000) return Math.floor(diff / 60000) + ' phút trước';
        if (diff < 86400000) {
            return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        }
        if (diff < 604800000) {
            const days = ['CN','T2','T3','T4','T5','T6','T7'];
            return days[d.getDay()] + ' ' + d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        }
        return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },
    shortTime(iso) {
        if (!iso) return '';
        const d = new Date(iso);
        if (isNaN(d)) return '';
        const now = new Date();
        if (now - d < 86400000 && d.getDate() === now.getDate())
            return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
    }
};

const pmToast = {
    _show(msg, type) {
        const c = document.getElementById('pm-toast-container');
        if (!c) return;
        const t = document.createElement('div');
        const colors = { success: '#22C55E', error: '#EF4444', info: '#3B82F6', warning: '#F59E0B' };
        t.style.cssText = `
            background:${colors[type]||colors.info};color:#fff;padding:12px 20px;border-radius:10px;
            font-size:.875rem;font-weight:600;box-shadow:0 4px 16px rgba(0,0,0,.2);
            animation:pm-toast-in .25s ease;pointer-events:none;max-width:320px;word-break:break-word;
        `;
        t.textContent = msg;
        c.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity .3s'; setTimeout(() => t.remove(), 300); }, 3200);
    },
    success(msg) { this._show(msg, 'success'); },
    error(msg)   { this._show(msg, 'error'); },
    info(msg)    { this._show(msg, 'info'); },
    warn(msg)    { this._show(msg, 'warning'); }
};

const pmApi = {
    backendUrl: '',   // same origin in MVC

    _headers(extra) {
        const token = pmGetToken();
        const h = { 'Content-Type': 'application/json', ...extra };
        if (token) h['Authorization'] = 'Bearer ' + token;
        return h;
    },

    // silent: true → 401 returns null without redirecting (for background/badge calls)
    async _req(method, url, body, silent = false) {
        try {
            const opts = { method, headers: this._headers(), credentials: 'same-origin' };
            if (body !== undefined) opts.body = JSON.stringify(body);
            const r = await fetch(url, opts);
            if (r.status === 401) {
                if (!silent) window.location.href = '/auth/login';
                return null;
            }
            if (r.status === 204 || r.headers.get('content-length') === '0') return true;
            const text = await r.text();
            if (!text) return true;
            let data;
            try { data = JSON.parse(text); } catch { return text; }
            if (!r.ok) {
                if (!silent) {
                    const msg = data?.message || data?.title || data?.error || 'Lỗi không xác định';
                    pmToast.error(msg);
                }
                return null;
            }
            return data;
        } catch (e) {
            if (!silent) pmToast.error('Mất kết nối mạng');
            return null;
        }
    },

    get(url)           { return this._req('GET',    url); },
    post(url, body)    { return this._req('POST',   url, body); },
    put(url, body)     { return this._req('PUT',    url, body); },
    patch(url, body)   { return this._req('PATCH',  url, body); },
    delete(url)        { return this._req('DELETE', url); },

    // Silent variants — for background calls (no redirect, no toast on 401)
    getSilent(url)        { return this._req('GET',    url, undefined, true); },
    postSilent(url, body) { return this._req('POST',   url, body,      true); },

    async postForm(url, formData) {
        try {
            const token = pmGetToken();
            const h = {};
            if (token) h['Authorization'] = 'Bearer ' + token;
            const r = await fetch(url, { method: 'POST', headers: h, body: formData, credentials: 'same-origin' });
            if (r.status === 401) { window.location.href = '/auth/login'; return null; }
            const text = await r.text();
            if (!text) return true;
            let data;
            try { data = JSON.parse(text); } catch { return text; }
            if (!r.ok) { pmToast.error(data?.message || 'Upload thất bại'); return null; }
            return data;
        } catch { pmToast.error('Lỗi upload'); return null; }
    }
};
