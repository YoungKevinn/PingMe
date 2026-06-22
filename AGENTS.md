# AGENTS.md — PingMe

> Đặt file này ở root làm việc `DoAnWeb/`, ngang hàng với `PingMe/` và `PingMe.Frontend/`.
> Solution file hiện nằm tại `PingMe/PingMe.sln`.

## Project identity

PingMe là DevSecOps collaboration platform, không chỉ là chat app.

| Layer | Module |
|---|---|
| Chat realtime | Collaboration |
| Snippet Center | Dev workflow |
| IOC Center | SOC workflow |
| Finding Tracker | Pentest workflow |
| Audit / Session / Security | Governance |

**Tech stack:**
- Backend: ASP.NET Core Web API + EF Core + MySQL + JWT + SignalR
- Frontend: Blazor WebAssembly + MudBlazor
- Markdown: Markdig | Code highlight: highlight.js

---

## Response language

**Luôn trả lời bằng tiếng Việt.** Ngắn gọn, thực tế, không vague.

---

## Build commands

Restore nếu dependencies có thể stale:

```bash
dotnet restore
```

Build sau mỗi lần sửa:

```bash
dotnet build PingMe/PingMe.csproj
dotnet build PingMe.Frontend/PingMe.Frontend.csproj
```

Nếu có solution file:

```bash
dotnet build PingMe/PingMe.sln
```

Nếu build lỗi: **dừng lại, sửa lỗi build trước, không tiếp tục feature.**

---

## Migration — chỉ khi schema thật sự thay đổi

Terminal / Codex CLI:

```bash
dotnet ef migrations add <Name> --project PingMe --startup-project PingMe --context AppDbContext
dotnet ef database update --project PingMe --startup-project PingMe --context AppDbContext
```

Visual Studio Package Manager Console:

```powershell
Add-Migration <Name> -Project PingMe -StartupProject PingMe -Context AppDbContext
Update-Database -Project PingMe -StartupProject PingMe -Context AppDbContext
```

**Không tự chạy `dotnet ef database update` khi chưa được user xác nhận.**
Nếu cần migration, giải thích lý do trước rồi chờ xác nhận.

---

## Safety rules

**Không bao giờ:**
- `git reset --hard`
- Xóa hoặc sửa migration cũ
- Reset hoặc drop database
- Rewrite kiến trúc không liên quan
- Sửa styling diện rộng không liên quan bug
- Thêm feature mới khi chưa được yêu cầu

**Không expose secrets:**
- Không print JWT secret, connection string, API key, SMTP password
- Không commit `appsettings.Development.json`, `.env`, file secret local
- Nếu cần config, dùng placeholder và document để user tự set

**Luôn:**
- Đọc code thật trong repo trước khi sửa
- Không đoán tên file, không copy mù từ prompt
- Sửa tối thiểu, đúng bug, build pass
- Sau mỗi fix báo cáo chính xác file đã sửa

---

## Bug fixing workflow

**Fix từng bug một**, không fix nhiều bug cùng lúc trừ khi được yêu cầu rõ ràng.

Quy trình mỗi bug:
1. Đọc file liên quan trong repo
2. Xác định root cause
3. Sửa tối thiểu
4. Build
5. Báo cáo:
   - Root cause là gì
   - File đã sửa (chính xác)
   - Có cần migration không
   - Bước test thủ công

Không sửa file ngoài scope của bug đang fix trừ khi bắt buộc để compile.

---

## SignalR rules

Khi thay đổi SignalR, phải đồng bộ cả hai:
- `PingMe/Hubs/ChatHub.cs`
- `PingMe.Frontend/Services/ChatHubService.cs`

Không tự thêm event name mới nếu không cần thiết.
Khi thay đổi broadcast, ghi rõ client nào nhận:
- sender, receiver, group members, affected user

---

## Current priority — Bug list

Không làm Global Search khi chưa được yêu cầu.

| # | Bug |
|---|---|
| 1 | Reaction add/remove DM + group không hoạt động |
| 2 | Message action chỉ hiện khi click, chưa hiện khi hover |
| 3 | Header "..." menu lệch, item disabled hiện cursor X |
| 4 | IOC Center hiện `Group: #ID` thay vì tên group |
| 5 | IOC light mode khó đọc title/value/badge |
| 6 | Unread badge không về 0 sau khi đọc |
| 7 | Message input lệch dòng, chưa auto expand |
| 8 | Tin quá dài gây lỗi SaveChangesAsync |
| 9 | Pin tin nhắn chưa có giới hạn 5 |
| 10 | Thêm người vào group dù chưa kết bạn |
| 11 | IOC trùng, phân quyền IOC sai, bị kick vẫn thấy IOC |
| 12 | Friend request sau unfriend bị lỗi incoming |
| 13 | Profile nhập quá dài gây crash |
| 14 | Code/IOC card light mode + fullscreen code bị vỡ format |

