# 📋 KẾ HOẠCH PHÁT TRIỂN HỆ THỐNG THI ONLINE

## 📊 Phân Tích Hệ Thống

### Kiến trúc tổng thể
```
Frontend (React/Next)
    ↓
ASP.NET API (10 modules)
    ↓
PostgreSQL (25 bảng)
    ↓
MinIO/Local Storage
```

### Độ phức tạp từng phần
| Thành phần | Độ phức tạp | Ước lượng giờ |
|-----------|-------------|--------------|
| Setup & Infrastructure | ⭐ | 20h |
| Auth & User Management | ⭐ | 25h |
| Core Management | ⭐ | 40h |
| Exam Management | ⭐⭐ | 50h |
| Question Management | ⭐⭐ | 45h |
| Exam Player UI | ⭐⭐⭐ | 80h |
| Grading System | ⭐⭐ | 60h |
| Reporting & Statistics | ⭐ | 35h |
| File Storage & OCR | ⭐⭐ | 40h |
| Testing & Deployment | ⭐⭐ | 50h |
| **TỔNG CỘNG** | | **445 giờ** |

---

## 🚀 PHASE 0: SETUP & INFRASTRUCTURE
**Mục đích**: Xây dựng nền tảng, database, project structure
**Ước lượng**: 2-3 tuần
**Kết quả đầu ra**: Dự án hoàn chỉnh cấu trúc, DB, API gateway

### ✅ Task List Phase 0

#### 0.1 Project Setup (3-4 ngày)
- [ ] Tạo ASP.NET 8 project với Clean Architecture
- [ ] Cấu hình folder structure (API, Application, Domain, Infrastructure)
- [ ] Setup Entity Framework Core
- [ ] Cấu hình appsettings.json (dev, staging, prod)
- [ ] Setup Swagger/OpenAPI documentation

#### 0.2 Database Design (5-7 ngày)
- [ ] Tạo PostgreSQL database
- [ ] Tạo 25 bảng theo schema
- [ ] Setup migrations
- [ ] Tạo stored procedures (nếu cần)
- [ ] Tạo indexes & constraints
- [ ] Setup database backup strategy

**Bảng Database (ưu tiên theo thứ tự)**:
```
Group 1 - Auth (2 bảng)
├── users
└── roles_permissions

Group 2 - School Structure (5 bảng)
├── schools
├── subjects
├── classes
├── teachers
└── students

Group 3 - Exam (4 bảng)
├── exams
├── exam_classes
├── exam_settings
└── exam_attempts

Group 4 - Questions (4 bảng)
├── question_types
├── questions
├── question_options
└── question_tags

Group 5 - Answers (4 bảng)
├── answers
├── answer_options
├── answer_canvas
└── autosave_answers

Group 6 - Grading (3 bảng)
├── grading_results
├── grading_annotations
└── grading_comments

Group 7 - Statistics & Storage (3 bảng)
├── exam_statistics
├── files
└── notifications
```

#### 0.3 Authentication & Authorization (4-5 ngày)
- [ ] Implement JWT authentication
- [ ] Setup role-based access control (RBAC)
- [ ] Create User service
- [ ] Implement password hashing (bcrypt)
- [ ] Setup refresh token mechanism
- [ ] Create login/logout endpoints
- [ ] Setup authorization middleware

#### 0.4 Core Infrastructure (3-4 ngày)
- [ ] Repository pattern setup
- [ ] Dependency injection configuration
- [ ] Exception handling middleware
- [ ] Logging system (Serilog)
- [ ] CORS setup
- [ ] API versioning

#### 0.5 Docker & DevOps (3 ngày)
- [ ] Tạo Dockerfile cho ASP.NET
- [ ] Tạo docker-compose.yml (API + PostgreSQL + Redis)
- [ ] Setup environment variables
- [ ] Nginx configuration (reverse proxy)

---

## 🏢 PHASE 1: CORE MANAGEMENT MODULES
**Mục đích**: Quản lý cơ bản người dùng, giáo viên, học sinh, lớp
**Ước lượng**: 3-4 tuần
**Dependency**: Phase 0 ✅

