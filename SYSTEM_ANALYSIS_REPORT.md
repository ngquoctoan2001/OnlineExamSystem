# 📊 BÁNG CÁO PHÂN TÍCH VÀ ĐẶC TẢ LẠI HỆ THỐNG THI ONLINE

**Ngày phân tích**: 13 tháng 3, 2026  
**Người phân tích**: AI System Analysis  
**Trạng thái**: Sẵn sàng cho Production ❌ (Cần sửa lỗi trước)

---

## 📋 PHẦN 1: TỔNG QUAN & TRẠNG THÁI HIỆN TẠI

### 1.1 Kiến Trúc Hệ Thống

```
┌─────────────────────────────────────────┐
│  Frontend React 18 + TypeScript + Vite  │  :3000
│  - 26 page components                   │
│  - 25 API client modules                │
└──────────────────┬──────────────────────┘
                   │ HTTP REST API
┌──────────────────▼──────────────────────┐
│  Backend ASP.NET Core 10 (Clean Arch)   │  :5000
│  - 28 Controllers                       │
│  - 38 Service classes                   │
│  - 23 Repository classes                │
│  - 31 Domain entities                   │
└──────┬───────────┬──────────────┬───────┘
       │           │              │
  ┌────▼───┐  ┌───▼────┐  ┌─────▼─────┐
  │PostgreSQL│ │ Redis  │  │ MinIO/    │
  │  16     │ │  7    │  │ Local FS  │
  │(31 TBL)│ │ (Cache)│  │(File Store)
  └────────┘ └────────┘  └───────────┘
```

### 1.2 Độ Hoàn Thành Tính Năng

| Tính Năng | Trạng Thái | Ghi Chú |
|-----------|-----------|---------|
| **Đăng nhập/Đăng ký** | ✅ Đầy đủ | JWT token, refresh token, audit log |
| **Quản lý người dùng** | ✅ 80% | CRUD hoạt động, RBAC cấu trúc có nhưng chưa enforce |
| **Quản lý lớp/môn học** | ✅ 90% | CRUD đầy đủ, ít validation |
| **Tạo đề thi** | ✅ 85% | CRUD hoạt động, import/export chưa xong |
| **Quản lý câu hỏi** | ✅ 80% | 7 loại câu hỏi support, bank quản lý |
| **Làm bài thi (Exam Player)** | ✅ 90% | UI đầy đủ, auto-save hoạt động, logic thời gian chưa |
| **Chấm điểm tự động** | ✅ 85% | MCQ/True-False hoạt động, essay/vẽ chưa |
| **Chấm điểm thủ công** | ✅ 80% | Giao diện chi tiết, logic ghi gốc không atomic |
| **Thống kê/Báo cáo** | ✅ 75% | Schema có, backend API chưa đầy đủ |
| **Xử lý vi phạm thi** | 🟡 20% | Schema có, frontend chưa integrate |
| **Chính sách thi lại** | ❌ 0% | Không có support |

---

## 🔴 PHẦN 2: CÁC LỖI LOGIC & SECURITY NGHIÊM TRỌNG

### 2.1 LỖI LOẠI 1: Không Enforce Quyền Hạn (CRITICAL)

**Vị trí**: Tất cả 28 controllers  
**Nguy Hiểm**: Rất cao  
**Tác Động**: Bất kỳ user đăng nhập nào cũng có thể làm việc của admin/giáo viên

#### Problem Code:
```csharp
// ❌ GradingController.cs
[Authorize]  // Chỉ check đăng nhập, không check vai trò!
[HttpPost("auto-grade/{attemptId}")]
public async Task AutoGrade(long attemptId)
{
    // Sinh viên có thể gọi endpoint này!
    var result = await _gradingService.AutoGradeAttemptAsync(attemptId);
}
```

#### Tình Huống Thực Tế Lớp Học:
```
Sinh viên Ngân (ID=101) gọi:
POST /api/grading/auto-grade/500

Hệ thống sẽ:
1. Check: Ngân đăng nhập? ✓ (đã đăng nhập)
2. Auto-grade bài kiểm tra → Score = 10/10
3. Ngân tself-assign score cao nhất!

Giáo viên không biết vì không có audit log
```

#### Giải Pháp:
```csharp
[Authorize(Roles = "TEACHER,ADMIN")]
[HttpPost("auto-grade/{attemptId}")]
public async Task AutoGrade(long attemptId)
{
    var userId = long.Parse(User.FindFirst("userId")?.Value ?? "0");
    var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
    var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
    
    // Kiểm tra: User có phải giáo viên của bài thi này không?
    if (exam.TeacherId != userId && !UserIsAdmin())
        return Forbid();
    
    return Ok(await _gradingService.AutoGradeAttemptAsync(attemptId));
}
```

**Scope ảnh hưởng**: 15+ endpoints cần fix:
- Tất cả grading endpoints
- Tất cả admin endpoints
- Exam update/delete endpoints
- Question management endpoints

---

### 2.2 LỖI LOẠI 2: Race Condition Khi Submit Đáp Án (CRITICAL)

**Vị trí**: `AnswerService.cs` → `SubmitAnswerAsync()`  
**Nguy Hiểm**: Cao - Dữ liệu corrupt  
**Tác Động**: Sinh viên click nhanh → Tạo nhiều answer record

#### Problem Code:
```csharp
// AnswerService.cs - Không thread-safe
public async Task<...> SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request)
{
    // Bước 1: Check xem đã có answer cho câu hỏi này chưa
    var existing = await _answerRepository.GetByAttemptAndQuestionAsync(
        attemptId, request.QuestionId);
    
    if (existing != null)
        return await UpdateAnswerAsync(...);
    
    // ❌ RACE CONDITION: Giữa check và create
    // Request 2 cũng check và không thấy existing
    
    var answer = new Answer { ... };
    await _answerRepository.CreateAsync(answer);  // Request 2 cũng insert!
    // Kết quả: 2 answer record cho cùng question
}
```

#### Timeline Lỗi:
```
Request 1 (Click Option A)  |  Request 2 (Click Option B)
────────────────────────────┼────────────────────────
GET existing (null)         |
                            | GET existing (null)
INSERT answer (Option A)    |
                            | INSERT answer (Option B)
                            
Kết quả: 2 answer record, chỉ 1 được count khi chấm điểm
Score calculation sai!
```

#### Giải Pháp:
```sql
-- Thêm UNIQUE constraint vào database
ALTER TABLE answers 
ADD CONSTRAINT uk_attempt_question 
UNIQUE(exam_attempt_id, question_id);
```

```csharp
// Code sử dụng upsert pattern
public async Task<...> SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request)
{
    // Một lần mà check và update, không split
    var existing = await _answerRepository
        .GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);
    
    if (existing != null) {
        existing.TextContent = request.TextContent;
        existing.AnsweredAt = DateTime.UtcNow;
        await _answerRepository.UpdateAsync(existing);
    } else {
        var answer = new Answer 
        { 
            ExamAttemptId = attemptId,
            QuestionId = request.QuestionId,
            TextContent = request.TextContent,
            AnsweredAt = DateTime.UtcNow
        };
        await _answerRepository.CreateAsync(answer);
    }
}
```