---

## Important files

### Backend
```
PingMe/Data/AppDbContext.cs
PingMe/Program.cs
PingMe/Models/User.cs
PingMe/Models/Message.cs
PingMe/Models/Group.cs
PingMe/Models/MessageReaction.cs
PingMe/Models/IocIndicator.cs
PingMe/Controllers/MessageController.cs
PingMe/Controllers/ReactionController.cs
PingMe/Controllers/IocController.cs
PingMe/Controllers/GroupController.cs
PingMe/Controllers/UserController.cs
PingMe/Services/MessageService.cs
PingMe/Services/ReactionService.cs
PingMe/Services/IocService.cs
PingMe/Services/GroupService.cs
PingMe/Services/UserService.cs
PingMe/Services/ConversationService.cs
PingMe/Services/ReadReceiptService.cs
PingMe/Hubs/ChatHub.cs
```

### Frontend
```
PingMe.Frontend/Program.cs
PingMe.Frontend/wwwroot/index.html
PingMe.Frontend/wwwroot/app.css
PingMe.Frontend/Services/ApiService.cs
PingMe.Frontend/Services/MessageService.cs
PingMe.Frontend/Services/ReactionService.cs
PingMe.Frontend/Services/IocService.cs
PingMe.Frontend/Services/GroupService.cs
PingMe.Frontend/Services/ChatHubService.cs
PingMe.Frontend/Components/Pages/Chat.razor
PingMe.Frontend/Components/MessageList.razor
PingMe.Frontend/Components/MessageInput.razor
PingMe.Frontend/Components/ConversationList.razor
PingMe.Frontend/Components/Pages/IocPage.razor
PingMe.Frontend/Components/Pages/SnippetsPage.razor
PingMe.Frontend/Helpers/MarkdownHelper.cs
PingMe.Frontend/Models/IocDtos.cs
```

---

## Bug specs

### Bug 1 — Reaction

**Files:** `ReactionController.cs`, `ReactionService.cs`, `MessageReaction.cs`, `AppDbContext.cs`, `ChatHub.cs`, frontend `ReactionService.cs`, `MessageList.razor`, `ChatHubService.cs`

- `POST /api/messages/{messageId}/reactions` body `{ "emoji": "..." }`
- `DELETE /api/messages/{messageId}/reactions?emoji=...` — query string, không dùng route segment (Unicode emoji vỡ route)
- Add/remove idempotent
- DM: chỉ sender hoặc receiver được react
- Group: chỉ member hiện tại được react
- Broadcast SignalR payload `{ messageId, groupId, senderId, receiverId, reactions: [{emoji, count, userIds}] }`
- Dùng đúng group naming hiện có trong ChatHub, không tự bịa

| Triệu chứng | Root cause |
|---|---|
| Click emoji → không có network request | Lỗi handler trong `MessageList.razor` |
| 401 | JWT token trong `ApiService.cs` |
| 404 | Route trong `ReactionController.cs` |
| 400 | Permission hoặc emoji validation |
| 500 | `ReactionService.cs` hoặc DB |

---

### Bug 2 — Message action hover

**File:** `MessageList.razor`

- Action container phải **luôn được render**, ẩn bằng `opacity/visibility/pointer-events`
- Không dùng conditional render chỉ khi hover — gây layout jump
- Hover vào message row thì hiện action

```css
.pm-message-actions { opacity:0; visibility:hidden; pointer-events:none; }
.pm-message-line:hover .pm-message-actions { opacity:1; visibility:visible; pointer-events:auto; }
```

---

### Bug 3 — Header "..." menu

**File:** `Chat.razor`

- Nếu `MudMenu`/`MudPopover` gây overlay bug → thay bằng HTML dropdown + CSS
- Item chưa làm: dùng class `is-disabled`, **không** dùng attribute `disabled` HTML
- `is-disabled`: `opacity: 0.55; cursor: default; pointer-events: none`
- Dropdown: z-index cao, không bị clip

---

### Bug 4 + 5 — IOC Center

**Files:** `IocDtos.cs` (backend + frontend), `IocService.cs`, `IocController.cs`, `IocPage.razor`

- `IocResponse` phải có `string? GroupName` và `string? PeerDisplayName`
- UI hiển thị `Group: <GroupName>`, fallback mới dùng `#ID`
- Visibility: chỉ thấy IOC group khi còn là `GroupMember`
- Duplicate block: `Type + Value + GroupId + PeerUserId` cùng scope
- IOC personal: duplicate theo `CreatedByUserId`
- **Không** tạo index trên `IocIndicators.Value` nếu Value dài (MySQL key limit)
- Light mode: title, value, badge, button phải đủ contrast; không phá dark mode