### ✅ Task List Phase 1

#### 1.1 Admin Dashboard Backend (4-5 ngày)
- [ ] Create Admin controller
- [ ] Implement admin authorization middleware
- [ ] Create endpoints for system management
- [ ] Setup audit logging for admin actions

#### 1.2 Teacher Management (5 ngày)
- [ ] GET /api/teachers (list all)
- [ ] POST /api/teachers (create)
- [ ] PUT /api/teachers/{id} (update)
- [ ] DELETE /api/teachers/{id}
- [ ] GET /api/teachers/{id}/classes (assigned classes)
- [ ] GET /api/teachers/{id}/subjects (assigned subjects)
- [ ] Implement validation (unique teacher_code)

**Database**: teachers, class_subject_teachers

#### 1.3 Student Management (5 ngày)
- [ ] GET /api/students (list all)
- [ ] POST /api/students (create)
- [ ] PUT /api/students/{id} (update)
- [ ] DELETE /api/students/{id}
- [ ] GET /api/students/{id}/classes (assigned classes)
- [ ] GET /api/students/{id}/exams (available exams)
- [ ] Implement validation (unique student_code)

**Database**: students, class_students

#### 1.4 Class Management (4-5 ngày)
- [ ] GET /api/classes (list all)
- [ ] POST /api/classes (create)
- [ ] PUT /api/classes/{id} (update)
- [ ] DELETE /api/classes/{id}
- [ ] POST /api/classes/{id}/students (add student)
- [ ] DELETE /api/classes/{id}/students/{id} (remove student)
- [ ] GET /api/classes/{id}/students (list students)

**Database**: classes, class_students

#### 1.5 Subject Management (3 ngày)
- [ ] GET /api/subjects (list all)
- [ ] POST /api/subjects (create)
- [ ] PUT /api/subjects/{id} (update)
- [ ] DELETE /api/subjects/{id}

**Database**: subjects

#### 1.6 Teaching Assignment (4 ngày)
- [ ] POST /api/classes/{id}/assign-teacher
  - Teacher A -> Teach Math to Class 10A1
- [ ] GET /api/classes/{id}/teachers-assignments
- [ ] DELETE /api/assignments/{id}

**Database**: class_subject_teachers

#### 1.7 Import Excel (5 ngày)
- [ ] Implement Excel parser
- [ ] POST /api/teachers/import (batch create teachers)
- [ ] POST /api/students/import (batch create students)
- [ ] Error handling & validation
- [ ] Import history tracking

#### 1.8 Unit Tests (3 ngày)
- [ ] Test all CRUD operations
- [ ] Test authorization
- [ ] Test validation logic

---

## 📝 PHASE 2: EXAM MANAGEMENT
**Mục đích**: Tạo, quản lý, cấu hình bài kiểm tra
**Ước lượng**: 3-4 tuần
**Dependency**: Phase 0, 1 ✅

### ✅ Task List Phase 2

#### 2.1 Exam CRUD (4-5 ngày)
- [ ] POST /api/exams (create exam)
  - Thông tin: title, subject, duration, start_time, end_time
  - Chỉ giáo viên được phân công mới tạo được
- [ ] GET /api/exams (list all)
- [ ] GET /api/exams/{id} (detail)
- [ ] PUT /api/exams/{id} (update)
- [ ] DELETE /api/exams/{id} (only if not started)

**Database**: exams

#### 2.2 Assign Classes to Exam (3 ngày)
- [ ] POST /api/exams/{id}/classes (assign class)
- [ ] GET /api/exams/{id}/classes (list assigned)
- [ ] DELETE /api/exams/{id}/classes/{class_id}

**Database**: exam_classes

#### 2.3 Exam Settings (3 ngày)
- [ ] POST /api/exams/{id}/settings (configure)
  - shuffle_questions: boolean
  - shuffle_answers: boolean
  - show_result_immediately: boolean
  - allow_review: boolean