---

### 2.3 LỖI LOẠI 3: Không Enforce Thời Gian Thi (CRITICAL)

**Vị Trí**: `ExamAttemptService.cs`, `AnswerService.cs`  
**Nguy Hiểm**: Rất cao  
**Tác Động**: Sinh viên nộp sau deadline vẫn được chấp nhận

#### Problem Code:
```csharp
// ExamAttemptService.cs - SubmitAttemptAsync()
public async Task<...> SubmitAttemptAsync(long attemptId)
{
    var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
    if (attempt == null) return (false, "Not found", null);
    
    if (attempt.Status != "IN_PROGRESS")
        return (false, "Not in progress", null);
    
    // ❌ KHÔNG CÓ: Check xem đã quá thời gian chưa!
    // if (DateTime.UtcNow > exam.EndTime) return REJECT
    
    attempt.Status = "SUBMITTED";
    attempt.EndTime = DateTime.UtcNow;
    await _examAttemptRepository.UpdateAsync(attempt);
}
```

#### Tình Huống Thực Tế:
```
Bài kiểm tra Tiếng Anh: 14:00-15:00 (60 phút)
Sinh viên Hùng:
- 14:55: Login vào bài
- 14:58: Bắt đầu làm 
- 15:10: Nộp bài (10 phút sau deadline!)

Hệ thống:
✓ CHẤP NHẬN nộp bài
(Seharusnya từ chối)

Giáo viên không biết bài nộp muộn
```

#### Giải Pháp:
```csharp
public async Task<...> SubmitAttemptAsync(long attemptId)
{
    var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
    var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
    
    // Kiểm tra thời gian
    if (exam.EndTime != default && DateTime.UtcNow > exam.EndTime)
        return (false, "Exam window closed", null);
    
    // Kiểm tra tổng thời gian làm = không vượt duration
    var timeTaken = DateTime.UtcNow - attempt.StartTime;
    if (timeTaken.TotalMinutes > exam.DurationMinutes)
        return (false, "Exceeded duration", null);
    
    attempt.Status = "SUBMITTED";
    attempt.EndTime = DateTime.UtcNow;
    await _examAttemptRepository.UpdateAsync(attempt);
}
```

**Cần thêm**:
```csharp
// Trong Exam entity
public bool AllowLateSubmission { get; set; } = false;
public int LateSubmissionMinutes { get; set; } = 0;
public decimal LatePenaltyPercent { get; set; } = 0;
```

---

### 2.4 LỖI LOẠI 4: Không Check Enrollment (CRITICAL)

**Vị Trí**: `ExamAttemptService.StartAttemptAsync()`  
**Nguy Hiểm**: Cao - Security  
**Tác Động**: Sinh viên lớp khác cũng làm được bài của lớp mình

#### Problem Code:
```csharp
// ExamAttemptService.cs
public async Task<...> StartAttemptAsync(long examId, long studentId)
{
    var exam = await _examRepository.GetByIdAsync(examId);
    var student = await _studentRepository.GetByIdAsync(studentId);
    
    // ❌ LỖI: Không check xem student có ở trong class of exam không!
    // Mà chỉ check có bài thi này tồn tại không
    
    if (exam is null)
        return (false, "Exam not found", null);
    
    var attempt = new ExamAttempt 
    { 
        ExamId = examId, 
        StudentId = studentId,
        StartTime = DateTime.UtcNow,
        Status = "IN_PROGRESS"
    };
    await _examAttemptRepository.CreateAsync(attempt);
    return (true, "Started", ...);
}
```

#### Tình Huống Thực Tế:
```
Bài thi Toán 10: Được gán cho Lớp 10A, 10B
Sinh viên Linh (Lớp 10C):
API endpoint: POST /api/exam-attempts/start?examId=5&studentId=50

Hệ thống:
1. Check: Exam (ID=5) tồn tại? ✓
2. Check: Student (ID=50) tồn tại? ✓
3. Tạo attempt

❌ Không check: Sinh viên 50 có ở lớp 10A hoặc 10B không?
👉 Sinh viên lớp 10C làm được bài kiểm tra của lớp khác!
```

#### Giải Pháp:
```csharp
public async Task<...> StartAttemptAsync(long examId, long studentId)
{
    var exam = await _examRepository.GetByIdAsync(examId);
    var student = await _studentRepository.GetByIdAsync(studentId);
    
    if (exam is null || student is null)
        return (false, "Invalid exam or student", null);
    
    // ✅ KIỂM TRA: Student có ở trong class được gán thi không?
    var allowedClasses = await _examClassRepository
        .GetClassesByExamAsync(examId);
    
    if (!allowedClasses.Contains(student.ClassId))
        return (false, "Student not enrolled for this exam", null);
    
    // Nếu có policy, check số lần thi
    if (exam.MaxAttemptsAllowed > 0) {
        var previousAttempts = await _examAttemptRepository
            .CountByStudentAndExamAsync(studentId, examId);
        
        if (previousAttempts >= exam.MaxAttemptsAllowed)
            return (false, "Maximum attempts exceeded", null);
    }
    
    var attempt = new ExamAttempt { ... };
    await _examAttemptRepository.CreateAsync(attempt);
}
```

---

### 2.5 LỖI LOẠI 5: Cascading Delete Không Đầy Đủ (CRITICAL)

**Vị Trí**: `ApplicationDbContext.cs`, Exam → Answer relationship  
**Nguy Hiểm**: Rất cao - Database inconsistency  
**Tác Động**: Xóa đề thi → Answer records mồ côi (orphaned)

#### Problem:
```
Database Relationships:
exams (1) ──── (M) exam_attempts
                      │
                      └──── (M) answers

Khi DELETE exam:
exams (deleted!)
├── exam_attempts → ON DELETE CASCADE (xóa)
│   └── answers → ❌ NO CASCADE (MỒ CÔI!)

Kết quả: Còn 150 answer records "nổi" trong DB
Score calculation bị lỗi
Gradebook report show sai
```

#### Tình Huống Thực Tế:
```
Giáo viên Minh tạo "Đề mô phỏng":
- 150 sinh viên làm bài
- Tạo 150 attempts × 5 questions = 750 answer records

Sau đó (hôm sau) xóa exam:
1. Exam record deleted ✓
2. ExamAttempt records deleted ✓
3. Answer records: STILL EXIST ❌
   - Chiếm 5% dung lượng database
   - Score calculations inconsistent

Admin không thấy vì "deleted exam không show"
Nhưng dữ liệu bị ngập lụt
```

#### Giải Pháp:
```csharp
// ApplicationDbContext.cs - OnModelCreating()
modelBuilder.Entity<Answer>()
    .HasOne(a => a.ExamAttempt)
    .WithMany(ea => ea.Answers)
    .HasForeignKey(a => a.ExamAttemptId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD THIS

modelBuilder.Entity<GradingResult>()
    .HasOne(gr => gr.ExamAttempt)
    .WithMany(ea => ea.GradingResults)
    .HasForeignKey(gr => gr.ExamAttemptId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD THIS

modelBuilder.Entity<ExamViolation>()
    .HasOne(ev => ev.ExamAttempt)
    .WithMany(ea => ea.ExamViolations)
    .HasForeignKey(ev => ev.ExamAttemptId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD THIS
```

