# 🎓 Online Exam System — Hệ Thống Thi Online

Hệ thống thi trực tuyến dành cho trường học, hỗ trợ tạo đề thi, làm bài, chấm điểm tự động/thủ công với phân quyền theo vai trò.

| Vai trò | Chức năng chính |
|---------|----------------|
| **Quản trị viên** | Quản lý người dùng, trường học, phân quyền, theo dõi hệ thống |
| **Giáo viên** | Tạo đề thi, quản lý câu hỏi, chấm bài, xem thống kê |
| **Học sinh** | Làm bài thi, xem kết quả, review câu trả lời |

---

## 🏗️ Kiến trúc hệ thống

```
┌─────────────────────────────────────────────┐
│  Frontend (React 18 + TypeScript + Vite)    │  :3000
└──────────────────┬──────────────────────────┘
                   │ /api (proxy)
┌──────────────────▼──────────────────────────┐
│  ASP.NET Core 10 Web API                    │  :5000
│  ├── Controllers (28)                       │
│  ├── Application Layer (Services, DTOs)     │
│  ├── Infrastructure Layer (EF Core, Repos)  │
│  └── Domain Layer (Entities)                │
└──────┬───────────┬──────────────┬───────────┘
       │           │              │
  ┌────▼───┐  ┌───▼────┐  ┌─────▼─────┐
  │ Postgres│  │ Redis  │  │ MinIO/    │
  │   16   │  │   7    │  │ Local FS  │
  │  :5433 │  │  :6379 │  │           │
  └────────┘  └────────┘  └───────────┘
```

### Clean Architecture (4 layers)

| Layer | Project | Vai trò |
|-------|---------|---------|
| **Domain** | `OnlineExamSystem.Domain` | Entities, business rules |
| **Application** | `OnlineExamSystem.Application` | Services, DTOs, interfaces |
| **Infrastructure** | `OnlineExamSystem.Infrastructure` | EF Core, repositories, external services |
| **API** | `OnlineExamSystem.API` | Controllers, middleware, configuration |

---

## 🗂️ Cấu trúc dự án

```
OnlineExamSystem/
├── src/
│   ├── OnlineExamSystem.Domain/            # 31 entities (AllEntities.cs)
│   ├── OnlineExamSystem.Application/
│   │   ├── DTOs/                           # Data Transfer Objects
│   │   ├── Interfaces/                     # Service contracts
│   │   ├── Services/                       # 38 service files
│   │   └── Repositories/
│   ├── OnlineExamSystem.Infrastructure/
│   │   ├── Data/                           # ApplicationDbContext + Migrations
│   │   ├── Repositories/                   # 23 repository files
│   │   └── Services/
│   └── OnlineExamSystem.API/
│       ├── Controllers/                    # 28 controllers
│       ├── DTOs/
│       └── Program.cs
├── frontend/
│   └── src/
│       ├── pages/                          # 26 page components
│       ├── api/                            # 25 API client modules
│       ├── components/                     # Header, Layout, Sidebar
│       ├── contexts/                       # AuthContext
│       └── types/
├── tests/
│   └── OnlineExamSystem.Tests/             # Phase 2-9 test suites
├── docs/
│   └── DATABASE_SCHEMA.md
├── docker-compose.yml
└── OnlineExamSystem.sln
```

---

## 📊 Database Schema — 31 bảng, 7 nhóm

### 1. Authentication & User Management (7 bảng)

| Bảng | Mô tả |
|------|--------|
| `users` | Tài khoản người dùng (email, password hash) |
| `roles` | Vai trò: Admin, Teacher, Student |
| `permissions` | Định nghĩa quyền hạn |
| `user_roles` | Gán vai trò cho người dùng |
| `role_permissions` | Gán quyền cho vai trò |
| `user_sessions` | JWT refresh tokens |
| `user_login_logs` | Lịch sử đăng nhập (IP tracking) |

### 2. School Structure (5 bảng)

| Bảng | Mô tả |
|------|--------|
| `schools` | Thông tin trường học |
| `subjects` | Môn học (Toán, Lý, Hóa, ...) |
| `classes` | Lớp học (10A1, 10A2, ...) |
| `teachers` | Hồ sơ giáo viên |
| `students` | Hồ sơ học sinh |

### 3. Exam Management (5 bảng)