- [ ] GET /api/exams/{id}/settings
- [ ] PUT /api/exams/{id}/settings

**Database**: exam_settings

#### 2.4 Exam Status Management (3 ngày)
- [ ] Implement exam status flow: DRAFT → ACTIVE → CLOSED
- [ ] POST /api/exams/{id}/activate (publish exam)
- [ ] POST /api/exams/{id}/close (close exam)
- [ ] Validate timing constraints

#### 2.5 Exam Preview (Test) (3-4 ngày)
- [ ] GET /api/exams/{id}/preview (view as A4 format)
- [ ] Export to PDF
- [ ] Print functionality

#### 2.6 Unit Tests (2 ngày)
- [ ] Test exam CRUD
- [ ] Test authorization (only assigned teacher)
- [ ] Test status transitions

---

## ❓ PHASE 3: QUESTION BANK MANAGEMENT
**Mục đích**: Quản lý câu hỏi, các loại câu, tags
**Ước lượng**: 3-4 tuần
**Dependency**: Phase 0, 1, 2 ✅

### ✅ Task List Phase 3

#### 3.1 Question Type Setup (2 ngày)
- [ ] Tạo 5 loại câu hỏi:
  1. MCQ (Multiple Choice)
  2. True/False
  3. Short Answer
  4. Essay
  5. Drawing/Canvas

**Database**: question_types

#### 3.2 Create Questions (5-6 ngày)
- [ ] POST /api/questions (create)
  - Question type, subject, content, difficulty
  - created_by: teacher ID
- [ ] GET /api/questions (list with filters)
  - Filter by: question_type, subject, difficulty, created_by
- [ ] GET /api/questions/{id} (detail)
- [ ] PUT /api/questions/{id} (update)
- [ ] DELETE /api/questions/{id}

**Database**: questions

#### 3.3 Question Options (for MCQ/TrueFalse) (3-4 ngày)
- [ ] POST /api/questions/{id}/options (add option)
  - Ví dụ: { option_label: "A", content: "Hà Nội", is_correct: true }
- [ ] GET /api/questions/{id}/options
- [ ] PUT /api/questions/{id}/options/{option_id}
- [ ] DELETE /api/questions/{id}/options/{option_id}

**Database**: question_options

#### 3.4 Question Tags (3 ngày)
- [ ] POST /api/tags (create tag)
- [ ] POST /api/questions/{id}/tags (assign tag)
- [ ] GET /api/questions?tags=chap1,chap2 (filter by tag)

**Database**: question_tags

#### 3.5 Add Questions to Exam (4-5 ngày)
- [ ] POST /api/exams/{id}/questions (add question)
  - { question_id, order_index, max_score }
- [ ] GET /api/exams/{id}/questions (list questions in exam)
- [ ] PUT /api/exams/{id}/questions/{exam_question_id}
  - Reorder, change max_score
- [ ] DELETE /api/exams/{id}/questions/{exam_question_id}

**Database**: exam_questions

#### 3.6 Question Import from File (5-6 ngày)
- [ ] Parse Word (.docx) → extract questions
- [ ] Parse PDF → extract questions
- [ ] Parse Excel → extract questions
- [ ] Auto-create questions with validation
- [ ] Handle duplicates & conflicts
- [ ] Import preview & confirmation

#### 3.7 Question Bank Search & Filter (3-4 ngày)
- [ ] Full-text search questions
- [ ] Advanced filter: subject, type, difficulty, tags
- [ ] Pagination

#### 3.8 Unit Tests (2 ngày)
- [ ] Test question CRUD
- [ ] Test question options
- [ ] Test exam question ordering

---

## 🎮 PHASE 4: EXAM PLAYER (UI & Interaction)
**Mục đích**: Giao diện làm bài thi (phức tạp nhất)
**Ước lượng**: 4-5 tuần
**Dependency**: Phase 0, 1, 2, 3 ✅
**Độ phức tạp**: ⭐⭐⭐ (High Priority)

