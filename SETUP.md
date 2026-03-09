# Online Exam System - Backend Setup Guide

## 📋 Database Setup

### Prerequisites
- PostgreSQL 14+ hoặc Docker
- .NET 10 SDK
- Visual Studio Code / Visual Studio 2022

### Option 1: Using Docker (Recommended)
```bash
cd OnlineExamSystem
docker-compose up -d
```

This will start:
- PostgreSQL database (port 5432)
- pgAdmin (port 8080) - Database management
- Redis (port 6379) - Caching
- MinIO (port 9000) - File storage

### Option 2: Local PostgreSQL
1. Install PostgreSQL
2. Create database:
```sql
CREATE DATABASE onlineexam_dev;
```
3. Configure connection string in appsettings.json

## 🔧 Application Setup

### 1. Restore NuGet packages
```bash
cd OnlineExamSystem
dotnet restore
```

### 2. Create Database Migrations
```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../OnlineExamSystem.API
```

### 3. Apply Database Migrations
```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API
```

### 4. Run the Application
```bash
cd src/OnlineExamSystem.API
dotnet run
```

Application runs on: http://localhost:5000 (HTTPS) or http://localhost:5001

## 📚 API Documentation
Once the application runs, visit:
- Swagger UI: http://localhost:5000/swagger

## 🗄️ Database Schema
```
USERS & AUTH (7 tables)
├── users
├── user_sessions
├── user_login_logs
├── permissions
├── roles
├── role_permissions
└── user_roles

SCHOOL STRUCTURE (5 tables)
├── schools
├── subjects
├── classes
├── teachers
└── students

EXAM MANAGEMENT (5 tables)
├── exams
├── exam_classes
├── exam_settings
├── exam_attempts
└── exam_questions

QUESTIONS (4 tables)
├── question_types
├── questions
├── question_options
└── question_tags

ANSWERS (4 tables)
├── answers
├── answer_options
├── answer_canvas
└── autosave_answers

GRADING (3 tables)
├── grading_results
├── grading_annotations
└── grading_comments

STATISTICS & STORAGE (3 tables)
├── exam_statistics
├── files
└── notifications
```

## 🔐 Default Credentials (Development Only)
```
Admin:
- Username: admin
- Password: Admin123!@

Teacher:
- Username: teacher1
- Password: Teacher123!@

Student:
- Username: student1
- Password: Student123!@
```

## 📦 Project Structure
```
OnlineExamSystem/
├── src/
│   ├── OnlineExamSystem.Domain/        # Entities & Business Rules
│   ├── OnlineExamSystem.Application/   # Use Cases & Services
│   ├── OnlineExamSystem.Infrastructure # EF Core, Repositories
│   └── OnlineExamSystem.API            # Controllers & Gateway
├── tests/
├── docs/
└── docker-compose.yml
```

## 🚀 Next Steps
1. Implement Authentication Module (JWT)
2. Create Auth Controllers (Login, Register)
3. Implement User Management Services
4. Create Teacher Module
5. Create Student Module
6. Implement Exam Management
7. Implement Question & Answer Logic
8. Create Grading System

## 📞 Support
For issues or questions, contact the development team.