---

### 2.6 LỖI LOẠI 6: Concurrent Score Updates (HIGH)

**Vị Trí**: `GradingService.BatchGradeAsync()`, `ExamAttemptService.SubmitAttemptAsync()`  
**Nguy Hiểm**: Cao  
**Tác Động**: Điểm số bị ghi đè khi cùng lúc chấm

#### Problem Code:
```csharp
// GradingService.cs
public async Task<...> BatchGradeAsync(long attemptId, BatchGradeRequest request)
{
    var attempt = await _attemptRepo.GetByIdAsync(attemptId);
    var totalScore = 0m;
    
    // Chấm nhiều câu
    foreach (var grade in request.Grades) {
        await _gradingRepo.CreateAsync(new GradingResult { ... });
        totalScore += grade.Score;
    }
    
    // ❌ RACE CONDITION: Không có locking!
    // Hai request cùng lúc chấm câu khác nhau → Score value bị overwrite
    attempt.Score = totalScore;  // Last write wins!
    await _attemptRepo.UpdateAsync(attempt);
}

// ExamAttemptService.cs - Cùng lúc auto-grade
public async Task<...> AutoGradeAttemptAsync(ExamAttempt attempt)
{
    var totalScore = CalculateAutoGradeScore();
    attempt.Score = totalScore;  // 👈 Overwrite giáo viên chấm được!
    await _attemptRepo.UpdateAsync(attempt);
}
```

#### Timeline Lỗi:
```
Bài thi có 5 câu, chia cho 2 giáo viên chấm:
- Teacher1 chấm Q1 (8 điểm)
- Teacher2 chấm Q2 (7 điểm)  
- Auto-grade Q3-5 (12 điểm)

Lúc 10:00 → Tất cả chạy song song:

T1: GET attempt(score=null)
        SET score=8  (Q1 only)
        UPDATE ✓

        T2: GET attempt(score=8)
                SET score=7 (Q2 overwrite!) 
                UPDATE ✓ (Quên T1!)

            AG: GET attempt(score=7)
                    SET score=12 (Q3-5)
                    UPDATE ✓ (Quên T1 and T2!)

FINAL RESULT: score=12 (Sai! Phải là 8+7+12=27)
👉 Mất 8+7=15 điểm!
```

#### Giải Pháp - Sử dụng Optimistic Locking:
```csharp
// AllEntities.cs - ExamAttempt
public class ExamAttempt
{
    public long Id { get; set; }
    // ... other fields ...
    
    [Timestamp]  // ✅ Add this
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

// GradingService.cs - Sử dụng transaction + version check
public async Task<...> BatchGradeAsync(long attemptId, BatchGradeRequest request)
{
    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            var totalScore = 0m;
            
            foreach (var grade in request.Grades) {
                await _gradingRepo.CreateAsync(new GradingResult { ... });
                totalScore += grade.Score;
            }
            
            attempt.Score = totalScore;
            await _attemptRepo.UpdateAsync(attempt);  // Throw if version mismatch!
            await transaction.CommitAsync();
            
            return (true, "Graded successfully", ...);
        }
        catch (DbUpdateConcurrencyException) {
            await transaction.RollbackAsync();
            return (false, "Another user modified this attempt", null);
        }
    }
}
```

---

### 2.7 LỖI LOẠI 7: Ma Trận Phân Công Giảng Dạy Không Rõ Ràng (CRITICAL)

**Vị Trí**: Database schema + Authorization logic  
**Nguy Hiểm**: Rất cao - Security & Data access  
**Tác Động**: Giáo viên có quyền sai → Truy cập/sửa dữ liệu không được phép

#### Problem #1: Schema Thiếu Teaching Assignment Mapping

**Hiện Tại**: 
```
Entities có:
- Teacher (ID, user_id, school_id, subject_id) ❌ Vấn đề!
- Class (ID, school_id, name, grade)
- Subject (ID, school_id, name)
- ??? CLASS - SUBJECT - TEACHER mapping?
```

**Vấn đề**: Teacher entity chỉ có 1 `subject_id` → Giáo viên không thể dạy nhiều môn ở lớp khác nhau!

```csharp
// ❌ WRONG - Teacher chỉ dạy 1 môn toàn trường
public class Teacher 
{
    public long Id { get; set; }
    public long SubjectId { get; set; }  // ← Chỉ 1 môn!
    // Không có: ClassId, cách nào biết giáo viên dạy lớp nào?
}
```

**Tình Huống Fail**:
```
Giáo viên Hùng dạy:
- Toán cho lớp 10A, 10B
- Lý cho lớp 11A

Hiện tại schema:
- Teacher(id=5, subjectId=1) → Chỉ ghi Toán
- Không thể record: Lý + lớp 11A
- System không biết Hùng dạy lớp nào

👉 KẾT QUẢ:
  - Hùng tạo bài thi "Đại số" → Không biết assign cho lớp nào
  - Hùng view gradebook → Không biết lọc lớp nào
  - Hùng update điểm → Không có quyền kiểm soát
```

#### Problem #2: Không Phân Biệt Giáo Viên Bộ Môn vs Giáo Viên Chủ Nhiệm

**Hiện Tại**: Class entity có `homeroom_teacher_id` nhưng không dùng trong authorization

```csharp
public class Class 
{
    public long Id { get; set; }
    public long? HomeroomTeacherId { get; set; }  // ← Có nhưng không dùng!
    // Không phân biệt:
    // - Giáo viên chủ nhiệm (quản lý tất cả mọi thứ của lớp)
    // - Giáo viên bộ môn (chỉ quản lý môn học của mình)
}
```

**Kỳ Vọng**:
- **Giáo Viên Bộ Môn**: Xem + tạo bài + chấm điểm **CHỈ** cho lớp được giao dạy, môn được giao
- **Giáo Viên Chủ Nhiệm**: Xem tất cả dữ liệu lớp (điểm, học sinh, hoạt động), chỉnh sửa thông tin lớp
- **Đồng Thời**: Nếu vừa là GVCN vừa là GVBM → CÓ CẢ 2 QUYỀN

#### Problem #3: Không Enforce Authorization Theo Mapping

**Hiện Tại**: Controllers không check giáo viên có quyền với class/subject không

```csharp
// ❌ ExamsController.cs - CreateAsync
[Authorize(Roles = "TEACHER")]
[HttpPost]
public async Task<ActionResult> CreateExam([FromBody] CreateExamRequest request)
{
    // Không check: 
    // 1. Giáo viên có dạy class này không?
    // 2. Giáo viên có dạy subject này không?
    // 3. Mapping Class-Subject-Teacher có tồn tại?
    
    var exam = new Exam 
    { 
        ClassId = request.ClassId,      // ✓ Set
        SubjectId = request.SubjectId,  // ✓ Set
        CreatedBy = userId              // ✓ Set
        // ❌ Nhưng không verify user có quyền!
    };
    
    await _examRepository.CreateAsync(exam);
    return Ok(exam);
}
```