### ✅ Task List Phase 4

#### 4.1 Start Exam (3-4 ngày)
- [ ] POST /api/attempts (create exam attempt)
  - Create attempt record
  - Set start_time
  - Return attempt_id
- [ ] GET /api/attempts/{id} (get attempt details)
- [ ] Validate: student enrolled in exam class
- [ ] Validate: exam is active & not finished

**Database**: exam_attempts

#### 4.2 Question Navigation (5-6 ngày)
- [ ] GET /api/attempts/{id}/questions (list all questions with status)
  - Return: question content, type, options
  - Shuffle if exam_settings.shuffle_questions = true
  - Mark answered/unanswered questions
- [ ] Navigation: next, previous, jump to question
- [ ] Mark question as "visited"
- [ ] Display question counter (5/20)

#### 4.3 Answer Multiple Choice (4 ngày)
- [ ] POST /api/attempts/{id}/answers
  ```json
  {
    "question_id": 123,
    "selected_option_ids": [456]
  }
  ```
- [ ] PUT /api/attempts/{id}/answers/{answer_id} (update answer)
- [ ] GET /api/attempts/{id}/answers/{question_id} (current answer)

**Database**: answers, answer_options

#### 4.4 Answer Short Text (3-4 ngày)
- [ ] POST /api/attempts/{id}/answers
  ```json
  {
    "question_id": 123,
    "text_content": "4"
  }
  ```
- [ ] PUT update answer with new text

**Database**: answers

#### 4.5 Answer Essay (3-4 ngày)
- [ ] POST /api/attempts/{id}/answers
  ```json
  {
    "question_id": 123,
    "essay_content": "long text..."
  }
  ```
- [ ] Rich text editor support
- [ ] PUT update essay

**Database**: answers

#### 4.6 Canvas Drawing (5-7 ngày)
- [ ] Implement HTML5 Canvas API
- [ ] Features:
  - Pen tool (draw)
  - Eraser
  - Undo/Redo
  - Color picker
  - Clear canvas
  - Line thickness
- [ ] POST /api/attempts/{id}/answers
  ```json
  {
    "question_id": 123,
    "canvas_image": "data:image/png;...",
    "canvas_json": { strokes: [...] }
  }
  ```

**Database**: answer_canvas

#### 4.7 Auto-save (4-5 ngày)
- [ ] Save answer every 5-10 seconds automatically
- [ ] Show "Saving..." indicator
- [ ] POST /api/attempts/{id}/autosave
  ```json
  {
    "question_id": 123,
    "data": { answer content }
  }
  ```
- [ ] Handle offline save to localStorage
- [ ] Sync when online

**Database**: autosave_answers

#### 4.8 Timer & Countdown (3-4 ngày)
- [ ] Display remaining time
- [ ] Update every second (client-side calculation)
- [ ] Warn when < 5 minutes left
- [ ] Warn when < 1 minute left
- [ ] Show color change (green → yellow → red)

#### 4.9 Submit Exam (3 ngày)
- [ ] POST /api/attempts/{id}/submit
  - Set end_time
  - Auto-calculate scores for auto-graded questions
  - Update exam_attempts.status = "SUBMITTED"
- [ ] Auto-submit when time runs out
- [ ] Prevent re-submission

**Database**: exam_attempts

#### 4.10 Anti-Cheat Monitoring (Basic) (4-5 ngày)
- [ ] Detect tab switching
- [ ] Detect window blur events
- [ ] Detect fullscreen exit
- [ ] Log each violation
- [ ] Display warning message
- [ ] POST /api/attempts/{id}/violations (log violation)

**Database**: violation logs (new table)

#### 4.11 Review Mode (3 ngày)
- [ ] After exam submit, show review if allowed
- [ ] Display: questions, student answers, correct answers
- [ ] Highlight correct/incorrect
- [ ] Show teacher feedback (if graded)

#### 4.12 Frontend Tests (3 ngày)
- [ ] Test question navigation
- [ ] Test auto-save
- [ ] Test timer accuracy
- [ ] Test canvas drawing
- [ ] Test submit functionality