| Bảng | Mô tả |
|------|--------|
| `exams` | Đề thi (tiêu đề, thời gian, trạng thái) |
| `exam_classes` | Gán đề thi cho lớp |
| `exam_settings` | Cấu hình (xáo trộn, hiển thị kết quả, ...) |
| `exam_attempts` | Lần làm bài của học sinh |
| `exam_questions` | Câu hỏi trong đề thi (điểm, thứ tự) |

### 4. Question Management (4 bảng)

| Bảng | Mô tả |
|------|--------|
| `question_types` | Loại: MCQ, True/False, Short Answer, Essay, Drawing |
| `questions` | Ngân hàng câu hỏi |
| `question_options` | Đáp án cho câu hỏi trắc nghiệm |
| `question_tags` | Gắn thẻ phân loại câu hỏi |

### 5. Answers (4 bảng)

| Bảng | Mô tả |
|------|--------|
| `answers` | Câu trả lời của học sinh |
| `answer_options` | Đáp án đã chọn (MCQ) |
| `answer_canvas` | Bài vẽ (canvas drawing) |
| `autosave_answers` | Lưu tạm tự động |

### 6. Grading (3 bảng)

| Bảng | Mô tả |
|------|--------|
| `grading_results` | Kết quả chấm điểm |
| `grading_annotations` | Ghi chú chấm bài |
| `grading_comments` | Bình luận của giáo viên |

### 7. Statistics, Storage & System (3+ bảng)

| Bảng | Mô tả |
|------|--------|
| `exam_statistics` | Thống kê đề thi |
| `files` | Metadata file upload |
| `notifications` | Thông báo hệ thống |
| `activity_logs` | Nhật ký hoạt động |
| `exam_violations` | Vi phạm trong kỳ thi |
| `subject_exam_types` | Loại bài thi theo môn |
| `teaching_assignments` | Phân công giảng dạy |
| `class_students` | Học sinh thuộc lớp |

---

## 🔌 API Endpoints

### Authentication

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| `POST` | `/api/auth/register` | Đăng ký tài khoản |
| `POST` | `/api/auth/login` | Đăng nhập |
| `POST` | `/api/auth/refresh-token` | Refresh access token |
| `POST` | `/api/auth/logout` | Đăng xuất |
| `GET` | `/api/auth/me` | Lấy thông tin user hiện tại |

### Exam Management

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| `GET` | `/api/exams` | Danh sách đề thi (phân trang) |
| `POST` | `/api/exams` | Tạo đề thi mới |
| `GET` | `/api/exams/{id}` | Chi tiết đề thi |
| `PUT` | `/api/exams/{id}` | Cập nhật đề thi |
| `DELETE` | `/api/exams/{id}` | Xóa đề thi (chỉ DRAFT) |
| `POST` | `/api/exams/{id}/activate` | Kích hoạt đề thi |
| `POST` | `/api/exams/{id}/close` | Đóng đề thi |
| `POST` | `/api/exams/{id}/classes` | Gán lớp cho đề thi |
| `GET` | `/api/exams/{id}/settings` | Lấy cấu hình đề thi |
| `POST` | `/api/exams/{id}/settings` | Cấu hình đề thi |
| `POST` | `/api/exams/{id}/questions` | Thêm câu hỏi vào đề |
| `GET` | `/api/exams/{id}/questions` | Danh sách câu hỏi đề thi |

### Các nhóm API khác

| Controller | Mô tả |
|------------|--------|
| `TeachersController` | CRUD giáo viên, danh sách lớp phụ trách |
| `StudentsController` | CRUD học sinh, danh sách đề thi |
| `ClassesController` | CRUD lớp học |
| `SubjectsController` | CRUD môn học |
| `UsersController` | Quản lý tài khoản |
| `AdminController` | Chức năng quản trị |
| `QuestionsController` | Ngân hàng câu hỏi |
| `TagsController` | Quản lý thẻ câu hỏi |
| `AnswersController` | Câu trả lời |
| `AutosaveController` | Lưu tạm tự động |
| `GradingController` | Chấm điểm |
| `ScoresController` | Điểm số |
| `GradebookController` | Sổ điểm |
| `StatisticsController` | Thống kê |
| `NotificationsController` | Thông báo |
| `ActivityLogsController` | Nhật ký hoạt động |
| `FilesController` | Quản lý file |
| `UploadController` | Upload file |
| `HealthController` | Health check |

---

## 📦 Technology Stack