**Tình Huống Fail**:
```
Giáo viên Linh (Toán):
- Được dạy: Lớp 10A Toán, Lớp 10B Toán
- KHÔNG được dạy: Lớp 10C (gì cả), Lý, Hóa

Linh gọi API:
POST /api/exams
{
  "title": "Ôn tập Hóa học",
  "classId": 3,      // Lớp 10C
  "subjectId": 5,    // Hóa
  "duration": 60
}

System:
✓ Check: Linh là TEACHER? YES
✗ Check: Linh dạy lớp 10C? NO
✗ Check: Linh dạy môn Hóa? NO

❌ NHƯNG HỆ THỐNG CHẤP NHẬN!
👉 Linh tạo bài thi Hóa cho lớp không được phép
```

#### Problem #4: Data Leakage - Giáo Viên Xem Được Dữ Liệu Lớp Khác

**Hiện Tại**: Giáo viên xem tất cả class/exam/students (không lọc)

```csharp
// ❌ GradebookController.cs
[Authorize(Roles = "TEACHER")]
[HttpGet("classes/{classId}/grades")]
public async Task GetGradebook(long classId)
{
    // Không check: User có dạy class này không?
    var grades = await _gradingService.GetGradesByClassAsync(classId);
    return Ok(grades);  // ✓ Return tất cả
}

// Giáo viên Minh (Toán) gọi:
// GET /api/gradebook/classes/15/grades
// → Nhận được tất cả điểm Lý của lớp 15, mặc dù không dạy
```

#### Problem #5: Tạo Bài Thi Không Enforce Mapping

```csharp
// ❌ ExamsController - CreateExamAsync
var exam = new Exam 
{
    CreatedByTeacherId = userId,  // ← GHI NHỚ: Chưa validate!
    ClassId = request.ClassId,
    SubjectId = request.SubjectId
};

// Sau này chấm điểm:
var gradeGerm = new GradingResult {
    ExamAttemptId = attemptId,
    GradedByUserId = userId  // ← Chưa check! GV ngoài môn có thể chấm
};
```

#### Giải Pháp Toàn Diện

**Bước 1: Tạo Teaching Assignment Entity**

```csharp
public class TeachingAssignment
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public long TeacherId { get; set; }
    // ✅ NOTE: SubjectId NOT needed here!
    // Vì 1 GV = 1 môn cố định (lấy từ Teacher.SubjectId)
    // TeachingAssignment chỉ cần track Class-Teacher mapping
    
    // Để phân biệt loại giao pô:
    // SUBJECT_TEACHER: GV bộ môn dạy lớp này (luôn cùng với Teacher.SubjectId)
    // HOMEROOM_TEACHER: GV chủ nhiệm quản lý lớp (1 GV = 1 lớp GVCN tối đa)
    public string AssignmentType { get; set; } = "SUBJECT_TEACHER";
    
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }  // null = hiện tại còn dạy
    
    public virtual Class Class { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
}

// Update Class entity:
public class Class
{
    // ... existing ...
    public long? HomeroomTeacherId { get; set; }  // ✅ NEW: Explicitly track homeroom teacher
    
    public virtual Teacher? HomeroomTeacher { get; set; }
    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new();
}

// Update Teacher entity - KEEP SubjectId!
public class Teacher
{
    public long Id { get; set; }
    public long SubjectId { get; set; }  // ✅ KEEP - 1 GV = 1 môn cố định
    public long SchoolId { get; set; }
    // Học sinh dạy 1 môn duy nhất, nhưng có thể dạy many classes cùng môn
    
    public virtual Subject Subject { get; set; } = null!;
    public virtual School School { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<TeachingAssignment> Assignments { get; set; } = new();
}

// Implementation
public class TeacherAuthorizationService : ITeacherAuthorizationService
{
    private readonly ITeachingAssignmentRepository _assignRepo;
    private readonly ITeacherRepository _teacherRepo;
    
    public async Task<bool> CanTeachClassAsync(
        long teacherId, long classId, long examSubjectId)
    {
        // Step 1: Get teacher's fixed subject
        var teacher = await _teacherRepo.GetByIdAsync(teacherId);
        if (teacher == null) return false;
        
        // Step 2: Check if teacher teaches the exam's subject
        if (teacher.SubjectId != examSubjectId) return false;
        
        // Step 3: Check if teacher is assigned to this class
        var assignment = await _assignRepo.GetByClassAndTeacherAsync(classId, teacherId);
        return assignment != null && assignment.UnassignedAt == null;
    }
    
    public async Task<List<long>> GetTeacherAccessibleClassesAsync(long teacherId)
    {
        var assignments = await _assignRepo.GetByTeacherIdAsync(teacherId, activeOnly: true);
        return assignments.Select(a => a.ClassId).Distinct().ToList();
    }
    
    public async Task<List<long>> GetTeacherTaughtClassesAsync(long teacherId)
    {
        // Same as GetTeacherAccessibleClassesAsync
        return await GetTeacherAccessibleClassesAsync(teacherId);
    }
    
    public async Task<bool> IsHomeroomTeacherAsync(long teacherId, long classId)
    {
        var assignment = await _assignRepo.GetByClassAndTeacherAsync(classId, teacherId);
        return assignment != null 
            && assignment.AssignmentType == "HOMEROOM_TEACHER"
            && assignment.UnassignedAt == null;
    }
    
    public async Task<long?> GetTeacherSubjectAsync(long teacherId)
    {
        var teacher = await _teacherRepo.GetByIdAsync(teacherId);
        return teacher?.SubjectId;
    }
}
```

**Bước 3: Enforce trên Controllers**

```csharp
// ExamsController.cs
[Authorize(Roles = "TEACHER,ADMIN")]
[HttpPost]
public async Task<ActionResult> CreateExam([FromBody] CreateExamRequest request)
{
    var userId = User.GetUserId();
    var teacherId = await _teacherRepo.GetIdByUserIdAsync(userId);  // Get teacher ID
    
    if (teacherId == null)
        return Forbid("User is not a teacher");
    
    // ✅ NEW: Verify teaching assignment
    // Check: GV dạy lớp này không? + Môn của GV match môn của exam không?
    var canTeach = await _authService.CanTeachClassAsync(
        teacherId.Value, request.ClassId, request.SubjectId);
    
    if (!canTeach)
        return Forbid($"You are not assigned to teach this class, or you don't teach this subject");
    
    // ... create exam ...
}

// GradingController - Grade exam
[Authorize(Roles = "TEACHER,ADMIN")]
[HttpPost("attempts/{attemptId}/score")]
public async Task GradeQuestion(long attemptId, [FromBody] GradeQuestionRequest request)
{
    var userId = User.GetUserId();
    var attempt = await _attemptRepo.GetByIdAsync(attemptId);
    var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
    
    var teacherId = await _teacherRepo.GetIdByUserIdAsync(userId);
    
    // ✅ NEW: Can only grade if:
    // 1. You created the exam, OR
    // 2. You are homeroom teacher of the class, OR
    // 3. You teach the subject for that class
    var isExamCreator = exam.CreatedByTeacherId == teacherId;
    var isHomeroom = await _authService.IsHomeroomTeacherAsync(teacherId!.Value, exam.ClassId);
    var isSubjectTeacher = await _authService.CanTeachClassSubjectAsync(
        teacherId!.Value, exam.ClassId, exam.SubjectId);
    
    if (!isExamCreator && !isHomeroom && !isSubjectTeacher)
        return Forbid("Not authorized to grade this exam");
    
    // ... grade ...
}

// StudentsController - View class students
[Authorize(Roles = "TEACHER")]
[HttpGet("classes/{classId}/students")]
public async Task GetClassStudents(long classId)
{
    var userId = User.GetUserId();
    var teacherId = await _teacherRepo.GetIdByUserIdAsync(userId);
    
    // ✅ NEW: Can only view if teacher of this class
    var accessibleClasses = await _authService.GetTeacherAccessibleClassesAsync(teacherId!.Value);
    if (!accessibleClasses.Contains(classId))
        return Forbid("Not assigned to this class");
    
    var students = await _studentRepo.GetByClassIdAsync(classId);
    return Ok(students);
}
```

