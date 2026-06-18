# PingMe

**PingMe** là nền tảng cộng tác nội bộ dành cho nhóm kỹ thuật và an toàn thông tin, hỗ trợ trò chuyện thời gian thực, chia sẻ code snippet an toàn, quản lý IOC, workflow pentest finding, task, timeline, audit log và các cơ chế bảo mật phục vụ DevSecOps/SOC/Pentest team.

PingMe không chỉ là ứng dụng chat thông thường, mà được định hướng thành một **Dev/SOC/Pentest Collaboration Platform**.

## Mục tiêu dự án

PingMe kết hợp các workflow thường gặp trong nhóm kỹ thuật, SOC và pentest:

- **Realtime Chat**: lớp cộng tác chính cho cá nhân và nhóm.
- **Snippet Center**: hỗ trợ Dev workflow, chia sẻ code, markdown và snippet.
- **IOC Center**: hỗ trợ SOC workflow, quản lý indicator of compromise.
- **Pentest Finding Tracker**: hỗ trợ pentest workflow, theo dõi lỗ hổng và trạng thái xử lý.
- **Task Center & Threat Timeline**: quản lý công việc và theo dõi sự kiện kỹ thuật/bảo mật.
- **Security Governance**: audit log, session revoke, anomaly alert và block user.

## Tech Stack

### Backend

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- MySQL
- SignalR
- JWT Authentication
- Background Jobs

### Frontend

- Blazor WebAssembly
- MudBlazor
- Markdig
- highlight.js
- JavaScript interop cho scroll, audio recording và WebRTC call

## Tính năng chính

### 1. Core Chat

- Chat 1-1 và group chat
- Gửi text, hình ảnh, file, emoji
- Reply tin nhắn
- React emoji
- Edit tin nhắn dạng soft edit
- Delete/recall tin nhắn dạng soft delete
- Search trong conversation
- Infinite scroll/phân trang
- Read receipt
- Typing indicator
- Online status và LastSeen
- Conversation nickname/background
- Unread badge
- Group role: Admin, Co-Admin, Member
- Mention `@user` và `@all` trong group
- Voice message
- WebRTC call cơ bản

### 2. Dev Collaboration

- Render Markdown trong tin nhắn
- Highlight code block bằng highlight.js
- Nút copy code trong code block
- Snippet Center
- Tạo, sửa, xóa, tìm kiếm snippet
- Auto detect language
- Auto title
- Fullscreen snippet viewer
- Share snippet bằng token
- Hỗ trợ thời hạn share link: 1 giờ, 1 ngày, 7 ngày hoặc không hạn
- Revoke/restore share link
- Theo dõi AccessCount và LastAccessedAt
- Ghi log SnippetAccessLog khi mở share link
- Chèn snippet vào chat

### 3. SOC / Cybersecurity

- Lệnh `/ioc` trong chat
- Render IOC card trong tin nhắn
- IOC Center
- Lưu IOC vào database
- Search/filter IOC theo keyword, type, severity, status
- Add/Edit/Delete IOC
- Update status:
  - Open
  - Investigating
  - Resolved
  - FalsePositive
- External links:
  - CVE mở NVD
  - IP/hash/domain mở VirusTotal
  - URL mở trực tiếp
- IOC gắn được với MessageId, GroupId hoặc PeerUserId
- Dùng được trong cả DM và group

### 4. Pentest Workflow

- Pentest Finding Tracker
- Lệnh `/vuln` trong chat để tạo finding nhanh
- CRUD finding
- Filter theo severity, status, group
- Severity:
  - Info
  - Low
  - Medium
  - High
  - Critical
- Status:
  - Open
  - Confirmed
  - Exploited
  - Mitigated
  - Closed
  - FalsePositive
  - AcceptedRisk
- Theo dõi:
  - Description
  - PoC
  - Impact
  - Remediation
  - StepsToReproduce
- Badge màu theo severity/status

### 5. Task Center

- Lệnh `/task` trong chat
- Quản lý task theo group
- Giao task cho thành viên
- Filter theo group, assignee, status, priority
- Theo dõi task quá hạn
- Permission theo group role

### 6. Threat Timeline

- Tổng hợp các sự kiện kỹ thuật/bảo mật:
  - IOC
  - Finding
  - Task
  - File liên quan
- Không hiển thị tin nhắn thường để tránh nhiễu
- Có nút điều hướng sang IOC/Finding/Task/Chat liên quan

### 7. Security & Governance

- AuditLog
- UserSessions
- JWT/session revoke
- Login anomaly email alert
- Message expiry background job
- Block/unblock user
- Group webhook
- One-time secret sharing
- Dark/light mode

## Kiến trúc tổng quan

PingMe được tổ chức theo các lớp chức năng:

- **Chat realtime** = collaboration layer
- **Snippet Center** = Dev workflow
- **IOC Center** = SOC workflow
- **Finding Tracker** = Pentest workflow
- **Task Center + Threat Timeline** = workflow tracking
- **Audit/session/security** = governance layer

## Cấu trúc thư mục

```text
PingMe/
├── BackgroundJobs/
├── Controllers/
├── DTOs/
├── Data/
├── Hubs/
├── Middleware/
├── Migrations/
├── Models/
├── Services/
├── Settings/
├── wwwroot/
├── PingMe.Frontend/
│   ├── Components/
│   ├── Helpers/
│   ├── Models/
│   ├── Services/
│   └── wwwroot/
├── Program.cs
├── PingMe.csproj
└── PingMe.sln
```

## Yêu cầu môi trường

- .NET SDK 8
- MySQL Server
- Entity Framework Core Tools
- Trình duyệt hiện đại
- Visual Studio 2022 hoặc VS Code

Cài EF Core tools nếu máy chưa có:

```bash
dotnet tool install --global dotnet-ef
```

Hoặc cập nhật nếu đã cài:

```bash
dotnet tool update --global dotnet-ef
```

## Clone project

```bash
git clone https://github.com/YoungKevinn/PingMe.git
cd PingMe
git checkout MinhKhoa
```

Nếu branch `MinhKhoa` đã được đưa lên `main`, có thể dùng trực tiếp:

```bash
git checkout main
```

## Cấu hình database MySQL

Tạo database local:

```sql
CREATE DATABASE IF NOT EXISTS pingme_dev
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

Có thể tạo user riêng cho project:

```sql
CREATE USER IF NOT EXISTS 'pingme'@'localhost' IDENTIFIED BY 'pingme123';
CREATE USER IF NOT EXISTS 'pingme'@'127.0.0.1' IDENTIFIED BY 'pingme123';

GRANT ALL PRIVILEGES ON pingme_dev.* TO 'pingme'@'localhost';
GRANT ALL PRIVILEGES ON pingme_dev.* TO 'pingme'@'127.0.0.1';

FLUSH PRIVILEGES;
```

## Cấu hình môi trường local

Tạo file `appsettings.Development.json` ở thư mục backend khi chạy local.

Ví dụ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3307;Database=pingme_dev;User=pingme;Password=pingme123;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=None;"
  },
  "JwtSettings": {
    "Secret": "CHANGE_ME_TO_A_LONG_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "PingMe",
    "Audience": "PingMeUsers",
    "ExpiryMinutes": 1440
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "PingMe Security",
    "SenderEmail": "your_email@gmail.com",
    "Username": "your_email@gmail.com",
    "Password": "your_app_password"
  }
}
```

Không commit `appsettings.Development.json` lên GitHub vì file này chứa cấu hình local và secret.

## Chạy migration database

### Cách 1: Package Manager Console

```powershell
Update-Database -Project PingMe -StartupProject PingMe -Context AppDbContext
```

### Cách 2: .NET CLI

Nếu đang đứng ở root project backend, chạy:

```bash
dotnet ef database update --context AppDbContext
```

Nếu đứng ở thư mục cha của solution, có thể chỉ rõ project:

```bash
dotnet ef database update --project ./PingMe.csproj --startup-project ./PingMe.csproj --context AppDbContext
```

## Chạy backend

Ở thư mục chứa `PingMe.csproj`:

```bash
dotnet restore ./PingMe.csproj
dotnet build ./PingMe.csproj
dotnet run --project ./PingMe.csproj
```

Backend thường chạy ở một trong các cổng được cấu hình trong `Properties/launchSettings.json`.

## Chạy frontend

```bash
cd PingMe.Frontend
dotnet restore
dotnet build
dotnet run
```

Nếu frontend và backend chạy khác port, cần kiểm tra cấu hình backend base URL trong frontend service/program.

## Tài khoản và dữ liệu

Dự án sử dụng JWT authentication. Người dùng có thể đăng ký tài khoản mới từ giao diện frontend.

Không bịa tài khoản demo mặc định nếu chưa chắc có seed trong database. Khi demo, có thể tạo tài khoản mới bằng chức năng register.

## Các route/tính năng đáng demo

- Chat 1-1
- Group chat
- Snippet Center
- IOC Center
- Pentest Finding Tracker
- Task Center
- Threat Timeline
- Saved Messages
- One-time Secret Sharing
- Session management
- Audit/security flow

## Slash commands

Một số command nổi bật:

```text
/ioc
/vuln
/task
/reminder
```

Các command này giúp tạo nhanh IOC, finding, task hoặc reminder ngay trong chat.

## Lưu ý bảo mật

- Không commit `appsettings.Development.json`.
- Không commit `bin/`, `obj/`, `.vs/`, `publish/`.
- Không commit file upload thật trong `wwwroot/uploads`.
- Không commit Gmail App Password, JWT Secret, DB password thật.
- Nếu secret đã từng bị push lên GitHub, nên rotate lại secret/password.
- Nên dùng placeholder trong `appsettings.json`.
- Nên dùng `appsettings.Development.json`, User Secrets hoặc biến môi trường cho cấu hình local.

## Lưu ý database/migration

- Khi thay đổi model/entity, cần tạo migration mới.
- Khi chỉ sửa UI/service/query không đổi schema database, không cần migration.
- Không tạo index trên `IocIndicators.Value` nếu cột này dùng `varchar(2048)` vì MySQL có thể báo lỗi key quá dài.
- Nếu database thiếu cột sau khi pull code mới, hãy chạy `Update-Database`.

Tạo migration mới khi có thay đổi schema:

```powershell
Add-Migration MigrationName -Project PingMe -StartupProject PingMe -Context AppDbContext
Update-Database -Project PingMe -StartupProject PingMe -Context AppDbContext
```

## Build kiểm tra

Backend:

```bash
dotnet build ./PingMe.csproj
```

Frontend:

```bash
dotnet build ./PingMe.Frontend/PingMe.Frontend.csproj
```

## Roadmap

Một số hướng phát triển tiếp theo:

- Evidence Vault gắn với Pentest Finding
- Report Generator cho finding report
- Dashboard overview cho SOC/Pentest status
- Advanced Global Search
- Attack Notebook
- Nâng cấp notification realtime
- Nâng cấp phân quyền theo workspace/team

## Mô tả ngắn

**PingMe = Dev/SOC/Pentest Collaboration Platform**

Trong đó:

- Chat realtime = collaboration layer
- Snippet Center = Dev workflow
- IOC Center = SOC workflow
- Finding Tracker = Pentest workflow
- Task Center + Threat Timeline = workflow tracking
- Audit/session/security = governance layer
