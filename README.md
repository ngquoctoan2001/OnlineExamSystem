# 🎓 Online Exam System - Hệ thống thi online

## 📌 Tổng quan

Hệ thống thi online được xây dựng dành cho các trường học, cho phép:

- **Giáo viên**: Tạo đề thi, quản lý câu hỏi, chấm bài tự động/thủ công
- **Học sinh**: Làm bài thi, xem kết quả, review câu trả lời
- **Quản trị viên**: Quản lý toàn hệ thống, người dùng, trường học

## 🏗️ Kiến trúc

```
Frontend (React/Next.js)
        ↓
ASP.NET Core 10 Web API
        ↓
PostgreSQL Database
        ↓
File Storage (MinIO/Local)
```

### Clean Architecture Pattern
```
Domain Layer          → Entities, Enums, Interfaces
Application Layer    → Use Cases, Services, DTOs
Infrastructure Layer → Database, File Storage, External Services
API Layer           → Controllers, Middleware, Configuration
```

## 🗂️ Cấu trúc Thư mục

```
OnlineExamSystem/
├── src/
│   ├── OnlineExamSystem.Domain/        # Entities & Business Rules
│   │   ├── Entities/                   # Domain Models
│   │   └── Enums/
│   │
│   ├── OnlineExamSystem.Application/   # Use Cases & Services
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   ├── Interfaces/                 # Service Contracts
│   │   ├── Services/                   # Business Logic
│   │   └── Validators/
│   │
│   ├── OnlineExamSystem.Infrastructure # Data Access, External Services
│   │   ├── Data/                       # DbContext, Migrations
│   │   ├── Repositories/               # Repository Pattern
│   │   └── Services/                   # Email, File Storage, etc
│   │
│   └── OnlineExamSystem.API            # REST API Gateway
│       ├── Controllers/                # API Endpoints
│       ├── Middleware/                 # Custom Middleware
│       ├── Extensions/                 # Service Extensions
│       └── Program.cs                  # Configuration
│
├── tests/
│   ├── OnlineExamSystem.Tests.Unit/   # Unit Tests
│   └── OnlineExamSystem.Tests.Integration/
│
├── docs/
│   ├── DATABASE_SCHEMA.md
│   ├── API_DOCUMENTATION.md
│   └── USER_GUIDE.md
│
├── docker-compose.yml                 # Docker Services
├── SETUP.md                           # Setup Guide
└── README.md
```

## 📊 Database Schema

Tổng cộng **25 Bảng** được phân chia thành 7 nhóm:

### Group 1: Auth (7 bảng)
- `users` - Người dùng
- `user_sessions` - Phiên đăng nhập
- `user_login_logs` - Lịch sử đăng nhập
- `roles` - Vai trò (Admin, Teacher, Student)
- `permissions` - Quyền hạn
- `role_permissions` - Vai trò-Quyền
- `user_roles` - Người dùng-Vai trò

### Group 2: School Structure (5 bảng)
- `schools` - Trường học
- `subjects` - Môn học
- `classes` - Lớp học
- `teachers` - Giáo viên
- `students` - Học sinh

### Group 3: Exam Management (5 bảng)
- `exams` - Đề thi
- `exam_classes` - Đề thi-Lớp học
- `exam_settings` - Cấu hình đề thi
- `exam_attempts` - Lần làm bài
- `exam_questions` - Câu hỏi trong đề thi

### Group 4: Questions (4 bảng)
- `question_types` - Loại câu hỏi
- `questions` - Câu hỏi
- `question_options` - Tùy chọn câu hỏi
- `question_tags` - Gắn thẻ câu hỏi

### Group 5: Answers (4 bảng)
- `answers` - Câu trả lời
- `answer_options` - Tùy chọn đã chọn
- `answer_canvas` - Vẽ hình trả lời
- `autosave_answers` - Lưu tạm

### Group 6: Grading (3 bảng)
- `grading_results` - Kết quả chấm
- `grading_annotations` - Ghi chú chấm
- `grading_comments` - Bình luận chấm

### Group 7: Statistics & Storage (3 bảng)
- `exam_statistics` - Thống kê đề thi
- `files` - Lưu trữ tệp
- `notifications` - Thông báo

## 🚀 Getting Started

### 1. Prerequisites
- .NET 10 SDK
- PostgreSQL 14+ (hoặc Docker)
- Node.js 18+ (cho frontend)

### 2. Database Setup
```bash
# Start Docker containers
cd OnlineExamSystem
docker-compose up -d

# Or use local PostgreSQL
# Connection string: Host=localhost;Port=5432;Database=onlineexam_dev;Username=postgres;Password=postgres;
```