**Bước 4: Data Filtering - Chỉ show class được phép**

```csharp
// ExamsController - GetByTeacher
[Authorize(Roles = "TEACHER")]
[HttpGet("my-exams")]
public async Task GetMyExams()
{
    var userId = User.GetUserId();
    var teacherId = await _teacherRepo.GetIdByUserIdAsync(userId);
    
    // ✅ NEW: Filter by teaching assignments
    var assignments = await _authService.GetTeacherAssignmentsAsync(teacherId!.Value);
    var classIds = assignments.Select(a => a.ClassId).ToList();
    var homeroomClasses = await _authService.GetTeacherAccessibleClassesAsync(teacherId.Value);
    
    var allAccessibleClasses = classIds.Concat(homeroomClasses).Distinct();
    
    var exams = await _examRepo.GetByClassIdsAsync(allAccessibleClasses);
    return Ok(exams);
}

// GradebookController - Class grades
[Authorize(Roles = "TEACHER")]
[HttpGet("classes/{classId}")]
public async Task GetClassGradebook(long classId)
{
    var userId = User.GetUserId();
    var teacherId = await _teacherRepo.GetIdByUserIdAsync(userId);
    
    // ✅ NEW: Verify access
    var accessibleClasses = await _authService.GetTeacherAccessibleClassesAsync(teacherId!.Value);
    if (!accessibleClasses.Contains(classId))
        return Forbid();
    
    var grades = await _gradingService.GetGradesByClassAsync(classId);
    return Ok(grades);
}
```

---

## 🟡 PHẦN 3: CÁC TÍNH NĂNG CÒN THIẾU

### 3.1 Chính Sách Làm Bài Lại (Retake Policy) - ❌ MISSING

**Tác Động**: Không thể enforce retake policy ở trường học  
**Fields Cần Thêm**:

```csharp
public class Exam
{
    // ... existing fields ...
    
    // ❌ MISSING - Cần thêm:
    public int MaxAttemptsAllowed { get; set; } = 1;
    public bool AllowImmediateRetake { get; set; } = false;
    public int MinutesBetweenRetakes { get; set; } = 0; // 7 ngày = 10080 phút
    public bool AllowRetakeIfPassed { get; set; } = false;
    public decimal? RetakePassingScore { get; set; } = null; // Nếu < 5 thì thi lại
}
```

**Logic Cần Thêm**:
```csharp
public async Task<(bool Allow, string Reason)> CanAttemptExamAsync(
    long studentId, long examId)
{
    var exam = await _examRepository.GetByIdAsync(examId);
    var attempts = await _examAttemptRepository
        .GetAttemptsByStudentAndExamAsync(studentId, examId);
    
    // Check 1: Số lần thi
    if (attempts.Count >= exam.MaxAttemptsAllowed)
        return (false, "Exceeded max attempts");
    
    // Check 2: Đã pass thì không thi lại (trừ allow policy)
    var passedAttempt = attempts.FirstOrDefault(a => a.Score >= exam.PassingScore);
    if (passedAttempt != null && !exam.AllowRetakeIfPassed)
        return (false, "Already passed, retake not allowed");
    
    // Check 3: Thời gian giữa lần thi
    if (exam.MinutesBetweenRetakes > 0) {
        var lastAttempt = attempts.OrderByDescending(a => a.EndTime).FirstOrDefault();
        var minutesElapsed = (DateTime.UtcNow - lastAttempt?.EndTime).TotalMinutes;
        if (minutesElapsed < exam.MinutesBetweenRetakes)
            return (false, $"Must wait {exam.MinutesBetweenRetakes} minutes");
    }
    
    return (true, "Allowed");
}
```

### 3.2 Xử Lý Nộp Bài Muộn (Late Submission) - 🟡 PARTIAL

**Hiện Tại**: Deadline được định nghĩa nhưng không được enforce  
**Cần Thêm**:

```csharp
public class Exam
{
    // Existing
    public DateTime EndTime { get; set; }
    
    // ❌ MISSING:
    public bool AllowLateSubmission { get; set; } = false;
    public int LateSubmissionMinutes { get; set; } = 0;  // Bao lâu sau deadline
    public decimal LatePenaltyPercent { get; set; } = 0; // Trừ % điểm
}
```

**Logic**:
```csharp
public async Task<...> SubmitAttemptWithLateCheckAsync(long attemptId)
{
    var attempt = await _attemptRepo.GetByIdAsync(attemptId);
    var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
    
    var minutesLate = (DateTime.UtcNow - exam.EndTime).TotalMinutes;
    
    if (minutesLate > 0) {
        if (!exam.AllowLateSubmission)
            return (false, "Exam deadline passed", null);
        
        if (minutesLate > exam.LateSubmissionMinutes)
            return (false, "Late submission window closed", null);
        
        attempt.IsLateSubmission = true;
        attempt.MinutesLate = (int)minutesLate;
    }
    
    // ... submit logic ...
    
    // Áp dụng penalty sau khi chấm
    if (attempt.IsLateSubmission && exam.LatePenaltyPercent > 0) {
        var penalty = attempt.Score * (exam.LatePenaltyPercent / 100);
        attempt.Score -= penalty;
        attempt.LatePenaltyApplied = penalty;
    }
}
```

### 3.3 Tính Năng Audit Trail Cho Chấm Điểm - 🟡 PARTIAL

**Hiện Tại**: ActivityLog tồn tại nhưng chỉ record high-level  
**Cần Thêm** - Một bảng mới:

```csharp
public class GradingAuditLog
{
    public long Id { get; set; }
    public long GradingResultId { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public long GradedByUserId { get; set; }
    
    public decimal? OldScore { get; set; }
    public decimal NewScore { get; set; }
    public string? Comment { get; set; }
    
    public DateTime ChangedAt { get; set; }
    public string ChangeReason { get; set; } // "AUTO_GRADE", "MANUAL_GRADE", "OVERRIDE"
}
```

### 3.4 Notification Khi Công Bố Kết Quả - ❌ MISSING

