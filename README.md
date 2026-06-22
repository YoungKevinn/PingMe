# PingMe

Ứng dụng chat realtime dành cho nhóm bảo mật / pentest, xây dựng trên ASP.NET Core 9 + Blazor WebAssembly + SignalR + MySQL.

---

## Tech Stack

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET Core 9, EF Core 8, Pomelo MySQL |
| Frontend | Blazor WebAssembly (.NET 8), MudBlazor, Markdig |
| Realtime | SignalR |
| Auth | JWT Bearer |
| Database | MySQL 8 |
| Email | MailKit / MimeKit |
| Media | SixLabors.ImageSharp |

---

## Yêu cầu

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- MySQL 8 đang chạy trên `127.0.0.1:3306`
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

---

## Cài đặt & Chạy

### 1. Clone

```bash
git clone https://github.com/YoungKevinn/PingMe.git
cd PingMe
```

### 2. Tạo database

Đăng nhập MySQL và chạy:

```sql
CREATE DATABASE IF NOT EXISTS dbweb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 3. Cấu hình kết nối

File `PingMe/appsettings.json` (đã được cấu hình sẵn):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=dbweb;User=root;Password=123456;..."
}
```

Nếu credentials MySQL của bạn khác thì sửa `Password` và `User` tương ứng.

### 4. Apply migration

```bash
cd PingMe
dotnet ef database update
```

### 5. Chạy Backend

```bash
cd PingMe
dotnet run
```

Backend mặc định lắng nghe tại `https://localhost:5001`.

Swagger UI: `https://localhost:5001/swagger`

### 6. Chạy Frontend (tab mới)

```bash
cd PingMe.Frontend
dotnet run
```

Frontend mặc định tại `https://localhost:7xxx` (xem terminal output).

---

## Tính năng

### Chat & Messaging
- Tin nhắn trực tiếp (DM) và nhóm realtime qua SignalR
- Reply, forward, ghim tin nhắn
- Chỉnh sửa & thu hồi tin nhắn, lịch sử chỉnh sửa
- Đính kèm file / ảnh / voice
- Emoji reactions
- Tin nhắn tự hủy (TTL)
- Đánh dấu đã đọc (read receipts)
- Typing indicator

### Bình chọn (Poll)
- Tạo poll ngay trong chat (nút 🗳️ trong thanh nhập liệu)
- Vote realtime — kết quả cập nhật cho tất cả thành viên ngay lập tức
- Hỗ trợ chọn nhiều đáp án
- Hiển thị thanh % tiến trình và tổng lượt bình chọn

### Nhóm
- Tạo / chỉnh sửa nhóm, quản lý thành viên
- Phân quyền: Admin / CoAdmin / Member
- Timeline nhóm, Group Task

### Bảo mật
- Xác thực JWT, quản lý session đa thiết bị
- Block người dùng
- Audit log
- One-Time Secret (tin nhắn tự hủy sau khi xem một lần)
- Code Snippet chia sẻ có TTL và token truy cập
- Webhook tích hợp

### Công cụ Pentest / Security
- IOC Indicator tracker (IP, domain, hash, URL...)
- Pentest Finding tracker (`/vuln` command)
- Chat Reminder (`/reminder` command)
- Group Task (`/task` command)

### Khác
- Tìm kiếm toàn cục
- Lưu tin nhắn (saved messages)
- Đổi nickname, đổi ảnh nền cuộc trò chuyện
- Bạn bè (friend request / accept)
- Hỗ trợ Markdown trong tin nhắn

---

## Cấu trúc thư mục

```
PingMe/
├── PingMe/                  # Backend (ASP.NET Core API)
│   ├── Controllers/         # REST API endpoints
│   ├── Data/                # AppDbContext, migrations
│   ├── DTOs/                # Request / Response objects
│   ├── Hubs/                # SignalR ChatHub
│   ├── Migrations/          # EF Core migrations
│   ├── Models/              # Entity models
│   ├── Services/            # Business logic
│   ├── Middleware/          # JWT revocation, audit log
│   ├── BackgroundJobs/      # Message expiry, reminder dispatch
│   └── appsettings.json
│
└── PingMe.Frontend/         # Frontend (Blazor WASM)
    ├── Components/
    │   ├── Pages/           # Chat, Friends, Groups, Profile...
    │   ├── Dialogs/         # Modal dialogs
    │   ├── MessageList.razor
    │   └── MessageInput.razor
    ├── Models/              # DTO classes (mirror backend)
    ├── Services/            # API calls, SignalR, cache
    └── wwwroot/
```

---

## API Endpoints chính

| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/auth/register` | Đăng ký |
| POST | `/api/auth/login` | Đăng nhập |
| GET | `/api/messages/dm/{peerId}` | Lấy DM messages |
| GET | `/api/messages/group/{groupId}` | Lấy group messages |
| POST | `/api/messages` | Gửi tin nhắn |
| POST | `/api/messages/upload` | Upload file |
| POST | `/api/polls` | Tạo poll |
| POST | `/api/polls/{id}/vote` | Vote |
| DELETE | `/api/polls/{id}/vote` | Bỏ vote |
| POST | `/api/groups` | Tạo nhóm |
| GET | `/api/friends` | Danh sách bạn bè |
| GET | `/api/search` | Tìm kiếm |

Xem đầy đủ tại `https://localhost:5001/swagger`.

---

## SignalR Events

| Event | Chiều | Mô tả |
|---|---|---|
| `ReceiveMessage` | Server → Client | Tin nhắn mới |
| `MessageEdited` | Server → Client | Tin nhắn được sửa |
| `MessageDeleted` | Server → Client | Tin nhắn bị xóa |
| `MessageReactionUpdated` | Server → Client | Reaction thay đổi |
| `MessagePinned` | Server → Client | Ghim / bỏ ghim |
| `MessageRead` | Server → Client | Read receipt |
| `PollVoteUpdated` | Server → Client | Kết quả poll cập nhật |
| `UserStatusChanged` | Server → Client | Online / offline |
| `UserTyping` | Server → Client | Đang gõ... |
| `IncomingCall` | Server → Client | Cuộc gọi đến |

---

## Lưu ý

- File upload tối đa **25 MB**
- Tin nhắn văn bản tối đa **4000 ký tự**
- Poll tối đa **10 lựa chọn**
- Khi chạy lần đầu, backend tự tạo bảng nếu dùng `dotnet ef database update`