### 3. Application Setup
```bash
cd OnlineExamSystem

# Restore packages
dotnet restore

# Apply migrations
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API

# Run application
cd ../OnlineExamSystem.API
dotnet run
```

### 4. API Available at
- Swagger: http://localhost:5000/swagger
- Health Check: http://localhost:5000/api/health/status

## 📦 Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core 10 |
| **Database** | PostgreSQL 16 |
| **ORM** | Entity Framework Core 8 |
| **Frontend** | React / Next.js |
| **Authentication** | JWT (JSON Web Token) |
| **File Storage** | MinIO / Local Storage |
| **Caching** | Redis |
| **Logging** | Serilog |
| **API Docs** | Swagger/OpenAPI |

## ✨ Features

### Phase 1: Core Infrastructure
- ✅ Clean Architecture Setup
- ✅ Database Design & Schema
- ✅ Entity Framework Configuration
- ✅ API Gateway & Swagger Documentation

### Phase 2: Authentication & Authorization
- [ ] JWT Authentication
- [ ] Role-Based Access Control (RBAC)
- [ ] User Management

### Phase 3: User Management
- [ ] User CRUD
- [ ] Teacher Management
- [ ] Student Management
- [ ] Class Management

### Phase 4: Exam Management
- [ ] Create/Edit/Delete Exams
- [ ] Exam Settings
- [ ] Assign Exams to Classes
- [ ] Exam Scheduling

### Phase 5: Question Management
- [ ] Multiple Choice Questions
- [ ] True/False Questions
- [ ] Essay Questions
- [ ] Question Bank
- [ ] Question Search & Filter

### Phase 6: Exam Taking
- [ ] Exam Player UI
- [ ] Answer Submission
- [ ] Auto-Save
- [ ] Time Management
- [ ] Canvas Drawing Support

### Phase 7: Grading System
- [ ] Auto Grading (Multiple Choice)
- [ ] Manual Grading (Essay)
- [ ] Annotation & Comments
- [ ] Grade Review

### Phase 8: Reporting & Statistics
- [ ] Grade Reports
- [ ] Student Performance
- [ ] Exam Statistics
- [ ] Class Performance

### Phase 9: File Management
- [ ] File Upload
- [ ] OCR Support
- [ ] Document Management

## 📝 Default Accounts (Development)

```
Admin Account:
- Username: admin
- Password: Admin123!@

Teacher Account:
- Username: teacher1
- Password: Teacher123!@

Student Account:
- Username: student1
- Password: Student123!@
```

## 🔒 Security

- JWT Token-based Authentication
- Refresh Token Support
- Password Hashing (bcrypt)
- CORS Configuration
- HTTPS Support
- SQL Injection Prevention (EF Core Parameterization)
- CSRF Protection (Token)

## 📚 Documentation

- [Setup Guide](./SETUP.md)
- [Database Schema Documentation](./docs/DATABASE_SCHEMA.md)
- [API Documentation](./docs/API_DOCUMENTATION.md)
- [User Guide](./docs/USER_GUIDE.md)

## 🧪 Testing
```bash
# Unit Tests
dotnet test tests/OnlineExamSystem.Tests.Unit

# Integration Tests
dotnet test tests/OnlineExamSystem.Tests.Integration
```

## 🐛 Troubleshooting

### Database Connection Error
- Ensure PostgreSQL is running
- Check connection string in `appsettings.json`
- Verify database exists

### Migration Issues
```bash
# List migrations
dotnet ef migrations list --startup-project src/OnlineExamSystem.API

# Remove last migration
dotnet ef migrations remove --force --startup-project src/OnlineExamSystem.API
```

## 📈 Development Workflow

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/feature-name
   ```

2. **Implement Feature**
   - Follow SOLID principles
   - Add unit tests
   - Update documentation

3. **commit và Push**
   ```bash
   git add .
   git commit -m "feat: description"
   git push origin feature/feature-name
   ```

4. **Create Pull Request**
   - Describe changes
   - Link to issues
   - Request review

## 🎯 Performance Considerations

- Database indexing on frequently queried columns
- Query optimization using Entity Framework
- Redis caching for frequently accessed data
- Pagination for large datasets
- Async/await throughout the application

## 🔄 CI/CD Pipeline

- GitHub Actions for automated testing
- Docker containerization
- Automated deployment scripts

## 📞 Support & Contact

For issues or questions:
- Create an issue on GitHub
- Contact: projects@example.com

## 📄 License

MIT License - See LICENSE file for details

## 👥 Contributors

- Development Team
- Project Manager
- Quality Assurance Team

---

**Last Updated**: March 2026
**Version**: 1.0.0