**Yêu Cầu**: Sinh viên nhận notification khi điểm công bố

```csharp
// GradingService.cs - PublishResultAsync()
public async Task<...> PublishResultAsync(long attemptId)
{
    var attempt = await _attemptRepo.GetByIdAsync(attemptId);
    attempt.IsResultPublished = true;
    await _attemptRepo.UpdateAsync(attempt);
    
    // ❌ MISSING: Notify student
    await _notificationService.SendAsync(new Notification
    {
        UserId = attempt.Student.UserId,
        Title = "Kết quả bài kiểm tra công bố",
        Message = $"Bài {attempt.Exam.Title} - Điểm: {attempt.Score}/{attempt.Exam.TotalScore}",
        Type = "RESULT_PUBLISHED",
        RelatedAttemptId = attemptId,
        CreatedAt = DateTime.UtcNow
    });
}
```

### 3.5 Exam Violation Tracking - 🟡 SCHEMA CÓ, LOGIC CHƯA

**Hiện Tại**: Bảng `ExamViolation` tồn tại nhưng:
- Frontend không record violations
- API endpoint có nhưng không validate

**Cần Implement**:
```csharp
// ExamPlayerPage.tsx - Detect violations
const detectViolations = useCallback(async () => {
    const violations: ViolationType[] = [];
    
    // Detect 1: Browser tab mất focus (quitting)
    if (document.hidden) {
        violations.push("TAB_SWITCH");
    }
    
    // Detect 2: Copy-paste từ clipboard
    if (attemptedCopyPaste) {
        violations.push("ATTEMPTED_COPY");
    }
    
    // Detect 3: Right-click context menu
    if (attemptedInspect) {
        violations.push("INSPECT_ELEMENT");
    }
    
    // Detect 4: Full screen not enabled
    if (!isFullscreen && isProctored) {
        violations.push("NOT_FULLSCREEN");
    }
    
    if (violations.length > 0) {
        await examAttemptsApi.logViolation(attempt.id, { 
            violations,
            details: navigator.userAgent 
        });
    }
}, [attempt.id, isProctored]);
```

---

## ✅ PHẦN 4: GIẢI PHÁP ĐỀ XUẤT

### 4.1 Bản Vá Chi Tiết (Critical Fixes)

**Tuần 1 - Critical Security & Integrity Fixes (40-50 giờ)**

#### Fix 1: Authorization Enforcement
**File**: Tất cả 28 controllers  
**Độ ưu tiên**: 1️⃣  
**Effort**: 12 giờ

```csharp
// Template áp dụng tất cả controllers
[Authorize(Roles = "TEACHER,ADMIN")]
[HttpPost("grade")]
public async Task Grade(...) { ... }

[Authorize(Roles = "STUDENT")]
[HttpPost("submit")]
public async Task Submit(...) { ... }

[Authorize(Roles = "ADMIN")]
[HttpDelete("{id}")]
public async Task Delete(...) { ... }
```

**Scope**:
- GradingController (8 endpoints) → TEACHER
- AdminController (5 endpoints) → ADMIN
- ExamsController (Update/Delete) → TEACHER
- QuestionsController (Update/Delete) → TEACHER
- TotalEndpoints: 20+

---

#### Fix 2: Answer Submission Race Condition Fix
**File**: `AnswerService.cs` + `ApplicationDbContext.cs`  
**Độ ưu tiên**: 2️⃣  
**Effort**: 4 giờ

**Step 1 - DB Constraint** (Migration):
```csharp
// Migration: AddAnswerUniqueness.cs
migrationBuilder.CreateIndex(
    name: "ix_answers_attempt_question",
    table: "answers",
    columns: new[] { "exam_attempt_id", "question_id" },
    unique: true);
```

**Step 2 - Code Fix**:
```csharp
// AnswerService.cs - SubmitAnswerAsync
try {
    var existing = await _answerRepository.GetByAttemptAndQuestionAsync(...);
    if (existing != null) {
        existing.TextContent = request.TextContent;
        existing.AnsweredAt = DateTime.UtcNow;
        await _answerRepository.UpdateAsync(existing);
    } else {
        await _answerRepository.CreateAsync(new Answer { ... });
    }
} catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate")) {
    // Retry logic hoặc return error
    return (false, "Duplicate answer detected", null);
}
```

---

#### Fix 3: Exam Time Window Enforcement
**File**: `ExamAttemptService.cs` x 3 methods  
**Độ ưu tiên**: 3️⃣  
**Effort**: 6 giờ

```csharp
// Helper method
private async Task<(bool Valid, string Error)> ValidateExamTimeWindowAsync(
    Exam exam, DateTime currentTime)
{
    if (exam.StartTime != default && currentTime < exam.StartTime)
        return (false, "Exam not yet started");
    
    if (exam.EndTime != default && currentTime > exam.EndTime)
        return (false, "Exam window closed");
    
    return (true, "");
}

// Apply in: StartAttemptAsync, SubmitAttemptAsync, AnswerSubmitAsync
public async Task<...> StartAttemptAsync(long examId, long studentId)
{
    var exam = await _examRepository.GetByIdAsync(examId);
    
    var (valid, error) = await ValidateExamTimeWindowAsync(exam, DateTime.UtcNow);
    if (!valid) return (false, error, null);
    
    // ... rest of logic
}
```

---

#### Fix 4: Student Enrollment Verification
**File**: `ExamAttemptService.cs` → `StartAttemptAsync()`  
**Độ ưu tiên**: 4️⃣  
**Effort**: 5 giờ

```csharp
public async Task<...> StartAttemptAsync(long examId, long studentId)
{
    var exam = await _examRepository.GetByIdAsync(examId);
    var student = await _studentRepository.GetByIdAsync(studentId);
    
    if (exam is null || student is null)
        return (false, "Invalid exam or student", null);
    
    // ✅ NEW: Check enrollment
    var allowedClasses = await _examClassRepository
        .GetClassesByExamAsync(examId);
    
    if (!allowedClasses.Contains(student.ClassId)) {
        _logger.LogWarning(
            "Unauthorized exam attempt: Student {StudentId} not in allowed class for exam {ExamId}",
            studentId, examId);
        return (false, "Not enrolled for this exam", null);
    }
    
    // ... existing logic
}
```

---

#### Fix 5: Database Cascade Delete
**File**: `ApplicationDbContext.cs` → `OnModelCreating()`  
**Độ ưu tiên**: 5️⃣  
**Effort**: 3 giờ

```csharp
// ApplicationDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configs ...
    
    // ✅ ADD cascades for exam attempt relationships
    modelBuilder.Entity<Answer>()
        .HasOne(a => a.ExamAttempt)
        .WithMany(ea => ea.Answers)
        .HasForeignKey(a => a.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);
    
    modelBuilder.Entity<GradingResult>()
        .HasOne(gr => gr.ExamAttempt)
        .WithMany(ea => ea.GradingResults)
        .HasForeignKey(gr => gr.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);
    
    modelBuilder.Entity<ExamViolation>()
        .HasOne(ev => ev.ExamAttempt)
        .WithMany(ea => ea.ExamViolations)
        .HasForeignKey(ev => ev.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);
}
```

