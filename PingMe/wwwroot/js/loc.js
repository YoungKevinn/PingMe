// PingMe localization (vi/en) — port of Blazor LocalizationService.
// Translates elements tagged with:
//   data-i18n="key"            → element.textContent
//   data-i18n-title="key"      → element.title
//   data-i18n-placeholder="key"→ element.placeholder
// Storage key matches the original Blazor app ("pingme.lang") AND the
// MVC layout key ("pm_lang") so the globe toggle persists consistently.

const pmLoc = (() => {
    const TEXTS = {
        vi: {
            'nav.messages': 'Tin nhắn', 'nav.groups': 'Nhóm', 'nav.search': 'Tìm kiếm',
            'nav.saved': 'Đã lưu', 'nav.friends': 'Bạn bè', 'nav.code': 'Code',
            'nav.ioc': 'IOC', 'nav.pentest': 'Pentest', 'nav.timeline': 'Dòng thời gian',
            'nav.tasks': 'Công việc', 'nav.secrets': 'Secret', 'nav.blocked': 'Đã chặn',
            'nav.sessions': 'Phiên đăng nhập', 'nav.webhooks': 'Webhook', 'nav.settings': 'Cài đặt',

            'account.profile': 'Hồ sơ', 'account.logout': 'Đăng xuất', 'account.language': 'Ngôn ngữ',
            'account.theme': 'Chuyển theme',
            'account.switchToEnglish': 'Switch to English', 'account.switchToVietnamese': 'Chuyển sang Tiếng Việt',

            'saved.title': 'Tin nhắn đã lưu',
            'saved.subtitle': 'Bookmark các tin quan trọng để mở lại nhanh.',
            'saved.all': 'Tất cả', 'saved.dm': 'DM', 'saved.group': 'Nhóm', 'saved.empty': 'Chưa có tin nhắn đã lưu',

            'chat.emptyTitle': 'Chọn một cuộc trò chuyện',
            'chat.emptySubtitle': 'Chọn từ danh sách bên trái hoặc bắt đầu cuộc trò chuyện mới',
            'chat.messagesTitle': 'Tin nhắn', 'chat.searchPlaceholder': 'Tìm kiếm...',
            'chat.inputPlaceholder': 'Nhập tin nhắn...', 'chat.groupLabel': 'Nhóm',

            'search.title': 'Tìm kiếm',
            'search.subtitle': 'Tìm trong tin nhắn, người dùng, nhóm, snippet, IOC, finding, task và file.',
            'search.placeholder': 'Nhập từ khóa cần tìm...', 'search.noResults': 'Không tìm thấy kết quả',

            'pentest.title': 'Pentest', 'pentest.trackerTitle': 'Pentest Finding Tracker',
            'pentest.trackerSubtitle': 'Quản lý lỗ hổng theo group, endpoint, payload và trạng thái xử lý.',
            'pentest.totalFindings': 'Tổng finding', 'pentest.criticalHigh': 'Critical/High',
            'pentest.openFindings': 'Đang mở', 'pentest.closedMitigated': 'Đã xử lý/đóng',
            'pentest.newFinding': 'Finding mới', 'pentest.empty': 'Không có finding nào.',

            'secrets.title': 'Secret dùng một lần',
            'secrets.subtitle': 'Chia sẻ password, API key hoặc ghi chú nhạy cảm bằng link chỉ mở được một lần.',
            'secrets.refresh': 'Làm mới', 'secrets.createTitle': 'Tạo secret', 'secrets.empty': 'Chưa có secret nào.',

            'friends.title': 'Bạn bè', 'friends.searchPlaceholder': 'Nhập tên, username, email...',
            'friends.tabFriends': 'Bạn bè', 'friends.tabReceived': 'Lời mời nhận', 'friends.tabSent': 'Lời mời gửi',
            'friends.noFriends': 'Chưa có bạn bè nào.', 'friends.message': 'Nhắn tin', 'friends.remove': 'Xóa bạn',
            'friends.searchNew': 'Tìm bạn mới',

            'snippets.title': 'Code Snippets',
            'snippets.subtitle': 'Lưu, tìm kiếm và chia sẻ các đoạn code dùng trong nội bộ.',
            'snippets.createBtn': 'Tạo snippet mới', 'snippets.allLanguages': 'Tất cả ngôn ngữ',
            'snippets.empty': 'Bạn chưa có snippet nào. Hãy tạo snippet đầu tiên để lưu các đoạn code thường dùng.',

            'ioc.title': 'IOC Center',
            'ioc.subtitle': 'Lưu trữ, tìm kiếm và chia sẻ các Indicators of Compromise.',
            'ioc.addIoc': 'Thêm IOC', 'ioc.empty': 'Bạn chưa lưu IOC nào.',

            'timeline.title': 'Threat Timeline',
            'timeline.subtitle': 'Theo dõi và phân tích chuỗi sự kiện bảo mật theo thời gian thực.',
            'timeline.addEvent': 'Thêm sự kiện', 'timeline.empty': 'Chưa có sự kiện nào. Hãy thêm sự kiện đầu tiên.',
            'timeline.selectGroup': 'Chọn nhóm',

            'task.title': 'Công việc', 'task.subtitle': 'Quản lý công việc và theo dõi tiến độ.',
            'task.addTask': 'Thêm Task', 'task.empty': 'Chưa có task nào.',
            'task.todo': 'Cần làm', 'task.inProgress': 'Đang làm', 'task.done': 'Hoàn thành',

            'blocked.title': 'Người dùng đã chặn',
            'blocked.subtitle': 'Quản lý danh sách những người dùng bạn đã chặn.',
            'blocked.empty': 'Bạn chưa chặn ai.', 'blocked.unblock': 'Bỏ chặn',
            'blocked.name': 'Tên người dùng', 'blocked.blockedAt': 'Thời gian chặn',

            'sessions.title': 'Phiên hoạt động',
            'sessions.subtitle': 'Quản lý các thiết bị đang đăng nhập vào tài khoản của bạn.',
            'sessions.current': 'Phiên hiện tại', 'sessions.revokeAll': 'Đăng xuất tất cả phiên khác',

            'webhooks.title': 'Webhook', 'groups.title': 'Nhóm', 'profile.title': 'Hồ sơ'
        },
        en: {
            'nav.messages': 'Messages', 'nav.groups': 'Groups', 'nav.search': 'Search',
            'nav.saved': 'Saved', 'nav.friends': 'Friends', 'nav.code': 'Code',
            'nav.ioc': 'IOC', 'nav.pentest': 'Pentest', 'nav.timeline': 'Timeline',
            'nav.tasks': 'Tasks', 'nav.secrets': 'Secrets', 'nav.blocked': 'Blocked',
            'nav.sessions': 'Sessions', 'nav.webhooks': 'Webhooks', 'nav.settings': 'Settings',

            'account.profile': 'Profile', 'account.logout': 'Log out', 'account.language': 'Language',
            'account.theme': 'Toggle theme',
            'account.switchToEnglish': 'Switch to English', 'account.switchToVietnamese': 'Chuyển sang Tiếng Việt',

            'saved.title': 'Saved messages',
            'saved.subtitle': 'Bookmark important messages for quick access.',
            'saved.all': 'All', 'saved.dm': 'DM', 'saved.group': 'Group', 'saved.empty': 'No saved messages yet',

            'chat.emptyTitle': 'Select a conversation',
            'chat.emptySubtitle': 'Choose from the list on the left or start a new conversation',
            'chat.messagesTitle': 'Messages', 'chat.searchPlaceholder': 'Search...',
            'chat.inputPlaceholder': 'Type a message...', 'chat.groupLabel': 'Group',

            'search.title': 'Search',
            'search.subtitle': 'Search messages, users, groups, snippets, IOC, findings, tasks, and files.',
            'search.placeholder': 'Enter a keyword...', 'search.noResults': 'No results found',

            'pentest.title': 'Pentest', 'pentest.trackerTitle': 'Pentest Finding Tracker',
            'pentest.trackerSubtitle': 'Manage vulnerabilities by group, endpoint, payload, and remediation status.',
            'pentest.totalFindings': 'Total Findings', 'pentest.criticalHigh': 'Critical/High',
            'pentest.openFindings': 'Open', 'pentest.closedMitigated': 'Mitigated/Closed',
            'pentest.newFinding': 'New Finding', 'pentest.empty': 'No findings found.',

            'secrets.title': 'One-time Secret',
            'secrets.subtitle': 'Share passwords, API keys, or sensitive notes with a link that can be opened only once.',
            'secrets.refresh': 'Refresh', 'secrets.createTitle': 'Create secret', 'secrets.empty': 'No secrets yet.',

            'friends.title': 'Friends', 'friends.searchPlaceholder': 'Enter name, username, email...',
            'friends.tabFriends': 'Friends', 'friends.tabReceived': 'Received requests', 'friends.tabSent': 'Sent requests',
            'friends.noFriends': 'No friends yet.', 'friends.message': 'Message', 'friends.remove': 'Remove friend',
            'friends.searchNew': 'Find new friends',

            'snippets.title': 'Code Snippets',
            'snippets.subtitle': 'Save, search, and share internal code snippets.',
            'snippets.createBtn': 'Create new snippet', 'snippets.allLanguages': 'All languages',
            'snippets.empty': "You don't have any snippets. Create your first snippet to save frequently used code.",

            'ioc.title': 'IOC Center',
            'ioc.subtitle': 'Store, search, and share Indicators of Compromise.',
            'ioc.addIoc': 'Add IOC', 'ioc.empty': 'You have no saved IOCs.',

            'timeline.title': 'Threat Timeline',
            'timeline.subtitle': 'Track and analyze security events in real time.',
            'timeline.addEvent': 'Add event', 'timeline.empty': 'No events yet. Add the first event.',
            'timeline.selectGroup': 'Select a group',

            'task.title': 'Task Center', 'task.subtitle': 'Manage tasks and track progress.',
            'task.addTask': 'Add Task', 'task.empty': 'No tasks yet.',
            'task.todo': 'To Do', 'task.inProgress': 'In Progress', 'task.done': 'Done',

            'blocked.title': 'Blocked Users',
            'blocked.subtitle': 'Manage the list of users you have blocked.',
            'blocked.empty': "You haven't blocked anyone.", 'blocked.unblock': 'Unblock',
            'blocked.name': 'Username', 'blocked.blockedAt': 'Blocked at',

            'sessions.title': 'Active Sessions',
            'sessions.subtitle': 'Manage devices currently logged into your account.',
            'sessions.current': 'Current session', 'sessions.revokeAll': 'Log out all other sessions',

            'webhooks.title': 'Webhooks', 'groups.title': 'Groups', 'profile.title': 'Profile'
        }
    };

    let _lang = 'vi';

    function t(key) {
        if (!key) return '';
        const dict = TEXTS[_lang] || TEXTS.vi;
        if (key in dict) return dict[key];
        if (key in TEXTS.vi) return TEXTS.vi[key];
        return key;
    }

    function apply(root) {
        const scope = root || document;
        scope.querySelectorAll('[data-i18n]').forEach(el => {
            const k = el.getAttribute('data-i18n');
            const v = t(k);
            if (v) el.textContent = v;
        });
        scope.querySelectorAll('[data-i18n-title]').forEach(el => {
            el.title = t(el.getAttribute('data-i18n-title'));
        });
        scope.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
            el.placeholder = t(el.getAttribute('data-i18n-placeholder'));
        });
    }

    function setLang(lang) {
        _lang = (String(lang).toLowerCase() === 'en') ? 'en' : 'vi';
        localStorage.setItem('pm_lang', _lang);
        localStorage.setItem('pingme.lang', _lang);
        document.documentElement.lang = _lang;
        apply();
    }

    function init() {
        _lang = (localStorage.getItem('pm_lang') || localStorage.getItem('pingme.lang') || 'vi').toLowerCase();
        if (_lang !== 'en') _lang = 'vi';
        document.documentElement.lang = _lang;
        apply();
    }

    return { init, setLang, apply, t, get lang() { return _lang; } };
})();

document.addEventListener('DOMContentLoaded', () => pmLoc.init());