---

## 📊 PHASE 5: GRADING & SCORING
**Mục đích**: Chấm bài tự động và chấm tự luận
**Ước lượng**: 3-4 tuần
**Dependency**: Phase 0, 1, 2, 3, 4 ✅

### ✅ Task List Phase 5

#### 5.1 Auto-grading Algorithm (4-5 ngày)
- [ ] MCQ: compare selected_option with is_correct
- [ ] True/False: exact match
- [ ] Short Answer: exact/fuzzy match (case-insensitive)
  - Implement Levenshtein distance for fuzzy matching
- [ ] Implement scoring function
- [ ] POST /api/grading/auto-grade/{attempt_id}

**Database**: grading_results

#### 5.2 Manual Grading Interface (6-7 ngày)
- [ ] GET /api/attempts?status=SUBMITTED&subject_id=X (list pending)
- [ ] GET /api/attempts/{id}/grading-view
  - Show question + student answer
  - Show rubric/expected answer
- [ ] PUT /api/grading-results (submit grade)
  ```json
  {
    "question_id": 123,
    "attempt_id": 456,
    "score": 8,
    "comment": "...",
    "annotations": [...]
  }
  ```

**Database**: grading_results, grading_comments, grading_annotations

#### 5.3 Drawing/Essay Annotation (5 ngày)
- [ ] Implement image annotation tool (for canvas answers)
- [ ] Draw circle, arrow, line on student's drawing
- [ ] Add text comments
- [ ] Store annotations as JSON
- [ ] Export annotated image

**Database**: grading_annotations

#### 5.4 Grading Submission (2-3 ngày)
- [ ] POST /api/attempts/{id}/mark-as-graded
- [ ] Update exam_attempts.status = "GRADED"
- [ ] Calculate final score

#### 5.5 Score Calculation Rules (3 ngày)
- [ ] Define score calculation:
  - Sum all grading_results.score
  - Normalize to max_score if needed
  - Handle weight (if some questions worth more)

#### 5.6 Final Result Publishing (2 ngày)
- [ ] POST /api/attempts/{id}/publish-result
  - Make grades visible to students
- [ ] Update exam_results table

**Database**: exam_results

#### 5.7 View Results (3 ngày)
- [ ] GET /api/students/{id}/exam-results (view grades)
- [ ] GET /api/attempts/{id}/result-detail
  - Q&A review with scores
  - Teacher comments
  - Annotations

#### 5.8 Unit Tests (2 ngày)
- [ ] Test auto-grade logic
- [ ] Test score calculation
- [ ] Test grading permissions

---

## 📈 PHASE 6: REPORTING & STATISTICS
**Mục đích**: Báo cáo, thống kê, phân tích
**Ước lượng**: 2-3 tuần
**Dependency**: Phase 0, 1, 2, 3, 4, 5 ✅

### ✅ Task List Phase 6

#### 6.1 Exam Statistics (4-5 ngày)
- [ ] POST /api/exams/{id}/calculate-statistics
  - avg_score, max_score, min_score, pass_count, fail_count
- [ ] GET /api/exams/{id}/statistics
- [ ] Store in exam_statistics table

**Database**: exam_statistics

#### 6.2 Class Results Summary (3-4 ngày)
- [ ] GET /api/classes/{id}/exam-results
  - List all exams with class average
  - List all students with scores
- [ ] GET /api/exams/{id}/class-results/{class_id}
  - avg_score by question type
  - Difficulty distribution

**Database**: class_exam_results

#### 6.3 Score Distribution Chart (4 ngày)
- [ ] Calculate histogram: 0-2, 2-4, 4-6, 6-8, 8-10
- [ ] Store distribution data
- [ ] GET /api/exams/{id}/score-distribution

#### 6.4 Student Performance Report (4-5 ngày)
- [ ] GET /api/students/{id}/performance
  - All exams taken
  - Scores over time
  - Strong/weak subjects
