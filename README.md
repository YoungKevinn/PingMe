# PingMe

Ứng dụng chat realtime cho nhóm bảo mật / pentest, xây dựng theo **mô hình MVC** trên **ASP.NET Core 9** + **EF Core 8 (MySQL)** + **SignalR**.

> Toàn bộ ứng dụng nằm trong **một project ASP.NET Core MVC duy nhất** (`PingMe/`): Controller trả về View (`.cshtml`), kèm một lớp **Web API** (`/api/...`) và **SignalR Hub** để render động & realtime phía client bằng JavaScript thuần.

---

## Mục lục
- [Kiến trúc MVC](#kiến-trúc-mvc)
- [Tech Stack](#tech-stack)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Yêu cầu](#yêu-cầu)
- [Cài đặt & Chạy](#cài-đặt--chạy)
- [Tính năng](#tính-năng)
- [API & SignalR](#api--signalr)

---

## Kiến trúc MVC

Dự án tuân thủ mô hình **Model – View – Controller** của ASP.NET Core:

| Tầng | Vị trí | Vai trò |
|---|---|---|
| **Model** | `Models/` | Thực thể domain (User, Group, Message, IocIndicator, PentestFinding…) |
| **View** | `Views/**/*.cshtml` | Giao diện Razor + layout (`Views/Shared/_Layout.cshtml`) |
| **Controller (MVC)** | `MvcControllers/` | Trả về View cho từng trang (Account, Chat, Groups, Ioc, Pentest…) |
| **Controller (API)** | `Controllers/` | REST API trả JSON (`/api/...`) cho client |
| **ViewModel / DTO** | `ViewModels/`, `DTOs/` | Dữ liệu truyền giữa các tầng |
| **Service** | `Services/` | Toàn bộ logic nghiệp vụ (không nằm ở Controller/View) |
| **Data** | `Data/AppDbContext.cs` | Tầng truy cập dữ liệu (EF Core) |
| **Realtime** | `Hubs/ChatHub.cs` | SignalR cho tin nhắn/poll/typing… thời gian thực |

**Định tuyến** (trong `Program.cs`):
```csharp
builder.Services.AddControllersWithViews();        // MVC
app.MapControllerRoute("auth", "auth/{action=Login}", new { controller = "Account" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();                               // Web API (attribute routing)
app.MapHub<ChatHub>("/hubs/chat");                 // SignalR
```

> View là Razor shell mỏng; phần nội dung động được render phía client bằng JS (`wwwroot/js/*.js`) gọi Web API + SignalR. Đây là MVC kết hợp client-side rendering (kiểu AJAX), không phải Blazor.

---

## Tech Stack

| Layer | Công nghệ |
|---|---|
| Framework | ASP.NET Core 9 (MVC) |
| ORM / DB | EF Core 8, Pomelo MySQL, MySQL 8+ |
| Realtime | SignalR |
| Auth | JWT Bearer + Cookie |
| Frontend | Razor Views (`.cshtml`) + JavaScript thuần + CSS |
| Email | MailKit / MimeKit |
| Ảnh | SixLabors.ImageSharp |
| Mật khẩu | BCrypt.Net |
| Validation | FluentValidation |
| API docs | Swashbuckle (Swagger) |

---

## Cấu trúc thư mục

```
PingMe/                       # Project ASP.NET Core MVC (duy nhất)
├── Controllers/              # Web API controllers (REST, trả JSON)
├── MvcControllers/           # MVC controllers (trả về View)
├── Views/                    # Razor Views (.cshtml)
│   ├── Shared/_Layout.cshtml # Layout + sidebar + header
│   ├── Chat/  Groups/  Ioc/  Pentest/  Search/  Snippets/ ...
│   └── Shared/Dialogs/       # Partial views cho dialog
├── Models/                   # Entity models (domain)
├── ViewModels/               # ViewModel cho View
├── DTOs/                     # Request/Response objects của API
├── Services/                 # Logic nghiệp vụ
├── Data/AppDbContext.cs      # EF Core DbContext
├── Migrations/               # EF Core migrations
├── Hubs/ChatHub.cs           # SignalR hub
├── Middleware/               # JWT revocation, audit log…
├── BackgroundJobs/           # Hết hạn tin nhắn, gửi reminder…
├── Settings/                 # Option classes (Jwt, Email, FileStorage…)
├── wwwroot/
│   ├── js/                   # api.js, chat.js, loc.js, search.js… (render client)
│   ├── css/                  # app.css, site.css
│   └── uploads/              # File/ảnh người dùng tải lên
├── Program.cs                # Cấu hình app, DI, routing
└── appsettings.json          # Cấu hình (DB, JWT, Email…)
```

---

## Yêu cầu

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- **MySQL 8+** đang chạy tại `127.0.0.1:3306`
- (Tùy chọn) `dotnet-ef`: `dotnet tool install --global dotnet-ef`

---

## Cài đặt & Chạy

### 1. Tạo database
```sql
CREATE DATABASE IF NOT EXISTS dbweb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. Cấu hình kết nối
Sửa `PingMe/appsettings.json` (và `appsettings.Development.json` khi chạy môi trường Development) cho khớp MySQL của bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=dbweb;User=root;Password=123456;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=None;"
}
```
> ⚠️ Khi chạy bằng Visual Studio / `dotnet run` (mặc định môi trường **Development**), file `appsettings.Development.json` sẽ **ghi đè** connection string — nhớ chỉnh cả file này.

### 3. Áp dụng migration
```bash
cd PingMe
dotnet ef database update
```

### 4. Chạy ứng dụng
```bash
cd PingMe
dotnet run
```
- Ứng dụng: **https://localhost:5001** (hoặc http://localhost:5000)
- Swagger UI: **https://localhost:5001/swagger**

> Chỉ có **một** ứng dụng để chạy — không có project frontend riêng.

---

## Tính năng

### Chat & Messaging
- DM và chat nhóm realtime (SignalR)
- Reply, forward, ghim tin nhắn
- Sửa & thu hồi tin nhắn + lịch sử chỉnh sửa
- Đính kèm file / ảnh / voice
- Emoji reactions, typing indicator, read receipts
- Tin nhắn tự hủy (TTL), gửi code snippet, lệnh slash (`/ioc`, `/vuln`, `/task`, `/reminder`, `/snippet`)
- Bình chọn (Poll) realtime ngay trong chat

### Nhóm
- Tạo / sửa nhóm, đổi tên & ảnh nhóm
- Phân quyền **Admin / CoAdmin / Member**; kick, đổi vai trò
- **Giải tán nhóm** (Admin/CoAdmin); rời nhóm tự chuyển quyền Admin
- Timeline nhóm, Group Task

### Bảo mật & Pentest
- Xác thực JWT + Cookie, quản lý phiên đa thiết bị
- Block người dùng, audit log
- IOC tracker (IP / domain / hash / URL / CVE)
- Pentest Finding tracker, Chat Reminder
- One-Time Secret, Webhook, Code Snippet có TTL & token chia sẻ

### Khác
- Tìm kiếm toàn cục (tin nhắn, người dùng, nhóm, snippet, IOC, finding, task, file)
- Lưu tin nhắn, đổi nickname, đổi ảnh nền hội thoại
- Bạn bè (kết bạn / chấp nhận)
- Hồ sơ cá nhân, đổi mật khẩu
- **Đa ngôn ngữ vi/en** (nút quả địa cầu), Markdown trong tin nhắn

---

## API & SignalR

### API endpoints chính
| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/auth/register` · `/api/auth/login` | Đăng ký / Đăng nhập |
| GET | `/api/messages/dm/{peerId}` · `/api/messages/group/{groupId}` | Lấy tin nhắn |
| POST | `/api/messages` · `/api/messages/upload` | Gửi tin nhắn / Upload file |
| POST | `/api/polls` · `/api/polls/{id}/vote` | Tạo poll / Vote |
| POST/DELETE | `/api/groups` · `/api/groups/{id}/leave` · `/api/groups/{id}` | Tạo / Rời / Giải tán nhóm |
| GET | `/api/iocs` · `/api/snippets` · `/api/search` | IOC / Snippet / Tìm kiếm |

> Danh sách đầy đủ: **`/swagger`**.

### SignalR events (Server → Client)
`ReceiveMessage`, `MessageEdited`, `MessageDeleted`, `MessageReactionUpdated`, `MessagePinned`, `MessageRead`, `PollVoteUpdated`, `UserStatusChanged`, `UserTyping`, `GroupDeleted`, `GroupMemberKicked`, `IncomingCall`.

---

## Ghi chú
- Thời gian được lưu **UTC** trong DB và tự chuyển sang giờ địa phương ở client (bộ chuyển đổi UTC toàn cục trong `AppDbContext`).
- File tải lên nằm ở `wwwroot/uploads/`. Giới hạn mặc định: file ≤ **25 MB**, tin nhắn văn bản ≤ **4000 ký tự**, poll ≤ **10 lựa chọn**.
- Cấu hình DB / JWT / Email đặt trong `appsettings.json`.