**Migration**:
```csharp
// Migration: FixCascadeDeletes.cs
migrationBuilder.DropForeignKey("fk_answers_exam_attempt", "answers");
migrationBuilder.AddForeignKey(
    name: "fk_answers_exam_attempt",
    table: "answers",
    column: "exam_attempt_id",
    principalTable: "exam_attempts",
    principalColumn: "id",
    onDelete: ReferentialAction.Cascade);

// Repeat for GradingResult, ExamViolation
```

---

#### Fix 6: Optimistic Locking for Score Updates
**File**: `AllEntities.cs` + `GradingService.cs`  
**Độ ưu tiên**: 6️⃣  
**Effort**: 8 giờ

**Step 1 - Schema**:
```csharp
public class ExamAttempt
{
    public long Id { get; set; }
    // ... existing fields ...
    
    [Timestamp]  // ✅ Add this
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

**Migration**:
```csharp
// Migration: AddRowVersionToExamAttempt.cs
migrationBuilder.AddColumn<byte[]>(
    name: "row_version",
    table: "exam_attempts",
    type: "bytea",
    rowVersion: true);
```

**Step 2 - Service Code**:
```csharp
public async Task<...> BatchGradeAsync(long attemptId, BatchGradeRequest request, long gradedBy)
{
    try {
        using (var transaction = await _context.Database.BeginTransactionAsync()) {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            var totalScore = 0m;
            
            foreach (var item in request.Grades) {
                var grading = new GradingResult { ... };
                await _gradingRepo.CreateAsync(grading);
                totalScore += item.Score;
            }
            
            attempt.Score = totalScore;
            attempt.GradedAt = DateTime.UtcNow;
            await _attemptRepo.UpdateAsync(attempt);  // Will throw if version mismatch
            
            await transaction.CommitAsync();
            return (true, "Graded successfully", ...);
        }
    } catch (DbUpdateConcurrencyException) {
        _logger.LogWarning("Concurrency conflict on attempt {AttemptId}", attemptId);
        return (false, "Another user modified this, please reload", null);
    }
}
```

---

### 4.2 Bản Patch Cho Tính Năng Thiếu

**Tuần 2 – Feature Implementation (35-40 giờ)**

#### Feature 1: Retake Policy (8 giờ)
```csharp
// 1. Update Exam entity
public int MaxAttemptsAllowed { get; set; } = 1;
public int MinutesBetweenRetakes { get; set; } = 0;
public bool AllowRetakeIfPassed { get; set; } = false;
public decimal? RetakePassingScore { get; set; } = null;

// 2. Migration to add columns
// 3. Service method: CanAttemptExamAsync()
// 4. Controller validation in StartAttemptAsync()
// 5. Frontend: Hide "Start Attempt" button if cannot retry
```

#### Feature 2: Late Submission Support (6 giờ)
```csharp
// 1. Update Exam entity
public bool AllowLateSubmission { get; set; } = false;
public int LateSubmissionMinutes { get; set; } = 0;
public decimal LatePenaltyPercent { get; set; } = 0;

// 2. Update ExamAttempt entity
public bool IsLateSubmission { get; set; } = false;
public int? MinutesLate { get; set; }
public decimal? LatePenaltyApplied { get; set; }

// 3. Service logic to detect & apply penalty
// 4. Gradebook to show penalty info
```

#### Feature 3: Grading Audit Log (7 giờ)
```csharp
// 1. Create GradingAuditLog entity
// 2. Record every grade change
// 3. API endpoint to view audit trail
// 4. Admin report: Grade changes by teacher
```

#### Feature 4: Result Publication Notifications (5 giờ)
```csharp
// 1. Implement INotificationService
// 2. Send notification when result published
// 3. Add notification page in frontend
// 4. Mark notifications as read
```

#### Feature 5: Violation Tracking (8 giờ)
```csharp
// Frontend ExamPlayerPage.tsx:
// 1. Detect tab switch (visibilitychange event)
// 2. Detect fullscreen loss
// 3. Detect attempted console access
// 4. Send violation logs to backend

// Backend:
// 1. Store violations in ExamViolation table
// 2. Display in grading review (flag for teacher)
// 3. Statistics: violation rates by exam
```

---

### 4.3 Bản Vá Testing

**Tuần 3 – Testing & Quality (20-25 giờ)**

#### Unit Tests (8 giờ):
```csharp
// ExamAttemptServiceTests.cs
[TestFixture]
public class ExamAttemptServiceTests
{
    [Test]
    public async Task StartAttempt_ShouldReject_WhenNotEnrolled()
    {
        // Arrange: Student not in allowed class
        // Act: StartAttemptAsync()
        // Assert: Returns false "Not enrolled"
    }
    
    [Test]
    public async Task StartAttempt_ShouldReject_BeforeExamStart()
    {
        // Arrange: Current time before exam.StartTime
        // Act
        // Assert: Rejected
    }
    
    [Test]
    public async Task SubmitAttempt_ShouldReject_AfterDeadline()
    {
        // Arrange: After exam.EndTime
        // Act: SubmitAttemptAsync()
        // Assert: Rejected with deadline error
    }
}

// AnswerServiceTests.cs
[TestFixture]
public class AnswerServiceTests
{
    [Test]
    public async Task ConcurrentSubmits_ShouldHandleRaceCondition()
    {
        // Arrange: Two concurrent submit requests same question
        // Act: Submit both in parallel
        // Assert: Exactly 1 answer record (upsert worked) or error
    }
}
```

#### Integration Tests (7 giờ):
```csharp
// GradingConcurrencyTests.cs
[TestFixture]
public class GradingConcurrencyTests
{
    [Test]
    public async Task ConcurrentGrading_ShouldPreventScoreLoss()
    {
        // Simulate 2 teachers grading same attempt
        // Verify: Second update detects version conflict
        // Result: One succeeds, one gets error
    }
}

// ExamLifecycleTests.cs
[TestFixture]
public class ExamLifecycleTests
{
    [Test]
    public async Task DeleteExam_ShouldCascadeDelete_AllChildren()
    {
        // Delete exam
        // Assert: No orphaned answer records
    }
}
```

#### Manual Testing (5 giờ):
```
Test Case: Retake after failing
  1. Student takes exam, score 4/10
  2. Check: Cannot retake immediately (if policy says wait)
  3. After 7 days, allow retake
  4. Retake: score 7/10
  5. Verify: Gradebook shows retake score

Test Case: Late submission with penalty
  1. Exam ends 15:00, allow 5 min grace
  2. Student submits 15:03 (3 min late)
  3. Auto-grade: 8/10
  4. Apply 10% penalty: 7.2/10
  5. Verify: Gradebook shows both scores + penalty

Test Case: Concurrent teacher grading
  1. Exam has 5 questions split to 2 teachers
  2. Both grade same student simultaneously
  3. Verify: No score loss, one gets conflict error
  4. Second teacher retries → success