- [ ] Comparison: student vs class average
- [ ] Trend analysis

#### 6.5 Teacher Workload Report (3 ngày)
- [ ] GET /api/teachers/{id}/workload
  - Number of exams created
  - Number of pending grades
  - Average grading time
- [ ] GET /api/teachers/{id}/pending-grades

#### 6.6 Export Reports (4-5 ngày)
- [ ] Export to Excel:
  - Student list with scores
  - Score distribution
  - Performance analysis
- [ ] Export to PDF:
  - Exam report
  - Class summary

#### 6.7 System-wide Analytics (3-4 ngày)
- [ ] GET /api/analytics/dashboard
  - Total exams, attempts, students
  - Platform usage stats
  - System health metrics

#### 6.8 Unit Tests (2 ngày)
- [ ] Test statistic calculations
- [ ] Test export functionality

---

## 🔔 PHASE 7: NOTIFICATIONS & LOGGING
**Mục đích**: Thông báo, nhật ký hệ thống
**Ước lượng**: 2 tuần
**Dependency**: Phase 0-6 ✅

### ✅ Task List Phase 7

#### 7.1 Notification System (3-4 ngày)
- [ ] Create notification service
- [ ] Notification types:
  - Exam starts soon (for students)
  - Exam results published (for students)
  - Pending grades to mark (for teachers)
  - Exam created (for teachers)
- [ ] POST /api/notifications
- [ ] GET /api/notifications?read=false (unread)
- [ ] PUT /api/notifications/{id}/mark-as-read
- [ ] Real-time notifications (WebSocket or polling)

**Database**: notifications, notification_users

#### 7.2 Activity Logging (4 ngày)
- [ ] Log all critical actions:
  - User login/logout
  - Exam created/edited/deleted
  - Question created/edited
  - Exam started/submitted
  - Grade published
- [ ] GET /api/logs
- [ ] Filter by: action, user, date range
- [ ] Audit trail for compliance

**Database**: activity_logs, user_login_logs

#### 7.3 System Health Monitoring (3 ngày)
- [ ] Monitor API response times
- [ ] Database connection pool
- [ ] Disk space
- [ ] GET /api/health
- [ ] Alerting mechanism (log warnings)

---

## 📁 PHASE 8: FILE STORAGE & OCR (Optional Enhancement)
**Mục đích**: Quản lý file, OCR import đề
**Ước lượng**: 2-3 tuần
**Dependency**: Phase 0-3 ✅
**Priority**: Medium (can be deferred)

### ✅ Task List Phase 8

#### 8.1 File Upload Service (3-4 ngày)
- [ ] Setup MinIO or local storage
- [ ] POST /api/files/upload
  - Accept: images, documents
  - Validate file type & size (max 10MB)
  - Save to storage, return file_url
- [ ] GET /api/files/{id} (download)
- [ ] DELETE /api/files/{id}

**Database**: files

#### 8.2 Attach Files to Exam (2-3 ngày)
- [ ] POST /api/exams/{id}/files (attach file)
- [ ] GET /api/exams/{id}/files
- [ ] DELETE /api/exams/{id}/files/{file_id}

**Database**: exam_files

#### 8.3 OCR Import (Document Parsing) (5-7 ngày)
- [ ] Use Tesseract or similar library
- [ ] Parse PDF → extract questions
- [ ] Parse image (JPG/PNG) → extract text
- [ ] Auto-detect MCQ format:
  ```
  1. Question?
  A) Answer 1
  B) Answer 2
  C) Answer 3
  D) Answer 4
  ```
- [ ] Create questions with detected options
- [ ] Manual review & correction

---

## ✅ PHASE 9: TESTING & DEPLOYMENT
**Mục đích**: QA, testing, production release
**Ước lượng**: 2-3 tuần

### ✅ Task List Phase 9

#### 9.1 Unit Testing (3-4 ngày)
- [ ] Achieve 80%+ code coverage
- [ ] Test all services & repositories
- [ ] Test business logic
- [ ] Test validators