---

### Bug 6 — Unread badge

**Files:** `ConversationService.cs`, `ReadReceiptService.cs`, `ReadReceiptController.cs`, `MessageList.razor`, `ConversationList.razor`, `Chat.razor`

- Group unread không tính `message.SenderId == currentUserId`
- Khi mở conversation: gọi mark read
- Sau mark read: trigger `OnReadStateChanged` callback
- `Chat.razor` truyền `OnReadStateChanged="RefreshConversationListAsync"`

---

### Bug 7 + 8 — Message input + validation

**Files:** `MessageInput.razor`, `MessageService.cs`

- Textarea auto expand, max-height có scroll
- Max length 4000, vượt thì disable send hoặc Snackbar warning
- Backend validate trước `SaveChangesAsync`:
  - Text: không rỗng, <= 4000
  - File/Image: content có thể rỗng
  - Edit: không rỗng, <= 4000
  - Trả `BadRequest` rõ, không throw DB exception

---

### Bug 9 — Pin limit

**File:** `MessageService.cs`

- Tối đa 5 pinned messages mỗi DM/group
- Không tính `IsDeleted` hoặc `IsPinned == false`
- Pin thứ 6 → trả lỗi: `"Mỗi hội thoại chỉ được ghim tối đa 5 tin nhắn."`

---

### Bug 10 — Group member permission

**Files:** `GroupService.cs`, `GroupController.cs`, frontend `GroupService.cs`

- Creator là Admin
- Add member: requester phải Admin/CoAdmin, target phải đã kết bạn với requester
- Không thêm duplicate member
- Broadcast `GroupMemberAdded` / `GroupMemberKicked` tới group và target user

---

### Bug 12 — Friend request sau unfriend

**Files:** `FriendService.cs`, `FriendController.cs`, frontend `FriendService.cs`

- Khi unfriend (soft delete / status Removed), gửi lại lời mời phải restore hoặc tạo mới đúng
- Receiver phải thấy incoming request
- Không tạo duplicate friendship conflict

---

### Bug 13 — Profile validation

**Files:** `UserService.cs`, `UserController.cs`

| Field | Max |
|---|---|
| DisplayName | 100 |
| Bio | 500 |
| JobTitle | 100 |
| Department | 100 |
| WorkLocation | 150 |
| PhoneNumber | 30 |

Validate trước `SaveChangesAsync`. Trả `400 BadRequest` rõ, không crash.

---

### Bug 14 — Light mode markdown/code

**Files:** `MessageList.razor`, `MarkdownHelper.cs`, `app.css`, Snippet page

- IOC card: dark/light đều rõ
- Hash dài: word-wrap đúng, không tràn bubble
- Code block: monospace, `overflow: auto`, giữ whitespace
- Fullscreen code viewer: không mất format
- Không phá dark mode

---

## Manual verification checklist

Build pass chưa đủ. Sau mỗi fix cần test thủ công:

**Reaction:** DM add/remove, Group add/remove, SignalR update realtime
**Hover action:** Hover thấy action, không nhảy layout
**Header menu:** Mở được, Quản lý thành viên bấm được, disabled item không có cursor X
**IOC:** Tên group hiển thị, light mode rõ, duplicate bị chặn, bị kick không thấy IOC
**Unread:** Mở hội thoại → badge về 0, refresh sidebar
**Input:** Auto expand, tin dài bị chặn ở 4000
**Pin:** Không pin được tin thứ 6
**Group:** Chưa kết bạn không add được vào group
**Friend:** Unfriend → gửi lại → receiver thấy incoming
**Profile:** Nhập dài không crash
**Code/Markdown:** Fullscreen không vỡ format, light mode rõ

Với permission bugs, test đủ 4 role: unauthorized → normal member → CoAdmin → Admin → kicked user

---

## Known gotchas

| Issue | Fix |
|---|---|
| EF snapshot: `b.Navigation("AccessLogs")` lỗi nếu `CodeSnippet` không có nav prop | Xóa dòng đó trong snapshot |
| Index trên `IocIndicators.Value` varchar(2048) | Không tạo index, vượt MySQL 767-byte key limit |
| `AppDbContext.cs` dùng param tên `b` | Dùng `b.Entity<...>`, không dùng `modelBuilder.Entity<...>` |
| Razor nested quotes: `@onclick="() => Method(id, "value")"` | Dùng helper method thay thế |
| Avatar/file URL sai host | Build full URL từ backend base URL — frontend/backend có thể khác port |
| Message action render conditional khi hover | Luôn render, ẩn bằng opacity |
| `MudMenu`/`MudPopover` overlay bug | Thay bằng HTML dropdown + CSS |