```

---

## 📊 PHẦN 5: KẾ HOẠCH THỰC HIỆN

### 5.1 Timeline

| Phase | Công Việc | Giờ | Gate |
|-----|----------|-----|-------|
| **Phase 0** | Baseline + backup + data quality check | 6 | Baseline signed |
| **Phase 1** | Authorization enforcement (role + ownership) | 12 | 100% endpoint protection |
| **Phase 2** | Integrity core (race/time/enrollment/cascade) | 20 | Critical tests pass |
| **Phase 3** | Teaching assignment matrix + data filtering | 15 | Access control pass |
| **Phase 4** | Missing features (retake/late/audit/notify/violation) | 34 | Feature acceptance pass |
| **Phase 5** | Reliability gate (unit/integration/concurrency/load) | 18 | Regression pass |
| **Phase 6** | UAT + cutover + rollback readiness | 6 | Go-live approved |
| **TOTAL** |  | **111** |  |

### 5.1.1 Ràng Buộc Nghiệp Vụ Bắt Buộc

- 1 giáo viên chủ nhiệm tối đa 1 lớp tại một thời điểm.
- 1 giáo viên chỉ dạy 1 môn cố định (`Teacher.SubjectId`).
- Giáo viên có thể dạy môn đó ở nhiều lớp.
- Giáo viên có thể vừa là GVCN vừa là GVBM.
- Authorization bắt buộc kiểm tra đồng thời: `Teacher.SubjectId == Exam.SubjectId` và giáo viên có phân công lớp.

### 5.1.2 Thứ Tự Phụ Thuộc

`Phase 0 -> Phase 1 -> Phase 2 -> Phase 3 -> Phase 4 -> Phase 5 -> Phase 6`

- Không triển khai feature mới nếu chưa đóng xong Critical gate (Phase 1-3).
- Không go-live nếu Phase 5 còn lỗi Critical/High.

### 5.2 Priority Matrix

```
┌─────────────────────────────────────────┐
│ CRITICAL (Deploy Blocker)              │
├─────────────────────────────────────────┤
│ 1. Authorization enforcement            │
│ 2. Answer submission locking            │
│ 3. Exam time validation                 │
│ 4. Enrollment verification              │
│ 5. Cascade delete fixes                 │
│ 6. Concurrency control (locking)        │
└─────────────────────────────────────────┘
        ↓
┌─────────────────────────────────────────┐
│ HIGH (First 2 Weeks)                   │
├─────────────────────────────────────────┤
│ 7. Retake policy                        │
│ 8. Late submission handling             │
│ 9. Grading audit trail                  │
│ 10. Transaction boundaries              │
└─────────────────────────────────────────┘
        ↓
┌─────────────────────────────────────────┐
│ MEDIUM (Week 3-4)                      │
├─────────────────────────────────────────┤
│ 11. Notifications                       │
│ 12. Violation tracking                  │
│ 13. Advanced exam settings              │
│ 14. Report enhancements                 │
└─────────────────────────────────────────┘
```

### 5.3 Acceptance Criteria

**Trước khi Deploy to Production:**

- ✅ Tất cả 28 controllers có role-based authorization
- ✅ Race condition tests pass (5+ parallel submissions)
- ✅ Exam window validation: reject pre-window & post-deadline
- ✅ Enrollment check: reject unenrolled students
- ✅ Cascade delete: verify no orphaned records
- ✅ Concurrency tests: no score loss in parallel edits
- ✅ Retake policy: enforced per exam config
- ✅ Late submission: penalty applied correctly
- ✅ Grading audit: all changes logged
- ✅ Performance: < 200ms for submit answer, < 100ms for grade

---

## 📈 PHẦN 6: KẾT LUẬN

### 6.1 Tóm Tắt Sức Khỏe Hệ Thống

| Khía Cạnh | Điểm | Ghi Chú |
|----------|-----|--------|
| **Architecture** | 8/10 | Clean Architecture tốt, SOLID nguyên tắc tốt |
| **UI/UX** | 8/10 | Giao diện người dùng tốt, trải nghiệm mượt |
| **Database Design** | 7/10 | Schema tốt, thiếu vài constraint |
| **Business Logic** | 4/10 | ❌ Lỗi logic nghiêm trọng, unsafe |
| **Security** | 2/10 | ❌ No authorization, open to abuse |
| **Data Integrity** | 3/10 | ❌ Race conditions, orphaned records |
| **Production Ready** | ❌ NO | Need 3 weeks fix |

### 6.2 Rủi Ro Nếu Deploy Ngay

```
❌ KHÔNG NÊN DEPLOY HIỆN TẠI VÌ:

1. Security Risk (CRITICAL)
   - Bất kỳ user nào cũng có thể:
     * Grade bài của sinh viên khác
     * Xóa đề thi
     * Xem điểm của toàn lớp
   👉 Impact: Entire system compromised

2. Data Integrity Risk (CRITICAL)
   - Race conditions → Score loss
   - Orphaned records → Database bloat
   - Concurrent edits → Inconsistency
   👉 Impact: Wrong grades issued

3. Business Logic Risk (HIGH)
   - No retake policy → Can't manage exam policies
   - No time enforcement → Students cheat
   - No audit trail → No accountability
   👉 Impact: Unfair exams

RECOMMENDATION: Fix critical issues (2-3 weeks) before production
```

### 6.3 Ưu Điểm Hiện Tại

```
✅ ĐIỂM MẠNH:

1. Architecture (Clean Architecture đúng chuẩn)
   - Separation of concerns: Domain, Application, Infrastructure
   - Dependency Injection configured correctly
   - Repository pattern implemented
   
2. Frontend (React + TypeScript)
   - TypeScript type safety
   - Component-based architecture
   - Proper state management
   
3. Database (31 tables, well-organized)
   - Normalized schema
   - Good relationship design
   - Proper indexing
   
4. Development Foundation
   - Docker setup
   - CI/CD ready with docker-compose
   - Swagger API documentation
   - Unit test structure exists

👉 Có nền tảng tốt, chỉ cần "patch lỗi logic"
```

---

## 📋 TÀI LIỆU THAM KHẢO

### Danh sách Files Cần Sửa (Ưu tiên)

1. **GradingController.cs** - Add [Authorize(Roles = "TEACHER")]
2. **ExamAttemptsController.cs** - Add time validation
3. **AnswersController.cs** - Add transaction handling
4. **AuthController.cs** - Audit logging for all auth
5. **ApplicationDbContext.cs** - Add cascade deletes
6. **AllEntities.cs** - Add retake fields, RowVersion
7. **GradingService.cs** - Add locking, transactions
8. **ExamAttemptService.cs** - Add enrollment/time checks
9. **AnswerService.cs** - Add upsert + locking
10. Frontend pages - Add violation detection

### Test Files Cần Thêm

- `GradingConcurrencyTests.cs`
- `ExamTimeWindowTests.cs`
- `EnrollmentVerificationTests.cs`
- `RaceConditionTests.cs`
- `CascadeDeleteTests.cs`

---

**KẾT THÚC BÁNG CÁO PHÂN TÍCH**

*Báng cáo này được tạo bằng AI System Analysis. Mọi khuyến nghị dựa trên code review toàn bộ codebase.*

