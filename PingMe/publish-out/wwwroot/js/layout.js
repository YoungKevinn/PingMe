// PingMe layout: sidebar, theme, badges, resize

const pmLayout = {
    _dark: false,
    _initialized: false,
    _badgeTimer: null,
    _resizing: false,
    _resizeStartX: 0,
    _resizeStartW: 0,

    init() {
        // Guard: only run once even if called multiple times
        if (this._initialized) return;
        this._initialized = true;

        // Restore dark mode preference
        this._dark = localStorage.getItem('pm_dark') === '1';
        if (this._dark) {
            document.documentElement.classList.add('dark-mode');
            document.body.setAttribute('data-mud-theme', 'dark');
        } else {
            document.body.setAttribute('data-mud-theme', 'default');
        }

        // Mark active nav link
        this._markActive();

        // Load unread badges
        this.loadBadges();

        // Global mouse events for sidebar resize
        document.addEventListener('mousemove', e => this._onResizeMove(e));
        document.addEventListener('mouseup', () => this._stopResize());
    },

    _markActive() {
        const path = window.location.pathname.replace(/\/$/, '') || '/';
        document.querySelectorAll('.pm-nav-item').forEach(a => {
            const href = a.getAttribute('href')?.replace(/\/$/, '') || '';
            const isActive = href === path || (href !== '/' && path.startsWith(href));
            a.classList.toggle('active', isActive);
        });
    },

    toggleDrawer() {
        const sidebar = document.getElementById('pm-left-sidebar');
        if (!sidebar) return;
        const isVisible = sidebar.style.transform !== 'translateX(-264px)';
        const content = document.getElementById('pm-main-content');
        if (isVisible) {
            sidebar.style.transform = 'translateX(-264px)';
            sidebar.classList.remove('expanded');
            if (content) content.style.left = '0';
            localStorage.setItem('pm_sidebar', '0');
        } else {
            sidebar.style.transform = 'translateX(0)';
            sidebar.classList.add('expanded');
            if (content) content.style.left = '264px';
            localStorage.setItem('pm_sidebar', '1');
        }
    },

    toggleTheme() {
        this._dark = !this._dark;
        document.documentElement.classList.toggle('dark-mode', this._dark);
        document.body.setAttribute('data-mud-theme', this._dark ? 'dark' : 'default');
        localStorage.setItem('pm_dark', this._dark ? '1' : '0');
    },

    loadBadges() {
        // Debounce: skip if called again within 2 seconds (prevents spam on rapid tab switches)
        if (this._badgeTimer) return;
        this._badgeTimer = setTimeout(() => { this._badgeTimer = null; }, 2000);

        // Use silent calls — badge loading must NOT redirect to login on failure
        // Run all requests concurrently
        pmApi.getSilent('/api/conversations').then(convs => {
            if (convs?.length) {
                const total = convs.reduce((s, c) => s + (c.unreadCount || 0), 0);
                if (total > 0) {
                    const badge = document.getElementById('pm-nav-badge-msg');
                    const slot  = document.getElementById('pm-nav-badge-msg-slot');
                    if (badge) { badge.textContent = total > 99 ? '99+' : total; }
                    if (slot)  { slot.style.display = 'inline-flex'; }
                }
            }
        });

        pmApi.getSilent('/api/friends/requests').then(friendData => {
            if (friendData?.length) {
                const badge = document.getElementById('pm-nav-badge-friends');
                const slot  = document.getElementById('pm-nav-badge-friends-slot');
                if (badge) { badge.textContent = friendData.length > 99 ? '99+' : friendData.length; }
                if (slot)  { slot.style.display = 'inline-flex'; }
            }
        });

        pmApi.getSilent('/api/iocs/stats').then(iocStats => {
            if (iocStats) {
                const active = iocStats.activeCount !== undefined ? iocStats.activeCount : (iocStats.ActiveCount || 0);
                const badge = document.getElementById('pm-nav-badge-ioc');
                const slot  = document.getElementById('pm-nav-badge-ioc-slot');
                const label = document.querySelector('a[href^="/ioc"] .pm-nav-label');
                
                if (active > 0) {
                    if (badge) badge.textContent = active > 99 ? '99+' : active;
                    if (slot) {
                        slot.style.display = 'inline-flex';
                        slot.classList.add('has-badge');
                    }
                    if (label) label.classList.add('pm-nav-label-unread');
                } else {
                    if (slot) {
                        slot.style.display = 'none';
                        slot.classList.remove('has-badge');
                    }
                    if (label) label.classList.remove('pm-nav-label-unread');
                }
            }
        });
    },

    startSidebarResize(e) {
        const sidebar = document.getElementById('pm-left-sidebar');
        if (!sidebar) return;
        this._resizing = true;
        this._resizeStartX = e.clientX;
        this._resizeStartW = sidebar.getBoundingClientRect().width;
        document.body.style.userSelect = 'none';
        e.preventDefault();
    },

    _onResizeMove(e) {
        if (!this._resizing) return;
        const sidebar = document.getElementById('pm-left-sidebar');
        const content = document.getElementById('pm-main-content');
        if (!sidebar) return;
        const delta = e.clientX - this._resizeStartX;
        const newW = Math.max(180, Math.min(400, this._resizeStartW + delta));
        sidebar.style.width = newW + 'px';
        if (content) content.style.left = newW + 'px';
    },

    _stopResize() {
        if (!this._resizing) return;
        this._resizing = false;
        document.body.style.userSelect = '';
    },

    // Language toggle (vi / en)
    _lang: 'vi',
    initLang() {
        this._lang = localStorage.getItem('pm_lang') || 'vi';
        this._applyLang(this._lang);
    },
    toggleLanguage() {
        this._lang = this._lang === 'vi' ? 'en' : 'vi';
        localStorage.setItem('pm_lang', this._lang);
        this._applyLang(this._lang);
    },
    _applyLang(lang) {
        document.documentElement.lang = lang;
        // Update language button tooltip
        const btn = document.querySelector('[onclick="pmLayout.toggleLanguage()"]');
        if (btn) btn.title = lang === 'vi' ? 'Switch to English' : 'Chuyển sang Tiếng Việt';
        // Notify global localization if available
        if (typeof pmLoc !== 'undefined' && pmLoc.setLang) pmLoc.setLang(lang);
    }
};

// DOMContentLoaded listener is in _Layout.cshtml — init guard prevents double-run
document.addEventListener('DOMContentLoaded', () => { pmLayout.init(); pmLayout.initLang(); });