| Thành phần | Công nghệ | Phiên bản |
|------------|-----------|-----------|
| **Backend Framework** | ASP.NET Core | 10.0 |
| **ORM** | Entity Framework Core | 10.0 |
| **Database** | PostgreSQL | 16 |
| **Caching** | Redis | 7 |
| **Authentication** | JWT Bearer (HMAC-SHA256) | — |
| **Password Hashing** | BCrypt.Net | Work factor 12 |
| **File Storage** | MinIO / Local Storage | — |
| **API Docs** | Swagger / OpenAPI | — |
| **Frontend** | React + TypeScript | 18.2 / 5.2 |
| **Build Tool** | Vite | 5.0 |
| **HTTP Client** | Axios | 1.6 |
| **Routing** | React Router DOM | 6.20 |
| **Charts** | Recharts | 2.10 |
| **Containerization** | Docker + Docker Compose | — |
| **DB Admin UI** | Adminer | — |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/) (khuyến nghị)
- Hoặc PostgreSQL 14+ cài local

### 1. Khởi chạy infrastructure (Docker)

```bash
cd OnlineExamSystem
docker-compose up -d
```

Dịch vụ sẽ chạy:

| Service | URL / Port |
|---------|------------|
| PostgreSQL | `localhost:5433` |
| Redis | `localhost:6379` |
| Adminer (DB UI) | http://localhost:8080 |

### 2. Chạy Backend

```bash
# Restore packages
dotnet restore

# Apply database migrations
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API

# Chạy API
cd ../OnlineExamSystem.API
dotnet run
```

API sẽ chạy tại:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/health/status

### 3. Chạy Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend chạy tại http://localhost:3000 — tự động proxy `/api` → `http://localhost:5000`.

### 4. Chạy tất cả bằng Docker

```bash
docker-compose up -d  # Bao gồm cả API container
```

---

## 📝 Tài khoản mặc định (Development)

| Vai trò | Username | Password |
|---------|----------|----------|
| Admin | `admin` | `Admin123!@` |
| Giáo viên | `teacher1` | `Teacher123!@` |
| Học sinh | `student1` | `Student123!@` |

---

## ✨ Tiến độ phát triển

### Phase 0: Setup & Infrastructure ✅
- ✅ Clean Architecture project structure
- ✅ Entity Framework Core + PostgreSQL (31 entities)
- ✅ Database migrations
- ✅ Swagger/OpenAPI documentation
- ✅ CORS & Security middleware
- ✅ Dependency injection
- ✅ Docker & Docker Compose

### Phase 1: Authentication & User Management ✅
- ✅ JWT Bearer token authentication (HMAC-SHA256)
- ✅ BCrypt password hashing (work factor 12)
- ✅ Refresh token management
- ✅ Login/Logout/Register endpoints
- ✅ Role-based access control (RBAC)
- ✅ Login audit logging (IP tracking)
- ✅ User session tracking

### Phase 2: Exam Management ✅
- ✅ Exam CRUD operations
- ✅ Exam status flow: DRAFT → ACTIVE → CLOSED
- ✅ Assign classes to exams
- ✅ Exam settings (shuffle, show_result, ...)
- ✅ Question linking with scoring & ordering

### Phase 3: Question Bank Management 🔄
- 🟡 Question types (MCQ, True/False, Short Answer, Essay, Drawing)
- 🟡 Question CRUD
- 🟡 Question options management
- 🟡 Tags & categorization
- 🟡 Import questions

### Phase 4: Exam Taking ⏳
- ⬜ Exam Player UI
- ⬜ Answer submission
- ⬜ Auto-save
- ⬜ Time management
- ⬜ Canvas drawing support

### Phase 5: Grading System ⏳
- ⬜ Auto grading (trắc nghiệm)
- ⬜ Manual grading (tự luận)
- ⬜ Annotations & comments
- ⬜ Grade review

### Phase 6: Student & Teacher Management ⏳
- 🟡 Teacher CRUD (endpoints built)
- 🟡 Student CRUD (endpoints built)
- 🟡 Class management (endpoints built)
- 🟡 Subject management (endpoints built)

### Phase 7: Reporting & Statistics ⏳
- ⬜ Grade reports
- ⬜ Student performance analytics
- ⬜ Exam statistics
- ⬜ Class performance dashboards

### Phase 8: Notifications & Activity ⏳
- 🟡 Notification system (endpoints built)
- 🟡 Activity logging (endpoints built)

### Phase 9: File Management ⏳
- ⬜ File upload (MinIO/Local)
- ⬜ OCR support
- ⬜ Document management

> **Legend**: ✅ Hoàn thành | 🟡 Có code, chưa test đầy đủ | ⬜ Chưa triển khai

---

## 🔒 Bảo mật