#### 9.2 Integration Testing (3-4 ngày)
- [ ] Test API endpoints with real database
- [ ] Test complete workflows:
  - Create exam → Add questions → Take exam → Grade exam
- [ ] Test authorization & permissions
- [ ] Performance testing under load (400 students)

#### 9.3 E2E Testing (3-4 ngày)
- [ ] Test full user journeys:
  1. Admin: Create class → Add students → Assign teachers
  2. Teacher: Create exam → Add questions → Grade
  3. Student: View exam → Take exam → See results

#### 9.4 Security Testing (3 ngày)
- [ ] SQL injection tests
- [ ] XSS prevention
- [ ] CSRF protection
- [ ] Authentication/Authorization tests
- [ ] Rate limiting

#### 9.5 Performance Optimization (4 ngày)
- [ ] Database query optimization
- [ ] Add indexes
- [ ] Caching strategy (Redis)
- [ ] API response time < 200ms

#### 9.6 Documentation (3-4 ngày)
- [ ] API documentation (Swagger)
- [ ] Database schema documentation
- [ ] Deployment guide
- [ ] User manual (admin, teacher, student)
- [ ] Development setup guide

#### 9.7 Deployment Setup (4-5 ngày)
- [ ] Docker image building
- [ ] CI/CD pipeline (GitHub Actions / GitLab CI)
- [ ] Database migration automation
- [ ] SSL/TLS certificate
- [ ] Nginx configuration
- [ ] Backup & recovery plan

#### 9.8 UAT & Bug Fixes (5-7 ngày)
- [ ] User acceptance testing
- [ ] Bug fixing & regression testing
- [ ] Performance tuning
- [ ] Final security review

---

## 📊 TIMELINE ESTIMATE

| Phase | Duration | Total Days |
|-------|----------|-----------|
| 0: Setup | 2-3 weeks | 14-21 |
| 1: Core Management | 3-4 weeks | 21-28 |
| 2: Exam Management | 3-4 weeks | 21-28 |
| 3: Question Bank | 3-4 weeks | 21-28 |
| 4: Exam Player | 4-5 weeks | 28-35 |
| 5: Grading | 3-4 weeks | 21-28 |
| 6: Reporting | 2-3 weeks | 14-21 |
| 7: Notifications | 1-2 weeks | 7-14 |
| 8: File Storage | 2-3 weeks | 14-21 |
| 9: Testing & Deploy | 2-3 weeks | 14-21 |
| **TOTAL** | **26-34 weeks** | **175-245 days** |

**Realistically: 4-6 months for 1 developer (assuming 8 hours/day)**

---

## 🎯 CRITICAL PATH (Must-Have)

Minimum viable product (MVP) sequence:
1. ✅ Phase 0 (Setup)
2. ✅ Phase 1 (Core Management)
3. ✅ Phase 2 (Exam Management)
4. ✅ Phase 3 (Question Management)
5. ✅ Phase 4 (Exam Player)
6. ✅ Phase 5 (Grading)
7. ⚠️ Phase 6 (Reporting - basic version)
8. 🔵 Phase 7-8 (Nice to have)

**MVP Timeline: 3-4 months**

---

## 🎨 Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET 8 (.NET 8) |
| **Database** | PostgreSQL 15+ |
| **ORM** | Entity Framework Core 8 |
| **Frontend** | React 18 / Next.js 14 |
| **Storage** | MinIO / S3 / Local |
| **Cache** | Redis |
| **Real-time** | WebSocket / SignalR |
| **Logging** | Serilog |
| **Testing** | xUnit / Moq |
| **Container** | Docker |
| **CI/CD** | GitHub Actions / GitLab CI |
| **Reverse Proxy** | Nginx |

---

## 📝 Notes

- **Start with Phase 0** to establish solid foundation
- **Phase 4 (Exam Player)** is most complex - allocate more time
- **Test after each phase** to catch early issues
- **Database design is critical** - validate schema carefully before coding
- **Consider MVP approach** - launch with basic features first
- **Monitor performance** from early stages

