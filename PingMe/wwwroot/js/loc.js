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
            'common.refresh': 'Làm mới', 'common.apply': 'Áp dụng', 'common.search': 'Tìm kiếm',
            'common.allGroups': 'Tất cả nhóm', 'common.allAssignees': 'Tất cả người được giao',
            'common.allStatuses': 'Tất cả trạng thái', 'common.allPriorities': 'Tất cả mức độ',
            'common.assignedToMe': 'Giao cho tôi', 'common.overdue': 'Quá hạn',
            'common.expires': 'Hết hạn', 'time.5m': '5 phút', 'time.1h': '1 giờ', 'time.1d': '1 ngày',
            'secrets.placeholder': 'Secret', 'secrets.warning': 'Link chỉ hiển thị một lần! Hãy copy ngay.',
            'secrets.shareLink': 'Link chia sẻ', 'common.copyLink': 'Copy link',
            'ioc.searchLabel': 'Tìm kiếm IOC', 'ioc.searchPlaceholder': 'Nhập địa chỉ IP, Domain, Hash...',
            'common.type': 'Loại IOC', 'common.allTypes': 'Tất cả loại',
            'common.severity': 'Mức độ', 'common.allSeverities': 'Tất cả mức độ',
            'common.status': 'Status', 'common.all': 'Tất cả',
            'common.filter': 'Lọc', 'common.reset': 'Reset',
            'ioc.createTitle': 'Thêm IOC mới', 'ioc.createSubtitle': 'Quản lý type, severity, status và mô tả IOC.',
            'ioc.emptyStateTitle': 'Không tìm thấy IOC nào', 'ioc.emptyStateSubtitle': 'Bạn có thể tạo thủ công tại đây hoặc gửi trong chat bằng cú pháp <b>/ioc</b>.',
            'pentest.listTitle': 'Danh sách Findings', 'common.searchPlaceholder': 'Nhập từ khóa...',
            'common.group': 'Nhóm', 'common.personal': '-- Cá nhân (Personal) --',
            'ioc.value': 'Giá trị (Value) *', 'common.notes': 'Ghi chú', 'ioc.notesPlaceholder': 'Ghi chú phân tích, nguồn log, tình trạng xử lý...',
            'common.cancel': 'Hủy', 'common.save': 'Lưu', 'common.view': 'Xem', 'common.edit': 'Sửa', 'common.delete': 'Xóa', 'common.loadMore': 'Tải thêm',
            'common.groupId': 'Group ID', 'pentest.titleLabel': 'Tiêu đề (Tóm tắt lỗ hổng)', 'pentest.descLabel': 'Mô tả chi tiết',
            'pentest.save': 'Lưu Finding', 'task.search': 'Tìm kiếm task...',
            'common.createdAt': 'Tạo lúc', 'common.expiresAt': 'Hết hạn', 'common.viewedAt': 'Đã xem lúc', 'common.viewer': 'Người xem',

            'account.profile': 'Hồ sơ', 'account.logout': 'Đăng xuất', 'account.language': 'Ngôn ngữ',
            'account.theme': 'Chuyển theme',
            'account.switchToEnglish': 'Switch to English', 'account.switchToVietnamese': 'Chuyển sang Tiếng Việt',

            'saved.title': 'Tin nhắn đã lưu',
            'saved.subtitle': 'Bookmark các tin quan trọng để mở lại nhanh.',
            'saved.all': 'Tất cả', 'saved.dm': 'DM', 'saved.group': 'Nhóm', 'saved.empty': 'Chưa có tin nhắn đã lưu',

            'chat.emptyTitle': 'Chọn một cuộc trò chuyện',
            'chat.emptySubtitle': 'Chọn từ danh sách bên trái hoặc bắt đầu cuộc trò chuyện mới',
            'chat.messagesTitle': 'Tin nhắn', 'chat.searchPlaceholder': 'Tìm kiếm...',
            'chat.inputPlaceholder': 'Nhập tin nhắn... Gõ / để xem lệnh, @ để tag thành viên', 'chat.groupLabel': 'Nhóm',

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
            'common.refresh': 'Refresh', 'common.apply': 'Apply', 'common.search': 'Search',
            'common.allGroups': 'All groups', 'common.allAssignees': 'All assignees',
            'common.allStatuses': 'All statuses', 'common.allPriorities': 'All priorities',
            'common.assignedToMe': 'Assigned to me', 'common.overdue': 'Overdue',
            'common.expires': 'Expires', 'time.5m': '5 minutes', 'time.1h': '1 hour', 'time.1d': '1 day',
            'secrets.placeholder': 'Secret', 'secrets.warning': 'Link is shown only once! Copy it now.',
            'secrets.shareLink': 'Share link', 'common.copyLink': 'Copy link',
            'ioc.searchLabel': 'Search IOC', 'ioc.searchPlaceholder': 'Enter IP, Domain, Hash...',
            'common.type': 'IOC Type', 'common.allTypes': 'All types',
            'common.severity': 'Severity', 'common.allSeverities': 'All severities',
            'common.status': 'Status', 'common.all': 'All',
            'common.filter': 'Filter', 'common.reset': 'Reset',
            'ioc.createTitle': 'Add new IOC', 'ioc.createSubtitle': 'Manage type, severity, status and description.',
            'ioc.emptyStateTitle': 'No IOCs found', 'ioc.emptyStateSubtitle': 'You can create one manually here or send in chat using the <b>/ioc</b> command.',
            'pentest.listTitle': 'Findings List', 'common.searchPlaceholder': 'Enter keyword...',
            'common.group': 'Group', 'common.personal': '-- Personal --',
            'ioc.value': 'Value *', 'common.notes': 'Notes', 'ioc.notesPlaceholder': 'Analysis notes, log source, status...',
            'common.cancel': 'Cancel', 'common.save': 'Save', 'common.view': 'View', 'common.edit': 'Edit', 'common.delete': 'Delete', 'common.loadMore': 'Load more',
            'common.groupId': 'Group ID', 'pentest.titleLabel': 'Title (Vulnerability summary)', 'pentest.descLabel': 'Detailed description',
            'pentest.save': 'Save Finding', 'task.search': 'Search tasks...',
            'common.createdAt': 'Created at', 'common.expiresAt': 'Expires at', 'common.viewedAt': 'Viewed at', 'common.viewer': 'Viewer',

            'account.profile': 'Profile', 'account.logout': 'Log out', 'account.language': 'Language',
            'account.theme': 'Toggle theme',
            'account.switchToEnglish': 'Switch to English', 'account.switchToVietnamese': 'Chuyển sang Tiếng Việt',

            'saved.title': 'Saved messages',
            'saved.subtitle': 'Bookmark important messages for quick access.',
            'saved.all': 'All', 'saved.dm': 'DM', 'saved.group': 'Group', 'saved.empty': 'No saved messages yet',

            'chat.emptyTitle': 'Select a conversation',
            'chat.emptySubtitle': 'Choose from the list on the left or start a new conversation',
            'chat.messagesTitle': 'Messages', 'chat.searchPlaceholder': 'Search...',
            'chat.inputPlaceholder': 'Type a message... Use / for commands, @ to mention members', 'chat.groupLabel': 'Group',

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

    let _textMapCache = null;
    function getTextMap() {
        if (_textMapCache) return _textMapCache;
        const viToEn = {};
        const enToVi = {};
        for (const k in TEXTS.vi) {
            const vi = TEXTS.vi[k].trim();
            const en = (TEXTS.en[k] || '').trim();
            if (vi && en && vi !== en) {
                viToEn[vi] = en;
                viToEn[vi.toUpperCase()] = en.toUpperCase();
                enToVi[en] = vi;
                enToVi[en.toUpperCase()] = vi.toUpperCase();
            }
        }
        const extras = {
            '🔍 Tìm bạn mới': '🔍 Find new friends', 'Nhắn tin': 'Message', 'Hủy kết bạn': 'Unfriend',
            'Chấp nhận': 'Accept', 'Từ chối': 'Decline', 'Hủy lời mời': 'Cancel request',
            'Thêm bạn': 'Add friend', 'Tạo công việc mới': 'Create new task', 'Tiêu đề *': 'Title *',
            'Nhóm *': 'Group *', 'Ưu tiên': 'Priority', 'Trạng thái': 'Status',
            'Hạn chót (tùy chọn)': 'Deadline (optional)', 'Mô tả': 'Description',
            'BẠN BÈ': 'FRIENDS', 'LỜI MỜI NHẬN': 'RECEIVED REQUESTS', 'LỜI MỜI GỬI': 'SENT REQUESTS',
            'Chưa có bạn bè nào.': 'No friends yet.', 'Vui lòng nhập tiêu đề': 'Please enter title',
            'Vui lòng chọn nhóm': 'Please select group', 'Không có nhóm nào': 'No groups',
            'Sắp xếp': 'Sort', 'Mới nhất': 'Newest', 'Cũ nhất': 'Oldest',
            'Thêm vào': 'Add to', 'Quản trị viên': 'Admin', 'Thành viên': 'Member',
            'Đóng': 'Close', 'Tạo nhóm': 'Create group', 'Thêm thành viên': 'Add member',
            'Không có lời mời nào.': 'No requests.', 'Chưa gửi lời mời nào.': 'No sent requests.',
            'Người gửi': 'Sender', 'Người nhận': 'Receiver', 'Thời gian': 'Time',
            '💬 Nhắn tin': '💬 Message', '❌ Xóa bạn': '❌ Unfriend',
            'Tên nhóm': 'Group name', 'Rời nhóm': 'Leave group', 'Xóa nhóm': 'Delete group',
            'Bạn chưa tham gia nhóm nào.': 'You haven\'t joined any groups.',
            'Tạo Snippet': 'Create snippet', 'Ngôn ngữ': 'Language', 'Lưu thay đổi': 'Save changes',
            'Chưa có snippet nào.': 'No snippets yet.', 'Thêm sự kiện': 'Add event',
            'Chưa có sự kiện nào.': 'No events yet.', 'Nhóm sự kiện': 'Event group',
            'Bạn chưa chặn ai.': 'You haven\'t blocked anyone.', 'Bỏ chặn': 'Unblock',
            'Người dùng': 'User', 'Phiên hiện tại': 'Current session',
            'Đăng xuất tất cả phiên khác': 'Revoke all other sessions', 'Thu hồi': 'Revoke',
            'Trình duyệt': 'Browser', 'Đã tạo': 'Created', 'Tạo webhook': 'Create webhook',
            'URL Payload': 'Payload URL', 'Bí mật': 'Secret', 'Tạo': 'Create',
            'Bạn chưa có webhook nào.': 'No webhooks yet.', 'Tất cả': 'All',
            'Danh sách Findings': 'Findings List', 'Tải thêm': 'Load more',
            'Group ID': 'Group ID', 'Mức độ': 'Severity', 'Trạng thái': 'Status',
            'Tìm kiếm': 'Search', 'Lọc': 'Filter', 'Tìm kiếm IOC': 'Search IOC',
            'Loại IOC': 'IOC Type', 'Reset': 'Reset', 'Hủy': 'Cancel', 'Lưu': 'Save',
            'Sửa': 'Edit', 'Xóa': 'Delete', 'Xem': 'View', 'Áp dụng': 'Apply',
            'Làm mới': 'Refresh', 'Giao cho tôi': 'Assigned to me', 'Quá hạn': 'Overdue',
            'Tất cả nhóm': 'All groups', 'Tất cả người được giao': 'All assignees',
            'Tất cả trạng thái': 'All statuses', 'Tất cả mức độ': 'All priorities',
            'Hoàn thành': 'Complete', 'Mở lại': 'Reopen', 'Nhóm:': 'Group:',
            'Tạo bởi:': 'Created by:', 'Phụ trách:': 'Assigned to:', 'Hạn chót:': 'Deadline:',
            'Chưa phân công': 'Unassigned', '-- Chọn nhóm --': '-- Select group --',
            'Tiêu đề': 'Title', 'Tiêu đề (Tóm tắt lỗ hổng)': 'Title (Summary)',
            'Mô tả chi tiết': 'Detailed description', 'Lưu Finding': 'Save Finding',
            'Thêm IOC mới': 'Add new IOC', 'Quản lý type, severity, status và mô tả IOC.': 'Manage type, severity, status, and description.',
            '-- Cá nhân (Personal) --': '-- Personal --', 'Giá trị (Value) *': 'Value *',
            'Ghi chú': 'Notes', 'Thẻ (Tags)': 'Tags',

            // ── Nhóm ──
            'Nhóm của tôi': 'My groups', '👥 Nhóm của tôi': '👥 My groups',
            'Tạo nhóm mới': 'Create new group', 'Tên nhóm *': 'Group name *',
            'Tìm theo tên hoặc username': 'Search by name or username', 'Tìm': 'Search',
            'Bạn chưa có nhóm nào. Hãy tạo nhóm mới!': 'You have no groups yet. Create a new one!',
            'Không thể tải danh sách nhóm.': 'Failed to load groups.',
            'Vui lòng nhập tên nhóm': 'Please enter a group name',
            'Đã tạo nhóm thành công': 'Group created successfully', 'thành viên': 'members',
            'Chọn nhóm...': 'Select group...', 'Không có nhóm nào': 'No groups',

            // ── Tìm kiếm / Timeline ──
            'Đã xảy ra lỗi khi tìm kiếm.': 'An error occurred while searching.',
            'Không có dữ liệu': 'No data', 'Mở trong chat': 'Open in chat',
            'Chưa chọn nhóm': 'No group selected', 'Chọn một nhóm': 'Select a group',
            'Chọn nhóm ở thanh bên trái để xem timeline': 'Select a group on the left to view its timeline',
            'Chưa có hoạt động nào trong timeline này': 'No activity in this timeline yet',
            'Theo dõi và phân tích chuỗi sự kiện bảo mật theo thời gian thực.': 'Track and analyze the chain of security events in real time.',
            'đã báo cáo một lỗ hổng': 'reported a vulnerability', 'đã chia sẻ một IOC': 'shared an IOC',
            'đã cập nhật timeline': 'updated the timeline', 'đã giao một công việc mới': 'assigned a new task',
            'đã đính kèm một tệp': 'attached a file', 'Từ ngày': 'From date', 'Đến ngày': 'To date',
            'Tệp đính kèm': 'Attachment', 'Tất cả ngôn ngữ': 'All languages',
            'Tất cả cuộc trò chuyện riêng': 'All direct chats', 'Người chat riêng': 'Direct chat user',
            'Không tìm thấy kết quả': 'No results found', 'Nhập từ khóa cần tìm...': 'Enter a keyword...',

            // ── Đã lưu ──
            'Tin nhắn đã lưu': 'Saved messages', 'Tin nhắn riêng': 'DM',
            'Các tin nhắn bạn đã đánh dấu để xem lại': 'Messages you bookmarked to review later',
            'Không có tin nhắn đã lưu': 'No saved messages', 'Bỏ lưu': 'Unsave',
            'Đã bỏ lưu tin nhắn': 'Message unsaved', 'Không thể bỏ lưu tin nhắn': 'Failed to unsave message',
            'Không thể tải dữ liệu.': 'Failed to load data.', 'Xem tin nhắn': 'View message',

            // ── Code / Snippet ──
            'Tạo Snippet mới': 'Create new snippet', 'Chỉnh sửa Snippet': 'Edit snippet',
            'Chia sẻ đoạn code với nhóm': 'Share code with your group', 'Cập nhật nội dung snippet': 'Update snippet content',
            'Dán đoạn code của bạn': 'Paste your code', 'Không bắt buộc': 'Optional',
            'Thời gian hết hạn': 'Expiration', 'Không hết hạn': 'No expiry', '♾ Không hết hạn': '♾ No expiry',
            '⏰ Hết hạn': '⏰ Expired', '🔒 Đã thu hồi': '🔒 Revoked',
            'Chưa có snippet nào': 'No snippets yet', 'Không tìm thấy snippet phù hợp': 'No matching snippets found',
            'Không có tiêu đề': 'No title', 'Chia sẻ link': 'Share link', 'Thu hồi link': 'Revoke link',
            'Khôi phục link': 'Restore link', 'Toàn màn hình': 'Fullscreen', 'Xem chi tiết': 'View details',
            'Snippet này không có link chia sẻ': 'This snippet has no share link',
            'Vui lòng nhập code': 'Please enter code', 'Đã tạo snippet': 'Snippet created',
            'Đã cập nhật snippet': 'Snippet updated', 'Đã xóa snippet.': 'Snippet deleted.',
            'Xóa snippet thất bại.': 'Failed to delete snippet.', 'Đã copy code': 'Code copied',
            'Đã copy link chia sẻ': 'Share link copied', 'Đã thu hồi link': 'Link revoked',
            'Đã khôi phục link': 'Link restored', 'Mở file': 'Open file', 'Mở': 'Open',
            'Tìm theo tiêu đề, nội dung hoặc ngôn ngữ...': 'Search by title, content or language...',

            // ── Đã chặn ──
            'Người dùng đã chặn': 'Blocked users', 'Chặn người dùng': 'Block user',
            'Bạn chưa chặn ai.': "You haven't blocked anyone.", 'Bỏ chặn': 'Unblock', 'Chặn': 'Block',
            'Quản lý danh sách những người dùng bạn đã chặn.': "Manage the list of users you've blocked.",
            'Nhập ID hoặc tên để tìm. Họ sẽ không thể nhắn tin hay tương tác với bạn.': "Enter an ID or name to search. They won't be able to message or interact with you.",
            'Nhập ID hoặc tên để tìm...': 'Enter ID or name to search...', 'Hủy chọn': 'Deselect',
            'Đang chọn:': 'Selecting:', 'Đã chọn': 'Selected', 'Không tìm thấy kết quả.': 'No results found.',
            'Không tìm thấy người dùng.': 'No users found.', 'Đã bỏ chặn': 'Unblocked',
            'Bỏ chặn thất bại': 'Failed to unblock', 'Chặn thất bại': 'Failed to block', 'Đã chặn': 'Blocked',

            // ── Phiên đăng nhập ──
            'Phiên đăng nhập': 'Login sessions', 'Quản lý các thiết bị đang đăng nhập': 'Manage your active devices',
            'Không có phiên nào': 'No sessions', 'Thiết bị không xác định': 'Unknown device',
            'Hiện tại': 'Current', 'Đăng xuất': 'Log out', 'Đăng xuất tất cả': 'Log out all',
            'Đăng xuất tất cả các thiết bị khác?': 'Log out all other devices?',
            'Đã đăng xuất phiên': 'Session logged out', 'Đã đăng xuất tất cả phiên khác': 'All other sessions logged out',

            // ── Hồ sơ ──
            'Hồ sơ cá nhân': 'Profile', 'Chỉnh sửa hồ sơ': 'Edit profile', 'Đổi mật khẩu': 'Change password',
            'Đổi ảnh đại diện': 'Change avatar', 'Cập nhật thông tin hiển thị trong PingMe.': 'Update the info shown in PingMe.',
            'Các thay đổi của bạn sẽ được lưu vào tài khoản PingMe.': 'Your changes will be saved to your PingMe account.',
            'Xem thông tin tài khoản nội bộ của bạn': 'View your internal account info',
            'Tên hiển thị': 'Display name', 'Chức vụ': 'Position', 'Bộ phận / Team': 'Department / Team',
            'Vị trí làm việc': 'Workplace', 'Số điện thoại': 'Phone number', 'Ngày sinh': 'Date of birth',
            'Giới thiệu': 'About', 'Giới thiệu về bản thân...': 'About yourself...',
            'Mật khẩu hiện tại': 'Current password', 'Mật khẩu mới (tối thiểu 6 ký tự)': 'New password (min 6 characters)',
            'Xác nhận mật khẩu mới': 'Confirm new password', 'Xác nhận cập nhật hồ sơ?': 'Confirm profile update?',
            'Không thể tải hồ sơ. Vui lòng thử lại.': 'Failed to load profile. Please try again.',
            'Đang tải hồ sơ...': 'Loading profile...', 'Hồ sơ người dùng': 'User profile',

            // ── Chung (bạn bè / trạng thái) ──
            'Bỏ bạn bè?': 'Unfriend?', 'Đã chấp nhận kết bạn': 'Friend request accepted',
            'Đã gửi lời mời kết bạn': 'Friend request sent', 'Đã hủy lời mời': 'Request cancelled',
            'Đã từ chối': 'Declined', 'Đã xóa khỏi danh sách bạn bè': 'Removed from friends',
            'Có lỗi xảy ra': 'An error occurred', 'Đang tải...': 'Loading...', 'Đang tìm kiếm...': 'Searching...',
            'Đang tạo...': 'Creating...', 'Đã tải hết dữ liệu': 'All data loaded', 'Tải thêm': 'Load more',
            'Quay lại': 'Back', '← Quay về trang chủ': '← Back to home', 'Tiếp tục': 'Continue',
            'Xác nhận lưu': 'Confirm save', 'Hình ảnh': 'Image', 'Không có mô tả': 'No description',
            'Chọn': 'Select', 'Chỉnh sửa': 'Edit',

            // ── Bổ sung theo phản hồi ──
            'Quản lý': 'Manage', 'Xóa bộ lọc': 'Clear filters',
            'Chưa có chức vụ': 'No position', 'Chưa có bộ phận': 'No department',
            'Chưa có giới thiệu.': 'No bio.', 'Bảo mật tài khoản': 'Account security',
            'Mật khẩu': 'Password', 'Thay đổi mật khẩu đăng nhập của bạn': 'Change your login password',
            'Chọn một finding để xem chi tiết hoặc tạo mới.': 'Select a finding to view details or create a new one.'
        };
        for (const [v, e] of Object.entries(extras)) {
            viToEn[v] = e;
            viToEn[v.toUpperCase()] = e.toUpperCase();
            enToVi[e] = v;
            enToVi[e.toUpperCase()] = v.toUpperCase();
        }
        _textMapCache = { viToEn, enToVi };
        return _textMapCache;
    }

    function translateNode(node) {
        if (!node) return;
        if (node.nodeType === Node.TEXT_NODE) {
            const text = node.nodeValue;
            if (!text) return;
            const trimmed = text.trim();
            if (!trimmed) return;
            const maps = getTextMap();
            let translated = null;
            if (_lang === 'en' && maps.viToEn[trimmed]) translated = maps.viToEn[trimmed];
            else if (_lang === 'vi' && maps.enToVi[trimmed]) translated = maps.enToVi[trimmed];
            if (translated) {
                // Must preserve original surrounding whitespace
                node.nodeValue = text.replace(trimmed, translated);
            }
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            const tag = node.tagName.toUpperCase();
            if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'CODE' || tag === 'TEXTAREA') return;
            if (node.classList && (node.classList.contains('pm-message-text') || node.classList.contains('pm-msg-text') || node.classList.contains('pm-payload-preview'))) return;
            
            if (node.hasAttribute('placeholder')) {
                const p = node.getAttribute('placeholder').trim();
                const maps = getTextMap();
                let tp = null;
                if (_lang === 'en' && maps.viToEn[p]) tp = maps.viToEn[p];
                else if (_lang === 'vi' && maps.enToVi[p]) tp = maps.enToVi[p];
                if (tp) node.setAttribute('placeholder', tp);
            }
            
            // Collect children first to avoid live NodeList issues if mutations occur, though here it's safe
            const children = Array.from(node.childNodes);
            for (let i = 0; i < children.length; i++) {
                translateNode(children[i]);
            }
        }
    }

    const domObserver = new MutationObserver((mutations) => {
        for (const m of mutations) {
            if (m.addedNodes.length > 0) {
                m.addedNodes.forEach(node => {
                    // Skip chat history append completely
                    if (node.nodeType === Node.ELEMENT_NODE && node.classList) {
                        if (node.classList.contains('pm-chat-history') || node.classList.contains('pm-message-row') || node.classList.contains('pm-message-line')) return;
                    }
                    translateNode(node);
                });
            }
        }
    });

    let _isObserving = false;

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
        translateNode(scope === document ? document.body : scope);
        
        if ((scope === document || scope === document.body) && !_isObserving) {
            domObserver.observe(document.body, { childList: true, subtree: true });
            _isObserving = true;
        }
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