| Tính năng | Chi tiết |
|-----------|----------|
| Authentication | JWT Bearer token (HMAC-SHA256, 60 phút hết hạn) |
| Password | BCrypt hashing, work factor 12 |
| Session | Refresh token rotation |
| Rate Limiting | 10 req/phút (auth), 200 req/phút (API) |
| CORS | Cấu hình cho frontend origin |
| HTTPS | Redirect middleware enabled |
| SQL Injection | Entity Framework Core parameterization |
| Audit | Login logs với IP tracking |
| Authorization | Role-based attributes trên controllers |

---

## 🧪 Testing

```bash
# Chạy tất cả tests
dotnet test

# Chạy test cho phase cụ thể
dotnet test tests/OnlineExamSystem.Tests
```

Cấu trúc test theo phase:
```
tests/OnlineExamSystem.Tests/
├── Phase2/      # Exam Management tests
├── Phase3/      # Question tests
├── Phase4/      # Exam Taking tests
├── Phase5/      # Grading tests
├── Phase6/      # Management tests
├── Phase7/      # Statistics tests
├── Phase9/      # File Management tests
└── Integration/ # Integration tests
```

---

## ⚙️ Cấu hình

### Connection String (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=onlineexam_dev;Username=postgres;Password=postgres"
  }
}
```

### JWT Settings

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-min-32-characters-long-for-hs256!",
    "ExpirationMinutes": 60,
    "Issuer": "onlineexam.local",
    "Audience": "onlineexam.users"
  }
}
```

### File Storage

```json
{
  "FileStorage": {
    "LocalPath": "wwwroot/uploads",
    "MinioEndpoint": "localhost:9000",
    "MinioAccessKey": "minioadmin",
    "MinioSecretKey": "minioadmin"
  }
}
```

### Docker Services (`docker-compose.yml`)

| Service | Image | Port | Mô tả |
|---------|-------|------|--------|
| `postgres` | `postgres:16-alpine` | 5433 → 5432 | Database chính |
| `redis` | `redis:7-alpine` | 6379 | Caching |
| `adminer` | `adminer` | 8080 | Database admin UI |
| `api` | Custom build | 5000 → 8080 | ASP.NET Core API |

---

## 🐛 Troubleshooting

### Lỗi kết nối database
```bash
# Kiểm tra PostgreSQL đang chạy
docker ps | Select-String postgres

# Kiểm tra kết nối
docker exec -it onlineexam_db psql -U postgres -d onlineexam_dev -c "SELECT 1"
```

### Lỗi migration
```bash
# Liệt kê migrations
dotnet ef migrations list --startup-project src/OnlineExamSystem.API

# Apply lại
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API

# Xóa migration cuối
dotnet ef migrations remove --force --startup-project ../OnlineExamSystem.API
```

### Lỗi frontend không kết nối API
- Đảm bảo API đang chạy tại `http://localhost:5000`
- Kiểm tra proxy config trong `frontend/vite.config.ts`
- Kiểm tra CORS settings trong `Program.cs`

### Reset toàn bộ dữ liệu
```bash
docker-compose down -v   # Xóa volumes
docker-compose up -d     # Tạo lại từ đầu
# Sau đó apply migrations lại
```

---

## 📈 Development Workflow

```bash
# 1. Tạo feature branch
git checkout -b feature/ten-tinh-nang

# 2. Code & test
dotnet build
dotnet test

# 3. Commit
git add .
git commit -m "feat: mô tả thay đổi"

# 4. Push & tạo Pull Request
git push origin feature/ten-tinh-nang
```

### Coding Conventions
- Clean Architecture — không tham chiếu ngược layer
- Async/await xuyên suốt
- Repository pattern cho data access
- DTOs cho API request/response
- Pagination cho danh sách lớn

---

## 📚 Tài liệu

| Tài liệu | Đường dẫn |
|-----------|-----------|
| Hướng dẫn cài đặt | [SETUP.md](./SETUP.md) |
| Database Schema | [docs/DATABASE_SCHEMA.md](./docs/DATABASE_SCHEMA.md) |
| API Specification | [api-spec.md](./api-spec.md) |
| Feature Specification | [exam-system-features.md](./exam-system-features.md) |
| System Specification | [specification.md](./specification.md) |
| Phase Plan | [PHASE_PLAN.md](./PHASE_PLAN.md) |

---

## 📄 License

MIT License

---

**Phiên bản**: 1.0.0  
**Cập nhật lần cuối**: Tháng 3, 2026
