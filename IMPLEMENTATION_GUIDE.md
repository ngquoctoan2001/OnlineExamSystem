# 🔧 HƯỚNG DẪN THỰC HIỆN CÁC FIX

**Mục tiêu**: Chi tiết từng bước fix các lỗi quan trọng  
**Thời gian**: 3 tuần (111 giờ)  
**Độ ưu tiên**: Critical → High → Medium

---

## PHASE TRIỂN KHAI (SAU PHÂN TÍCH)

| Phase | Mục tiêu | Deliverables chính | Effort |
|---|---|---|---|
| **Phase 0: Baseline & Data Guard** | Khóa baseline, chuẩn hóa dữ liệu teacher-class-subject | Checklist baseline, script backup, data quality report | 6h |
| **Phase 1: Security Gate** | Đóng lỗ hổng quyền truy cập API | Role enforcement cho controllers, deny-by-default policy | 12h |
| **Phase 2: Exam Integrity Core** | Chặn gian lận logic thi và data corruption | Time-window, enrollment, race-condition fix, cascade delete | 20h |
| **Phase 3: Teaching Assignment Matrix** | Enforce đúng mô hình phân công giảng dạy thực tế | TeachingAssignment + auth service + filtered endpoints | 15h |
| **Phase 4: Missing Features** | Bổ sung nghiệp vụ còn thiếu | Retake, late submission, audit log, notification, violation | 34h |
| **Phase 5: Reliability & Test Gate** | Đảm bảo ổn định trước production | Unit/integration/concurrency/load tests + bugfix vòng cuối | 18h |
| **Phase 6: UAT & Cutover** | Triển khai chính thức an toàn | UAT sign-off, runbook deploy/rollback, go-live checklist | 6h |
|  |  | **TOTAL** | **111h** |

### Trạng thái thực thi (cập nhật 2026-03-13)

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Notifications ownership guard | **Done** | Chặn truy cập chéo `userId`, mark/delete theo owner hoặc admin. |
| ExamAttempts ownership guard | **Done** | Enforce ownership theo claim cho student và exam-access cho teacher/admin trên attempt endpoints. |
| Retake policy (max attempts/cooldown/pass-retake) | **Done** | Đã có field model + DTO + service enforcement + migration. |
| Late submission (grace/penalty) | **Done** | Đã triển khai backend + migration từ phase trước. |
| Notification khi publish kết quả | **Done** | Đã có trong grading flow. |
| Grading audit trail dedicated table | **Partial** | **Quyết định hiện tại**: giữ ActivityLog (`GRADE_UPDATED`) cho release này; chưa tạo bảng `GradingAuditLog` riêng. |
| Violation tracking frontend + backend end-to-end | **Partial** | Backend có API/log violation, frontend detection/UX còn thiếu đầy đủ theo checklist PO/UAT. |

### Quyết định audit log

- Chọn **giữ ActivityLog** cho giai đoạn hiện tại để tránh mở rộng schema và rủi ro phát sinh trước UAT.
- Điều kiện nâng cấp sau UAT: nếu cần truy vết pháp lý chi tiết hơn (old/new score theo từng field), sẽ triển khai `GradingAuditLog` dedicated ở phase sau.

### Nguyên tắc nghiệp vụ bắt buộc (theo xác nhận)

- 1 giáo viên chủ nhiệm tối đa 1 lớp tại một thời điểm.
- 1 giáo viên dạy đúng 1 môn cố định (`Teacher.SubjectId`).
- 1 giáo viên có thể dạy nhiều lớp của môn đó.
- 1 giáo viên có thể vừa là GVCN vừa là GVBM.
- Mọi action tạo/sửa/chấm đề phải kiểm tra đồng thời: môn dạy + lớp được phân công.

### Dependency Flow

`Phase 0 -> Phase 1 -> Phase 2 -> Phase 3 -> Phase 4 -> Phase 5 -> Phase 6`

- Không được vào `Phase 4` nếu `Phase 1-3` chưa pass gate.
- Không được go-live nếu `Phase 5` còn lỗi mức Critical/High.

### Chi tiết triển khai theo Phase

#### Phase 0: Baseline & Data Guard (6h)

- Việc cần làm:
- Freeze nhánh release, chốt tag baseline, bật audit migration.
- Backup DB + snapshot schema trước thay đổi.
- Chạy data check: teacher không có subject, class không có homeroom, assignment trùng active.
- Đầu ra bắt buộc:
- Baseline tag + backup verified + báo cáo data quality.
- Exit criteria:
- Có thể rollback về baseline trong <= 30 phút.

#### Phase 1: Security Gate (12h)

- Việc cần làm:
- Áp role policy cho controller/action theo ma trận `ADMIN/TEACHER/STUDENT`.
- Enforce ownership checks cho update/delete/grade endpoints.
- Chuẩn hóa helper lấy `userId`, `teacherId`, role claims.
- Đầu ra bắt buộc:
- Endpoint matrix bảo vệ 100% + log deny access.
- Exit criteria:
- Test "student gọi endpoint teacher/admin" đều trả `403`.

#### Phase 2: Exam Integrity Core (20h)

- Việc cần làm:
- Fix race condition submit answer (unique index + upsert + retry-safe).
- Enforce time window + duration + enrollment check trong attempt lifecycle.
- Bổ sung cascade delete cho `answers/grading_results/exam_violations`.
- Bổ sung optimistic locking cho score update.
- Đầu ra bắt buộc:
- Không còn duplicate answers, không còn orphan records, không còn score overwrite.
- Exit criteria:
- Concurrency tests pass với >= 2 luồng chấm đồng thời.

#### Phase 3: Teaching Assignment Matrix (15h)

- Việc cần làm:
- Hoàn thiện model theo nghiệp vụ đã chốt:
- `Teacher.SubjectId` là môn cố định của giáo viên.
- `TeachingAssignment` chỉ map `TeacherId-ClassId` + `AssignmentType`.
- Enforce rule: giáo viên chủ nhiệm tối đa 1 lớp active.
- Triển khai `ITeacherAuthorizationService` để check:
- `teacher.SubjectId == exam.SubjectId`.
- teacher có assignment active với class.
- Áp data filtering cho `exams/students/gradebook/grading`.
- Đầu ra bắt buộc:
- Giáo viên chỉ thao tác đúng lớp được phân công và đúng môn dạy.
- Exit criteria:
- Access-control integration tests pass cho cả 3 vai trò: GVBM, GVCN, ADMIN.

#### Phase 4: Missing Features (34h)

- Việc cần làm:
- Retake policy: max attempts, cooldown, pass-retake policy.
- Late submission: grace window + penalty.
- Grading audit trail: lưu lịch sử thay đổi điểm.
- Notification khi publish kết quả.
- Violation tracking frontend + backend.
- Đầu ra bắt buộc:
- Tất cả feature có API + UI + test case nghiệp vụ.
- Exit criteria:
- PO/UAT checklist feature đạt 100%.

#### Phase 5: Reliability & Test Gate (18h)

- Việc cần làm:
- Unit tests cho service trọng yếu.
- Integration tests cho lifecycle exam và authorization.
- Load/concurrency tests cho submit/grade.
- Hardening logging, alert, retry policy.
- Đầu ra bắt buộc:
- Bộ test regression chạy xanh, không còn blocker.
- Exit criteria:
- Không còn bug mức Critical/High mở.

#### Phase 6: UAT & Cutover (6h)

- Việc cần làm:
- Chạy UAT với kịch bản thực tế trường học (GVBM/GVCN/STUDENT/ADMIN).
- Chuẩn bị deploy checklist, rollback runbook, migration runbook.
- Go-live theo cửa sổ bảo trì + monitoring 24h đầu.
- Đầu ra bắt buộc:
- Biên bản UAT pass + go-live approval.
- Exit criteria:
- Production smoke test pass sau deploy.

---

## TUẦN 1: FIX CÁC LỖI CRITICAL (53 giờ)

### FIX #1: Authorization Enforcement (12 giờ)

**Mục tiêu**: Enforce role-based access trên tất cả endpoints  
**Scope**: 28 controllers, ~25 endpoints cần sửa

#### Bước 1: Tạo Authorization Helper

Tạo file: `src/OnlineExamSystem.API/Extensions/AuthorizationExtensions.cs`

```csharp
using System.Security.Claims;

public static class AuthorizationExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("userId")?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : 0L;
    }

    public static (bool IsTeacher, long TeacherId) GetTeacherInfo(
        this ClaimsPrincipal user)
    {
        // Giả định có teacher_id claim
        var teacherIdClaim = user.FindFirst("teacher_id")?.Value;
        return (long.TryParse(teacherIdClaim, out var tid) && tid > 0, tid);
    }

    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value == role ||
               user.IsInRole(role);
    }
}
```

#### Bước 2: Sửa GradingController (12 endpoints)

File: `src/OnlineExamSystem.API/Controllers/GradingController.cs`

```csharp
[ApiController]
[Route("api/grading")]
[Authorize]
[Produces("application/json")]
[Tags("Grading")]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly IExamRepository _examRepository;
    private readonly ILogger<GradingController> _logger;

    // ... constructor ...

    // ✅ FIX 1: AutoGrade - Only TEACHER/ADMIN
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPost("auto-grade/{attemptId}")]
    public async Task<ActionResult<ResponseResult<List<GradingResultResponse>>>> 
        AutoGrade(long attemptId)
    {
        var userId = User.GetUserId();
        if (userId == 0) return Unauthorized();

        var (success, message, data) = await _gradingService.AutoGradeAttemptAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<GradingResultResponse>> { Success = true, Data = data });
    }

    // ✅ FIX 2: ManualGrade - Only exam's TEACHER
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPut("attempts/{attemptId}/questions/{questionId}")]
    public async Task<ActionResult<ResponseResult<GradingResultResponse>>> 
        ManualGrade(long attemptId, long questionId, [FromBody] ManualGradeRequest request)
    {
        var userId = User.GetUserId();
        
        // Verify: Is user the teacher of this exam?
        var attempt = await _gradingService.GetAttemptAsync(attemptId);
        if (attempt == null) return NotFound();
        
        var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
        if (exam?.CreatedBy != userId)
            return Forbid("You are not authorized to grade this exam");

        var (success, message, data) = await _gradingService.ManualGradeAsync(attemptId, questionId, request);
        return success ? Ok(new ResponseResult<GradingResultResponse> { Success = true, Data = data })
                       : BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = message });
    }

    // ✅ FIX 3: BatchGrade - Only exam's TEACHER
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPut("attempts/{attemptId}/batch-grade")]
    public async Task<ActionResult<ResponseResult<List<GradingResultResponse>>>> 
        BatchGrade(long attemptId, [FromBody] BatchGradeRequest request)
    {
        var userId = User.GetUserId();
        var attempt = await _gradingService.GetAttemptAsync(attemptId);
        if (attempt == null) return NotFound();
        
        var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
        if (exam?.CreatedBy != userId)
            return Forbid();

        var (success, message, data) = await _gradingService.BatchGradeAsync(attemptId, request, userId);
        return success ? Ok(new ResponseResult<List<GradingResultResponse>> { Success = true, Data = data })
                       : BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = message });
    }

    // ✅ FIX 4: PublishResult - Only exam's TEACHER
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPost("attempts/{attemptId}/publish")]
    public async Task<ActionResult<ResponseResult<PublishResultResponse>>> 
        Publish(long attemptId)
    {
        var userId = User.GetUserId();
        var attempt = await _gradingService.GetAttemptAsync(attemptId);
        if (attempt == null) return NotFound();
        
        var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
        if (exam?.CreatedBy != userId && !User.HasRole("ADMIN"))
            return Forbid();

        var (success, message, data) = await _gradingService.PublishAsync(attemptId);
        return success ? Ok(new ResponseResult<PublishResultResponse> { Success = true, Data = data })
                       : BadRequest(new ResponseResult<PublishResultResponse> { Success = false, Message = message });
    }

    // ✅ FIX 5: GetGradingView - TEACHER of exam OR STUDENT who took it
    [Authorize]
    [HttpGet("attempts/{attemptId}/view")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> 
        GetGradingView(long attemptId)
    {
        var userId = User.GetUserId();
        var (success, message, data) = await _gradingService.GetGradingViewAsync(attemptId);
        
        if (!success) return BadRequest(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        // Authorization: Check if user is teacher of exam OR the student
        var attempt = data;
        if (User.HasRole("TEACHER"))
        {
            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            if (exam?.CreatedBy != userId)
                return Forbid();
        }
        else if (User.HasRole("STUDENT"))
        {
            // Check if attempt belongs to this student
            var studentClaim = User.FindFirst("student_id")?.Value;
            if (!long.TryParse(studentClaim, out var studentId) || studentId != attempt.StudentId)
                return Forbid();
        }

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Data = data });
    }

    // ... Apply similar pattern to remaining endpoints ...
}
```

#### Bước 3: Kiểm tra Authorization ở Controllers khác

**Danh sách file cần kiểm tra**:

```
ExamsController
  - POST / (Create) → TEACHER
  - PUT {id} (Update) → TEACHER who created it
  - DELETE {id} → TEACHER or ADMIN
  
QuestionsController
  - POST / → TEACHER
  - PUT {id} → TEACHER who created it
  - DELETE {id} → TEACHER or ADMIN
  
AdminController
  - Tất cả endpoints → ADMIN only
  
StudentsController
  - GET → TEACHER, ADMIN
  - Create/Update → ADMIN only
  - DELETE → ADMIN only

TeachersController
  - POST / → ADMIN only
  - PUT {id} → ADMIN only
  - DELETE {id} → ADMIN only
```

---

### FIX #2: Answer Submission Race Condition (4 giờ)

**Mục tiêu**: Đảm bảo 1 question = 1 answer record  
**Vị trí**: Database + Service layer

#### Bước 1: Tạo Migration (Database)

File: `src/OnlineExamSystem.Infrastructure/Migrations/AddAnswerUniqueness.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing index if exists
            migrationBuilder.DropIndex(
                name: "ix_answers_exam_attempt_id",
                table: "answers");

            // Create unique index
            migrationBuilder.CreateIndex(
                name: "ix_answers_attempt_question_unique",
                table: "answers",
                columns: new[] { "exam_attempt_id", "question_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_answers_attempt_question_unique",
                table: "answers");

            migrationBuilder.CreateIndex(
                name: "ix_answers_exam_attempt_id",
                table: "answers",
                column: "exam_attempt_id");
        }
    }
}
```

#### Bước 2: Sửa AnswerService

File: `src/OnlineExamSystem.Infrastructure/Services/AnswerService.cs`

```csharp
public async Task<(bool Success, string Message, AnswerResponse? Data)> 
    SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request)
{
    try
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId);
        if (attempt == null)
            return (false, "Exam attempt not found", null);

        if (attempt.Status != "IN_PROGRESS")
            return (false, "Exam attempt is not in progress", null);

        // ✅ FIX: Use upsert pattern instead of check-then-insert
        var existing = await _answerRepository
            .GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);

        Answer answer;
        if (existing != null)
        {
            // Update existing
            existing.TextContent = request.TextContent;
            existing.EssayContent = request.EssayContent;
            existing.CanvasImage = request.CanvasImage;
            existing.AnsweredAt = DateTime.UtcNow;
            answer = existing;
            
            await _answerRepository.UpdateAsync(answer);
        }
        else
        {
            // Create new
            answer = new Answer
            {
                ExamAttemptId = attemptId,
                QuestionId = request.QuestionId,
                TextContent = request.TextContent,
                EssayContent = request.EssayContent,
                CanvasImage = request.CanvasImage,
                AnsweredAt = DateTime.UtcNow
            };
            
            await _answerRepository.CreateAsync(answer);
        }

        // If has selected options, handle them
        if (request.SelectedOptionIds?.Any() == true)
        {
            // Delete old options
            await _answerRepository.DeleteAnswerOptionsByAnswerIdAsync(answer.Id);
            
            // Insert new options
            foreach (var optionId in request.SelectedOptionIds)
            {
                await _answerRepository.CreateAnswerOptionAsync(new()
                {
                    AnswerId = answer.Id,
                    OptionId = optionId
                });
            }
        }

        return (true, "Answer saved", MapToResponse(answer));
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
    {
        // Race condition: Already saved by another request
        _logger.LogWarning(ex, "Duplicate answer detected for attempt {AttemptId}, question {QuestionId}", 
            attemptId, request.QuestionId);
        
        // Retry: Get the answer that was just created
        var answer = await _answerRepository
            .GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);
        
        if (answer != null)
            return (true, "Answer saved (by concurrent request)", MapToResponse(answer));
        
        return (false, "Conflict saving answer", null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting answer");
        return (false, $"Error: {ex.Message}", null);
    }
}
```

---

### FIX #3: Exam Time Window Enforcement (6 giờ)

**Mục tiêu**: Check exam timing trước khi allow submit  
**Vị trí**: ExamAttemptService.cs

#### Bước 1: Tạo Helper Method

```csharp
// ExamAttemptService.cs

private async Task<(bool Valid, string? ErrorMessage)> ValidateExamTimeWindowAsync(
    Exam exam, long studentId, DateTime currentTime)
{
    if (exam == null)
        return (false, "Exam not found");

    // Check 1: Exam chưa bắt đầu?
    if (exam.StartTime != default && currentTime < exam.StartTime)
        return (false, $"Exam starts at {exam.StartTime:yyyy-MM-dd HH:mm} UTC");

    // Check 2: Exam đã kết thúc?
    if (exam.EndTime != default && currentTime > exam.EndTime)
    {
        // Allow late submission nếu within grace period
        var minutesLate = (currentTime - exam.EndTime).TotalMinutes;
        if (!exam.AllowLateSubmission || minutesLate > exam.LateSubmissionMinutes)
            return (false, "Exam window closed");
    }

    // Check 3: Student có ở class được gán thi?
    var student = await _studentRepository.GetByIdAsync(studentId);
    if (student == null)
        return (false, "Student not found");

    var allowedClasses = await _examClassRepository.GetClassesByExamAsync(exam.Id);
    if (!allowedClasses.Any(c => c == student.ClassId))
        return (false, "You are not enrolled for this exam");

    return (true, null);
}
```

#### Bước 2: Sửa StartAttemptAsync

```csharp
public async Task<(bool Success, string Message, ExamAttemptResponse? Data)> 
    StartAttemptAsync(long examId, long studentId)
{
    try
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        var student = await _studentRepository.GetByIdAsync(studentId);

        if (exam == null) return (false, "Exam not found", null);
        if (student == null) return (false, "Student not found", null);

        // ✅ FIX: Validate time window & enrollment
        var (valid, errorMessage) = await ValidateExamTimeWindowAsync(
            exam, studentId, DateTime.UtcNow);
        
        if (!valid)
            return (false, errorMessage ?? "Cannot start exam", null);

        // Check max attempts
        if (exam.MaxAttemptsAllowed > 0)
        {
            var attemptCount = await _examAttemptRepository
                .CountByStudentAndExamAsync(studentId, examId);
            
            if (attemptCount >= exam.MaxAttemptsAllowed)
                return (false, "Maximum attempts exceeded", null);
        }

        // Check retake policy
        var lastAttempt = await _examAttemptRepository
            .GetLastAttemptByStudentAndExamAsync(studentId, examId);
        
        if (lastAttempt != null)
        {
            // Check 1: Already passed?
            if (lastAttempt.Score >= exam.PassingScore && !exam.AllowRetakeIfPassed)
                return (false, "You already passed, retake not allowed", null);

            // Check 2: Time between retakes
            if (exam.MinutesBetweenRetakes > 0)
            {
                var minutesSinceLast = (DateTime.UtcNow - (lastAttempt.EndTime ?? lastAttempt.StartTime))
                    .TotalMinutes;
                
                if (minutesSinceLast < exam.MinutesBetweenRetakes)
                    return (false, $"Must wait {Math.Ceiling(exam.MinutesBetweenRetakes - minutesSinceLast)} minutes before retaking", null);
            }
        }

        var attempt = new ExamAttempt
        {
            ExamId = examId,
            StudentId = studentId,
            StartTime = DateTime.UtcNow,
            Status = "IN_PROGRESS"
        };

        await _examAttemptRepository.CreateAsync(attempt);
        return (true, "Exam attempt started", MapToResponse(attempt, exam, student));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting exam attempt");
        return (false, $"Error: {ex.Message}", null);
    }
}
```

#### Bước 3: Sửa SubmitAttemptAsync

```csharp
public async Task<(bool Success, string Message, SubmitExamAttemptResponse? Data)> 
    SubmitAttemptAsync(long attemptId)
{
    try
    {
        var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
        if (attempt == null)
            return (false, "Exam attempt not found", null);

        if (attempt.Status != "IN_PROGRESS")
            return (false, "Attempt is not in progress", null);

        var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
        if (exam == null)
            return (false, "Exam not found", null);

        // ✅ FIX: Check time and duration
        var now = DateTime.UtcNow;
        var timeTaken = now - attempt.StartTime;

        // Check 1: Exam duration exceeded?
        if (timeTaken.TotalMinutes > exam.DurationMinutes)
        {
            // Allow if within grace period
            var minutesOver = timeTaken.TotalMinutes - exam.DurationMinutes;
            if (!exam.AllowLateSubmission || minutesOver > exam.LateSubmissionMinutes)
                return (false, $"Duration exceeded by {Math.Floor(minutesOver)} minutes", null);
        }

        // Check 2: Exam window closed?
        if (exam.EndTime != default && now > exam.EndTime)
        {
            var minutesLate = (now - exam.EndTime).TotalMinutes;
            if (!exam.AllowLateSubmission || minutesLate > exam.LateSubmissionMinutes)
                return (false, "Exam window closed", null);
            
            attempt.IsLateSubmission = true;
            attempt.MinutesLate = (int)minutesLate;
        }

        attempt.Status = "SUBMITTED";
        attempt.EndTime = now;
        await _examAttemptRepository.UpdateAsync(attempt);

        // Auto-grade MCQ/TRUE_FALSE
        var autoScore = await AutoGradeAttemptAsync(attempt);
        if (autoScore.HasValue)
        {
            attempt.Score = autoScore;
            await _examAttemptRepository.UpdateAsync(attempt);
        }

        await _activityLog.LogAsync(null, "EXAM_SUBMITTED", "ExamAttempt", attempt.Id,
            $"ExamId: {attempt.ExamId}, StudentId: {attempt.StudentId}, Late: {attempt.IsLateSubmission}");

        return (true, "Exam attempt submitted successfully", new SubmitExamAttemptResponse
        {
            AttemptId = attempt.Id,
            Status = attempt.Status,
            SubmittedAt = attempt.EndTime ?? DateTime.UtcNow,
            Message = attempt.IsLateSubmission 
                ? $"Submitted {attempt.MinutesLate} minutes late" 
                : "Your exam has been submitted"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting exam attempt");
        return (false, $"Error: {ex.Message}", null);
    }
}
```

---

### FIX #4: Enrollment Verification (3 giờ)

**Mục tiêu**: Check sinh viên ở lớp được gán thi  
**Vị trí**: ExamAttemptService.StartAttemptAsync (đã bao gồm ở FIX #3)

**Tóm tắt**: Đã thêm vào ValidateExamTimeWindowAsync ở FIX #3

---

### FIX #5: Database Cascade Delete (3 giờ)

**Mục tiêu**: Xóa exam → xóa tất cả con (attempts, answers, gradings)  
**Vị trí**: ApplicationDbContext.cs + Migration

#### Bước 1: Sửa ApplicationDbContext.OnModelCreating

File: `src/OnlineExamSystem.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...

    // Answer relationships
    modelBuilder.Entity<Answer>()
        .HasOne(a => a.ExamAttempt)
        .WithMany(ea => ea.Answers)
        .HasForeignKey(a => a.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD

    // GradingResult relationships
    modelBuilder.Entity<GradingResult>()
        .HasOne(gr => gr.ExamAttempt)
        .WithMany(ea => ea.GradingResults)
        .HasForeignKey(gr => gr.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD

    modelBuilder.Entity<GradingResult>()
        .HasOne(gr => gr.Question)
        .WithMany()
        .HasForeignKey(gr => gr.QuestionId)
        .OnDelete(DeleteBehavior.Restrict);  // Keep question even if grade deleted

    // ExamViolation relationships
    modelBuilder.Entity<ExamViolation>()
        .HasOne(ev => ev.ExamAttempt)
        .WithMany(ea => ea.ExamViolations)
        .HasForeignKey(ev => ev.ExamAttemptId)
        .OnDelete(DeleteBehavior.Cascade);  // ✅ ADD

    // ExamAttempt relationships
    modelBuilder.Entity<ExamAttempt>()
        .HasOne(ea => ea.Exam)
        .WithMany(e => e.ExamAttempts)
        .HasForeignKey(ea => ea.ExamId)
        .OnDelete(DeleteBehavior.Cascade);  // Keep existing

    modelBuilder.Entity<ExamAttempt>()
        .HasOne(ea => ea.Student)
        .WithMany()
        .HasForeignKey(ea => ea.StudentId)
        .OnDelete(DeleteBehavior.Restrict);

    // ... rest of configuration ...
}
```

#### Bước 2: Tạo Migration

```bash
# Terminal
cd src/OnlineExamSystem.Infrastructure
dotnet ef migrations add FixCascadeDeletes
dotnet ef database update
```

File migration sẽ auto-generate hoặc tạo tay:

```csharp
// FixCascadeDeletes.cs
public partial class FixCascadeDeletes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop old FK without cascade
        migrationBuilder.DropForeignKey(
            name: "fk_answers_exam_attempts",
            table: "answers");

        migrationBuilder.DropForeignKey(
            name: "fk_grading_results_exam_attempts",
            table: "grading_results");

        migrationBuilder.DropForeignKey(
            name: "fk_exam_violations_exam_attempts",
            table: "exam_violations");

        // Add new FK with cascade
        migrationBuilder.AddForeignKey(
            name: "fk_answers_exam_attempts",
            table: "answers",
            column: "exam_attempt_id",
            principalTable: "exam_attempts",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_grading_results_exam_attempts",
            table: "grading_results",
            column: "exam_attempt_id",
            principalTable: "exam_attempts",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_exam_violations_exam_attempts",
            table: "exam_violations",
            column: "exam_attempt_id",
            principalTable: "exam_attempts",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ... rollback logic ...
    }
}
```

---

### FIX #6: Optimistic Locking (8 giờ)

**Mục tiêu**: Prevent score loss khi concurrent grading  
**Vị trí**: AllEntities.cs + GradingService.cs

#### Bước 1: Thêm RowVersion vào ExamAttempt

File: `src/OnlineExamSystem.Domain/Entities/AllEntities.cs`

```csharp
public class ExamAttempt
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long StudentId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public bool IsResultPublished { get; set; }
    
    // ✅ ADD: Optimistic locking
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // ... navigation properties ...
}
```

#### Bước 2: Tạo Migration

```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef migrations add AddRowVersionToExamAttempt
dotnet ef database update
```

#### Bước 3: Sửa GradingService - BatchGradeAsync

```csharp
public async Task<(bool Success, string Message, List<GradingResultResponse>? Data)> 
    BatchGradeAsync(long attemptId, BatchGradeRequest request, long gradedBy)
{
    try
    {
        // ✅ FIX: Use transaction + version checking
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                await transaction.RollbackAsync();
                return (false, "Exam attempt not found", null);
            }

            var results = new List<GradingResultResponse>();
            decimal totalScore = 0m;

            // Grade each question
            foreach (var item in request.Grades)
            {
                var examQuestion = await _examQuestionRepo.GetExamQuestionAsync(
                    attempt.ExamId, item.QuestionId);
                
                if (examQuestion == null) continue;

                if (item.Score < 0 || item.Score > examQuestion.MaxScore)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Invalid score for question {item.QuestionId}", null);
                }

                // Create or update grading result
                var existing = await _gradingRepo.GetByAttemptAndQuestionAsync(
                    attemptId, item.QuestionId);

                if (existing == null)
                {
                    existing = await _gradingRepo.CreateAsync(new GradingResult
                    {
                        ExamAttemptId = attemptId,
                        QuestionId = item.QuestionId,
                        Score = item.Score,
                        GradedAt = DateTime.UtcNow,
                        GradedByUserId = gradedBy,
                        Comments = item.Comment
                    });
                }
                else
                {
                    existing.Score = item.Score;
                    existing.GradedAt = DateTime.UtcNow;
                    existing.GradedByUserId = gradedBy;
                    existing.Comments = item.Comment;
                    existing = await _gradingRepo.UpdateAsync(existing);
                }

                totalScore += item.Score;
                results.Add(MapToResponse(existing));
            }

            // Update total score
            attempt.Score = totalScore;
            
            // ✅ KEY: This will throw DbUpdateConcurrencyException if another request modified
            await _attemptRepo.UpdateAsync(attempt);
            await transaction.CommitAsync();

            _logger.LogInformation("Batch graded attempt {AttemptId} by user {UserId}", attemptId, gradedBy);
            return (true, "Graded successfully", results);
        }
    }
    catch (DbUpdateConcurrencyException)
    {
        _logger.LogWarning("{AttemptId} was modified by another request", attemptId);
        return (false, "This attempt was modified by another user. Please reload and try again.", null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error batch grading attempt {AttemptId}", attemptId);
        return (false, $"Error: {ex.Message}", null);
    }
}
```

#### Bước 4: Sửa GradingService - ManualGradeAsync

```csharp
public async Task<(bool Success, string Message, GradingResultResponse? Data)> 
    ManualGradeAsync(long attemptId, long questionId, ManualGradeRequest request, long gradedBy)
{
    try
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                await transaction.RollbackAsync();
                return (false, "Attempt not found", null);
            }

            var examQuestion = await _examQuestionRepo.GetExamQuestionAsync(attempt.ExamId, questionId);
            if (examQuestion == null)
            {
                await transaction.RollbackAsync();
                return (false, "Question not found", null);
            }

            if (request.Score < 0 || request.Score > examQuestion.MaxScore)
            {
                await transaction.RollbackAsync();
                return (false, $"Score must be between 0 and {examQuestion.MaxScore}", null);
            }

            // Create or update grading
            var grading = await _gradingRepo.GetByAttemptAndQuestionAsync(attemptId, questionId);
            
            if (grading == null)
            {
                grading = await _gradingRepo.CreateAsync(new GradingResult
                {
                    ExamAttemptId = attemptId,
                    QuestionId = questionId,
                    Score = request.Score,
                    GradedAt = DateTime.UtcNow,
                    GradedByUserId = gradedBy,
                    Comments = request.Comment
                });
            }
            else
            {
                grading.Score = request.Score;
                grading.GradedAt = DateTime.UtcNow;
                grading.GradedByUserId = gradedBy;
                grading.Comments = request.Comment;
                grading = await _gradingRepo.UpdateAsync(grading);
            }

            // Recalculate total score
            var allGradings = await _gradingRepo.GetByAttemptIdAsync(attemptId);
            attempt.Score = allGradings.Sum(g => g.Score);

            // ✅ Will detect concurrent modifications
            await _attemptRepo.UpdateAsync(attempt);
            await transaction.CommitAsync();

            return (true, "Graded successfully", MapToResponse(grading));
        }
    }
    catch (DbUpdateConcurrencyException)
    {
        _logger.LogWarning("Grading conflict for attempt {AttemptId}", attemptId);
        return (false, "This attempt was modified by another user", null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error grading attempt");
        return (false, $"Error: {ex.Message}", null);
    }
}
```

---

### FIX #7: Teaching Assignment Authorization (15 giờ)

**Mục tiêu**: Enforce quyền giáo viên theo phân công giảng dạy  
**Scope**: Schema + 4 authorization services + 15 controller endpoints  
**Vấn đề**: Teacher chỉ track 1 subject, không thể dạy nhiều lớp/môn; không verify giáo viên có quyền tạo/sửa bài thi

#### Bước 1: Tạo TeachingAssignment Entity

File: `src/OnlineExamSystem.Domain/Entities/AllEntities.cs`

```csharp
public class TeachingAssignment
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public long TeacherId { get; set; }  // ✅ Chỉ cần TeacherId, SubjectId lấy từ Teacher.SubjectId
    
    /// <summary>
    /// SUBJECT_TEACHER: Giáo viên bộ môn (dạy môn của mình cho lớp này)
    /// HOMEROOM_TEACHER: Giáo viên chủ nhiệm (quản lý tất cả lớp)
    /// 
    /// Lưu ý:
    /// - 1 giáo viên = chủ nhiệm tối đa 1 lớp
    /// - 1 giáo viên = 1 môn cố định (lấy từ Teacher.SubjectId)
    /// - Nhưng có thể dạy cùng môn cho nhiều lớp (multiple SUBJECT_TEACHER assignments)
    /// </summary>
    public string AssignmentType { get; set; } = "SUBJECT_TEACHER";
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnassignedAt { get; set; } = null;  // null = hiện tại còn dạy
    
    // Navigation
    public virtual Class Class { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
}

// Update: Class.cs
public class Class
{
    // ... existing ...
    public long? HomeroomTeacherId { get; set; }  // ✅ NEW: Track explicitly (GV chủ nhiệm)
    
    public virtual Teacher? HomeroomTeacher { get; set; }
    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
}

// Update: Teacher.cs - KEEP SubjectId (1 giáo viên = 1 môn)
public class Teacher
{
    public long Id { get; set; }
    public long SubjectId { get; set; }  // ✅ KEEP - 1 GV chỉ dạy 1 môn cố định
    public long SchoolId { get; set; }
    
    public virtual Subject Subject { get; set; } = null!;
    public virtual School School { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<TeachingAssignment> Assignments { get; set; } = new List<TeachingAssignment>();
}
```

#### Bước 2: Tạo Migration

```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef migrations add CreateTeachingAssignmentEntity
```

Content:

```csharp
// Migrations/[timestamp]_CreateTeachingAssignmentEntity.cs
public partial class CreateTeachingAssignmentEntity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create TeachingAssignment table
        // Note: SubjectId không cần vì lấy từ Teacher.SubjectId (1 GV = 1 môn)
        migrationBuilder.CreateTable(
            name: "teaching_assignments",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", 
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                class_id = table.Column<long>(type: "bigint", nullable: false),
                teacher_id = table.Column<long>(type: "bigint", nullable: false),
                assignment_type = table.Column<string>(type: "character varying(50)", 
                    nullable: false, defaultValue: "SUBJECT_TEACHER"),
                assigned_at = table.Column<DateTime>(type: "timestamp with time zone", 
                    nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                unassigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_teaching_assignments", x => x.id);
                table.ForeignKey(name: "fk_teaching_assignments_class",
                    column: x => x.class_id,
                    principalTable: "classes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "fk_teaching_assignments_teacher",
                    column: x => x.teacher_id,
                    principalTable: "teachers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create unique index: 1 teacher = max 1 HOMEROOM_TEACHER assignment per class
        // (for SUBJECT_TEACHER, multiple are allowed: same teacher can teach same subject to many classes)
        migrationBuilder.CreateIndex(
            name: "ix_teaching_assignments_homeroom_unique",
            table: "teaching_assignments",
            columns: new[] { "teacher_id", "assignment_type" },
            unique: true,
            filter: "assignment_type = 'HOMEROOM_TEACHER' AND unassigned_at IS NULL");

        // Create index for faster queries
        migrationBuilder.CreateIndex(
            name: "ix_teaching_assignments_class_teacher",
            table: "teaching_assignments",
            columns: new[] { "class_id", "teacher_id" });

        // Add HomeroomTeacherId to Class for explicit tracking
        migrationBuilder.AddColumn<long>(
            name: "homeroom_teacher_id",
            table: "classes",
            type: "bigint",
            nullable: true);

        // Add FK for HomeroomTeacher
        migrationBuilder.AddForeignKey(
            name: "fk_class_homeroom_teacher",
            table: "classes",
            column: "homeroom_teacher_id",
            principalTable: "teachers",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        // ✅ IMPORTANT: Keep Teacher.SubjectId - 1 GV = 1 môn cố định
        // No deletion of Teacher.subject_id column
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_class_homeroom_teacher",
            table: "classes");

        migrationBuilder.DropTable(name: "teaching_assignments");
        
        migrationBuilder.DropColumn(name: "homeroom_teacher_id", table: "classes");
    }
}
```

#### Bước 3: Tạo Authorization Service Interface

File: `src/OnlineExamSystem.Application/Interfaces/ITeacherAuthorizationService.cs`

```csharp
public interface ITeacherAuthorizationService
{
    /// <summary>
    /// Check if teacher can teach a specific class-subject combination
    /// 
    /// Logic:
    /// 1. Get teacher's SubjectId (1 GV = 1 môn cố định)
    /// 2. Check if exam's SubjectId == teacher's SubjectId
    /// 3. Check if teacher has SUBJECT_TEACHER or HOMEROOM_TEACHER assignment to ClassId
    /// 
    /// Returns: true nếu:
    /// - GV dạy môn của exam (teacher.subjectId == exam.subjectId), AND
    /// - GV được giao dạy lớp này hoặc là GVCN của lớp (active assignment)
    /// </summary>
    Task<bool> CanTeachClassAsync(long teacherId, long classId, long examSubjectId);
    
    /// <summary>
    /// Get all class IDs that teacher can access (assigned or is homeroom for)
    /// </summary>
    Task<List<long>> GetTeacherAccessibleClassesAsync(long teacherId);
    
    /// <summary>
    /// Get all ClassId mà teacher dạy (theo teacher's fixed subject)
    /// </summary>
    Task<List<long>> GetTeacherTaughtClassesAsync(long teacherId);
    
    /// <summary>
    /// Check if teacher is homeroom teacher (GVCN) of a class
    /// </summary>
    Task<bool> IsHomeroomTeacherAsync(long teacherId, long classId);
    
    /// <summary>
    /// Get teacher's fixed subject (1 GV = 1 môn)
    /// </summary>
    Task<long?> GetTeacherSubjectAsync(long teacherId);
}
```


    
    /// <summary>
    /// Get all TeachingAssignment records for a teacher (active only)
    /// </summary>
    Task<List<TeachingAssignment>> GetTeacherAssignmentDetailsAsync(long teacherId);
}
```

#### Bước 4: Implement Authorization Service

File: `src/OnlineExamSystem.Infrastructure/Services/TeacherAuthorizationService.cs`

```csharp
public class TeacherAuthorizationService : ITeacherAuthorizationService
{
    private readonly ITeachingAssignmentRepository _assignmentRepo;
    private readonly ITeacherRepository _teacherRepo;
    private readonly ILogger<TeacherAuthorizationService> _logger;

    public TeacherAuthorizationService(
        ITeachingAssignmentRepository assignmentRepo,
        ITeacherRepository teacherRepo,
        ILogger<TeacherAuthorizationService> logger)
    {
        _assignmentRepo = assignmentRepo;
        _teacherRepo = teacherRepo;
        _logger = logger;
    }

    /// <summary>
    /// Check if teacher can teach exam for a specific class
    /// 
    /// Logic:
    /// 1. Get teacher's fixed SubjectId (1 GV = 1 môn)
    /// 2. If exam.SubjectId != teacher.SubjectId => Reject (không dạy môn này)
    /// 3. Check TeachingAssignment: teacher được giao lớp này không?
    ///    - SUBJECT_TEACHER assignment exists? (primary)
    ///    - or HOMEROOM_TEACHER assignment exists? (can teach any subject in their class)
    /// </summary>
    public async Task<bool> CanTeachClassAsync(long teacherId, long classId, long examSubjectId)
    {
        // Step 1: Get teacher's fixed subject
        var teacher = await _teacherRepo.GetByIdAsync(teacherId);
        if (teacher == null)
        {
            _logger.LogWarning("Teacher {TeacherId} not found", teacherId);
            return false;
        }

        // Step 2: Check if teacher teaches the exam's subject
        if (teacher.SubjectId != examSubjectId)
        {
            _logger.LogWarning(
                "Teacher {TeacherId} (teaches subject {TeacherSubject}) cannot teach exam of subject {ExamSubject}",
                teacherId, teacher.SubjectId, examSubjectId);
            return false;
        }

        // Step 3: Check if teacher is assigned to this class
        var assignment = await _assignmentRepo.GetByClassAndTeacherAsync(classId, teacherId);
        
        if (assignment == null || assignment.UnassignedAt != null)
        {
            _logger.LogWarning(
                "Teacher {TeacherId} not assigned to Class {ClassId}",
                teacherId, classId);
            return false;
        }

        _logger.LogInformation(
            "Teacher {TeacherId} authorized to teach Class {ClassId}, Subject {ExamSubject}",
            teacherId, classId, examSubjectId);
        return true;
    }

    public async Task<List<long>> GetTeacherAccessibleClassesAsync(long teacherId)
    {
        var assignments = await _assignmentRepo.GetByTeacherIdAsync(teacherId, activeOnly: true);
        return assignments
            .Select(a => a.ClassId)
            .Distinct()
            .ToList();
    }

    public async Task<List<long>> GetTeacherTaughtClassesAsync(long teacherId)
    {
        // Same as GetTeacherAccessibleClassesAsync (all active assignments)
        return await GetTeacherAccessibleClassesAsync(teacherId);
    }

    public async Task<bool> IsHomeroomTeacherAsync(long teacherId, long classId)
    {
        var assignment = await _assignmentRepo.GetByClassAndTeacherAsync(classId, teacherId);
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

#### Bước 5: Tạo Repository

File: `src/OnlineExamSystem.Infrastructure/Repositories/TeachingAssignmentRepository.cs`

```csharp
public interface ITeachingAssignmentRepository
{
    Task<TeachingAssignment> CreateAsync(TeachingAssignment assignment);
    Task<TeachingAssignment> UpdateAsync(TeachingAssignment assignment);
    Task DeleteAsync(long id);
    Task<TeachingAssignment?> GetByIdAsync(long id);
    
    /// <summary>
    /// Get assignment by ClassId + TeacherId (any type, active or not)
    /// </summary>
    Task<TeachingAssignment?> GetByClassAndTeacherAsync(long classId, long teacherId);
    
    /// <summary>
    /// Get all assignments for a teacher
    /// </summary>
    Task<List<TeachingAssignment>> GetByTeacherIdAsync(long teacherId, bool activeOnly = true);
    
    /// <summary>
    /// Get all assignments for a class
    /// </summary>
    Task<List<TeachingAssignment>> GetByClassIdAsync(long classId, bool activeOnly = true);
}

public class TeachingAssignmentRepository : ITeachingAssignmentRepository
{
    private readonly ApplicationDbContext _context;

    public TeachingAssignmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TeachingAssignment?> GetByClassAndTeacherAsync(long classId, long teacherId)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Teacher)
            .FirstOrDefaultAsync(ta => ta.ClassId == classId && ta.TeacherId == teacherId);
    }

    public async Task<List<TeachingAssignment>> GetByTeacherIdAsync(long teacherId, bool activeOnly = true)
    {
        var query = _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Teacher)
            .Where(ta => ta.TeacherId == teacherId);
        
        if (activeOnly)
            query = query.Where(ta => ta.UnassignedAt == null);
        
        return await query.ToListAsync();
    }

    public async Task<List<TeachingAssignment>> GetByClassIdAsync(long classId, bool activeOnly = true)
    {
        var query = _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Teacher)
            .Where(ta => ta.ClassId == classId);
        
        if (activeOnly)
            query = query.Where(ta => ta.UnassignedAt == null);
        
        return await query
            .OrderBy(ta => ta.AssignmentType)  // HOMEROOM_TEACHER first
            .ToListAsync();
    }

    // CRUD methods
    public async Task<TeachingAssignment> CreateAsync(TeachingAssignment assignment)
    {
        assignment.AssignedAt = DateTime.UtcNow;
        _context.TeachingAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<TeachingAssignment> UpdateAsync(TeachingAssignment assignment)
    {
        _context.TeachingAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task DeleteAsync(long id)
    {
        var assignment = await _context.TeachingAssignments.FindAsync(id);
        if (assignment != null)
        {
            _context.TeachingAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TeachingAssignment?> GetByIdAsync(long id)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Teacher)
            .FirstOrDefaultAsync(ta => ta.Id == id);
    }
}
```


    }

    public async Task<List<TeachingAssignment>> GetByClassIdAsync(long classId, bool activeOnly = true)
    {
        var query = _context.TeachingAssignments
            .Include(ta => ta.Teacher)
            .Include(ta => ta.Subject)
            .Where(ta => ta.ClassId == classId);
        
        if (activeOnly)
            query = query.Where(ta => ta.UnassignedAt == null);
        
        return await query.ToListAsync();
    }

    public async Task<List<TeachingAssignment>> GetBySubjectIdAsync(long subjectId, bool activeOnly = true)
    {
        var query = _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Teacher)
            .Where(ta => ta.SubjectId == subjectId);
        
        if (activeOnly)
            query = query.Where(ta => ta.UnassignedAt == null);
        
        return await query.ToListAsync();
    }

    // CRUD methods
    public async Task<TeachingAssignment> CreateAsync(TeachingAssignment assignment)
    {
        assignment.AssignedAt = DateTime.UtcNow;
        _context.TeachingAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<TeachingAssignment> UpdateAsync(TeachingAssignment assignment)
    {
        _context.TeachingAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task DeleteAsync(long id)
    {
        var assignment = await _context.TeachingAssignments.FindAsync(id);
        if (assignment != null)
        {
            _context.TeachingAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TeachingAssignment?> GetByIdAsync(long id)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Class)
            .Include(ta => ta.Subject)
            .Include(ta => ta.Teacher)
            .FirstOrDefaultAsync(ta => ta.Id == id);
    }
}
```

#### Bước 6: Register Services (Dependency Injection)

File: `src/OnlineExamSystem.Infrastructure/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ... existing registrations ...
        
        // ✅ ADD: Teaching Assignment services
        services.AddScoped<ITeachingAssignmentRepository, TeachingAssignmentRepository>();
        services.AddScoped<ITeacherAuthorizationService, TeacherAuthorizationService>();
        
        return services;
    }
}
```

#### Bước 7: Update Controllers - ExamsController

File: `src/OnlineExamSystem.API/Controllers/ExamsController.cs`

```csharp
[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ITeacherAuthorizationService _authService;
    private readonly ITeacherRepository _teacherRepo;
    private readonly ILogger<ExamsController> _logger;

    // ... constructor ...

    // ✅ FIX: Create exam - verify teaching assignment
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPost]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> CreateExam(
        [FromBody] CreateExamRequest request)
    {
        var userId = User.GetUserId();
        
        // Get teacher ID
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        if (teacher == null)
            return Forbid("User is not a teacher");

        // ✅ NEW: Verify teaching assignment
        // Check: Teacher dạy lớp này không? + Môn của teacher match môn của exam không?
        var canTeach = await _authService.CanTeachClassAsync(
            teacher.Id, request.ClassId, request.SubjectId);
        
        if (!canTeach)
            return Forbid($"You are not assigned to teach this class, or you don't teach this subject");

        var (success, message, data) = await _examService.CreateExamAsync(
            request, teacher.Id);
        
        return success ? Ok(new ResponseResult<ExamResponse> { Success = true, Data = data })
                       : BadRequest(new ResponseResult<ExamResponse> { Success = false, Message = message });
    }

    // ✅ FIX: Get my exams - filter by teaching assignments
    [Authorize(Roles = "TEACHER")]
    [HttpGet("my-exams")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetMyExams()
    {
        var userId = User.GetUserId();
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        
        if (teacher == null)
            return NotFound("Teacher not found");

        // Get all classes teacher has access to (được giao dạy hoặc là GVCN)
        var accessibleClasses = await _authService.GetTeacherAccessibleClassesAsync(teacher.Id);
        
        var exams = await _examService.GetExamsByClassesAsync(accessibleClasses);
        var response = exams.Select(MapToResponse).ToList();
        
        return Ok(new ResponseResult<List<ExamResponse>> { Success = true, Data = response });
    }

    // ✅ FIX: Update exam - verify authorization
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> UpdateExam(
        long id, [FromBody] UpdateExamRequest request)
    {
        var userId = User.GetUserId();
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        
        var (success, message, exam) = await _examService.GetExamAsync(id);
        if (!success || exam == null)
            return NotFound();

        // ✅ NEW: Check authorization
        // Only exam creator (teacher) or admin can edit
        if (exam.CreatedByTeacherId != teacher?.Id && !User.HasRole("ADMIN"))
            return Forbid("Not authorized to edit this exam");

        // If trying to change subject/class, verify new assignment
        if (exam.SubjectId != request.SubjectId || exam.ClassId != request.ClassId)
        {
            var canTeach = await _authService.CanTeachClassAsync(
                teacher!.Id, request.ClassId, request.SubjectId);
            
            if (!canTeach)
                return Forbid("You are not assigned to teach the new class, or you don't teach this subject");
        }

        var updateResult = await _examService.UpdateExamAsync(id, request);
        return updateResult.Success ? Ok(...) : BadRequest(...);
    }
}
```

#### Bước 8: Update Controllers - GradingController

```csharp
// GradingController.cs - Add authorization check

[ApiController]
[Route("api/grading")]
[Authorize]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly ITeacherAuthorizationService _authService;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IExamRepository _examRepo;

    // ✅ FIX: Only exam creator OR homeroom teacher OR subject teacher can grade
    [Authorize(Roles = "TEACHER,ADMIN")]
    [HttpPut("attempts/{attemptId}/questions/{questionId}")]
    public async Task<ActionResult<ResponseResult<GradingResultResponse>>> ManualGrade(
        long attemptId, long questionId, [FromBody] ManualGradeRequest request)
    {
        var userId = User.GetUserId();
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        
        var attempt = await _gradingService.GetAttemptAsync(attemptId);
        if (attempt == null) return NotFound();
        
        var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
        if (exam == null) return NotFound();

        // ✅ NEW: Check authorization
        var isCreator = exam.CreatedByTeacherId == teacher?.Id;
        var isHomeroom = await _authService.IsHomeroomTeacherAsync(teacher!.Id, exam.ClassId);
        
        // Can grade if: exam creator OR homeroom teacher OR teaches this subject for this class
        var canGrade = isCreator || isHomeroom || User.HasRole("ADMIN");
        
        // For subject teachers: Must both teach this subject AND be assigned to this class
        if (!canGrade && teacher != null)
        {
            canGrade = await _authService.CanTeachClassAsync(
                teacher.Id, exam.ClassId, exam.SubjectId);
        }

        if (!canGrade)
            return Forbid("Not authorized to grade this exam");

        var (success, message, data) = await _gradingService.ManualGradeAsync(
            attemptId, questionId, request, teacher.Id);
        
        return success ? Ok(...) : BadRequest(...);
    }
}
```

#### Bước 9: Update Controllers - StudentsController

```csharp
// StudentsController.cs - Authorize class access

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ITeacherAuthorizationService _authService;
    private readonly ITeacherRepository _teacherRepo;

    // ✅ FIX: Teacher can only see students of their assigned classes
    [Authorize(Roles = "TEACHER")]
    [HttpGet("class/{classId}")]
    public async Task<ActionResult<ResponseResult<List<StudentResponse>>>> GetClassStudents(long classId)
    {
        var userId = User.GetUserId();
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        
        if (teacher == null) return NotFound("Teacher not found");

        // ✅ NEW: Verify class access
        var accessibleClasses = await _authService.GetTeacherAccessibleClassesAsync(teacher.Id);
        if (!accessibleClasses.Contains(classId))
            return Forbid("You are not assigned to this class");

        var students = await _studentService.GetStudentsByClassAsync(classId);
        var response = students.Select(MapToResponse).ToList();
        
        return Ok(new ResponseResult<List<StudentResponse>> { Success = true, Data = response });
    }
}
```

#### Bước 10: Update Controllers - GradebookController

```csharp
// GradebookController.cs - Filter by authorization

[ApiController]
[Route("api/gradebook")]
[Authorize]
public class GradebookController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly ITeacherAuthorizationService _authService;
    private readonly ITeacherRepository _teacherRepo;

    // ✅ FIX: Teacher can only view gradebook of their classes
    [Authorize(Roles = "TEACHER")]
    [HttpGet("classes/{classId}")]
    public async Task<ActionResult<ResponseResult<ClassGradebookResponse>>> GetClassGradebook(long classId)
    {
        var userId = User.GetUserId();
        var teacher = await _teacherRepo.GetByUserIdAsync(userId);
        
        if (teacher == null) return NotFound("Teacher not found");

        // ✅ NEW: Verify access
        var accessibleClasses = await _authService.GetTeacherAccessibleClassesAsync(teacher.Id);
        if (!accessibleClasses.Contains(classId))
            return Forbid("You are not authorized to view this class");

        var gradebook = await _gradingService.GetClassGradebookAsync(classId);
        return Ok(new ResponseResult<ClassGradebookResponse> { Success = true, Data = gradebook });
    }
}
```

#### Bước 11: Seed Data - Tạo Teaching Assignments

File: `src/OnlineExamSystem.Infrastructure/Data/DbContextSeed.cs`

```csharp
public static async Task SeedTeachingAssignmentsAsync(ApplicationDbContext context)
{
    if (await context.TeachingAssignments.AnyAsync())
        return;

    // Example: 
    // - Giáo viên Hùng (ID=1, SubjectId=1=Toán):
    //   * Dạy lớp 10A (SUBJECT_TEACHER)
    //   * Dạy lớp 10B (SUBJECT_TEACHER)
    //   * Chủ nhiệm lớp 10A (HOMEROOM_TEACHER)
    //
    // - Giáo viên Linh (ID=2, SubjectId=2=Lý):
    //   * Dạy lớp 10A (SUBJECT_TEACHER)
    //   * Dạy lớp 11A (SUBJECT_TEACHER)
    //   * Chủ nhiệm lớp 11A (HOMEROOM_TEACHER)

    var assignments = new[]
    {
        // Hùng (GV Toán + GVCN 10A)
        new TeachingAssignment { TeacherId = 1, ClassId = 1, AssignmentType = "SUBJECT_TEACHER" },  // Dạy 10A
        new TeachingAssignment { TeacherId = 1, ClassId = 2, AssignmentType = "SUBJECT_TEACHER" },  // Dạy 10B
        new TeachingAssignment { TeacherId = 1, ClassId = 1, AssignmentType = "HOMEROOM_TEACHER" }, // GVCN 10A
        
        // Linh (GV Lý + GVCN 11A)
        new TeachingAssignment { TeacherId = 2, ClassId = 1, AssignmentType = "SUBJECT_TEACHER" },  // Dạy 10A
        new TeachingAssignment { TeacherId = 2, ClassId = 3, AssignmentType = "SUBJECT_TEACHER" },  // Dạy 11A
        new TeachingAssignment { TeacherId = 2, ClassId = 3, AssignmentType = "HOMEROOM_TEACHER" }, // GVCN 11A
    };

    context.TeachingAssignments.AddRange(assignments);
    await context.SaveChangesAsync();
}
```

#### Bước 12: Unit Tests

File: `tests/OnlineExamSystem.Tests/TeacherAuthorizationServiceTests.cs`

```csharp
[TestFixture]
public class TeacherAuthorizationServiceTests
{
    private ITeacherAuthorizationService _service;
    private Mock<ITeachingAssignmentRepository> _assignRepoMock;
    private Mock<ITeacherRepository> _teacherRepoMock;

    [SetUp]
    public void Setup()
    {
        _assignRepoMock = new Mock<ITeachingAssignmentRepository>();
        _teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<TeacherAuthorizationService>>();
        
        _service = new TeacherAuthorizationService(
            _assignRepoMock.Object,
            _teacherRepoMock.Object,
            loggerMock.Object);
    }

    [Test]
    public async Task CanTeachClass_ReturnTrue_WhenTeacherAssignedAndSubjectMatches()
    {
        // Arrange: GV dạy Toán (SubjectId=1), được giao lớp 10A
        var teacher = new Teacher { Id = 1, SubjectId = 1 }; // Toán
        var assignment = new TeachingAssignment
        {
            TeacherId = 1,
            ClassId = 10,
            AssignmentType = "SUBJECT_TEACHER",
            UnassignedAt = null  // Active
        };
        
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(teacher);
        
        _assignRepoMock.Setup(r => r.GetByClassAndTeacherAsync(10, 1))
            .ReturnsAsync(assignment);

        // Act: Check if GV can teach Toán (SubjectId=1) for lớp 10A
        var result = await _service.CanTeachClassAsync(1, 10, 1);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public async Task CanTeachClass_ReturnFalse_WhenSubjectDoesNotMatch()
    {
        // Arrange: GV dạy Toán (SubjectId=1), nhưng exam là Lý (SubjectId=2)
        var teacher = new Teacher { Id = 1, SubjectId = 1 }; // Toán
        
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(teacher);

        // Act: Check if GV can teach Lý (SubjectId=2)
        var result = await _service.CanTeachClassAsync(1, 10, 2);  // 2 = Lý

        // Assert: Subject mismatch => False
        Assert.IsFalse(result);
    }

    [Test]
    public async Task CanTeachClass_ReturnFalse_WhenNotAssignedToClass()
    {
        // Arrange: GV dạy Toán, nhưng không được giao lớp 10C
        var teacher = new Teacher { Id = 1, SubjectId = 1 }; // Toán
        
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(teacher);
        
        _assignRepoMock.Setup(r => r.GetByClassAndTeacherAsync(10, 1))
            .ReturnsAsync((TeachingAssignment?)null);  // No assignment

        // Act
        var result = await _service.CanTeachClassAsync(1, 10, 1);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task IsHomeroomTeacher_ReturnTrue_WhenTeacherIsHomeroomTeacher()
    {
        // Arrange
        var assignment = new TeachingAssignment
        {
            TeacherId = 1,
            ClassId = 10,
            AssignmentType = "HOMEROOM_TEACHER",
            UnassignedAt = null
        };
        
        _assignRepoMock.Setup(r => r.GetByClassAndTeacherAsync(10, 1))
            .ReturnsAsync(assignment);

        // Act
        var result = await _service.IsHomeroomTeacherAsync(1, 10);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public async Task GetTeacherAccessibleClasses_ReturnDistinctClassIds()
    {
        // Arrange: GV dạy 2 lớp (1 lần SUBJECT_TEACHER, 1 lần HOMEROOM_TEACHER)
        var assignments = new[]
        {
            new TeachingAssignment { ClassId = 10, AssignmentType = "SUBJECT_TEACHER" },
            new TeachingAssignment { ClassId = 11, AssignmentType = "SUBJECT_TEACHER" },
            new TeachingAssignment { ClassId = 10, AssignmentType = "HOMEROOM_TEACHER" }, // Duplicate class
        };
        
        _assignRepoMock.Setup(r => r.GetByTeacherIdAsync(1, true))
            .ReturnsAsync(assignments.ToList());

        // Act
        var result = await _service.GetTeacherAccessibleClassesAsync(1);

        // Assert: Should return distinct classes: [10, 11]
        Assert.AreEqual(2, result.Count);
        Assert.Contains(10, result);
        Assert.Contains(11, result);
    }
}
```

---

**FIX #7 Summary**:
- ✅ Create TeachingAssignment entity + migration
- ✅ Create ITeacherAuthorizationService + implementation
- ✅ Create ITeachingAssignmentRepository
- ✅ Register in dependency injection
- ✅ Update 4 controllers: ExamsController, GradingController, StudentsController, GradebookController
- ✅ Add 5+ authorization checks across 15+ endpoints
- ✅ Seed data + unit tests

**Effort: ~15 giờ** (Schema 2h + Service 4h + Repository 2h + Controllers 5h + Tests 2h)

---

## TUẦN 2: TÍNH NĂNG THIẾU (34 giờ)

### FEATURE #1: Retake Policy (8 giờ)

#### Bước 1: Update entities

```csharp
// AllEntities.cs - Exam class
public class Exam
{
    // ... existing fields ...
    
    // ✅ ADD:
    public int MaxAttemptsAllowed { get; set; } = 1;
    public int MinutesBetweenRetakes { get; set; } = 0;  // 0 = no waiting
    public bool AllowRetakeIfPassed { get; set; } = false;
    public decimal? RetakePassingScore { get; set; } = null;
}
```

#### Bước 2: Add repository methods

```csharp
// IExamAttemptRepository
public interface IExamAttemptRepository
{
    // ... existing methods ...
    
    Task<int> CountByStudentAndExamAsync(long studentId, long examId);
    Task<ExamAttempt?> GetLastAttemptByStudentAndExamAsync(long studentId, long examId);
}

// ExamAttemptRepository
public async Task<int> CountByStudentAndExamAsync(long studentId, long examId)
{
    return await _context.ExamAttempts
        .Where(a => a.StudentId == studentId && a.ExamId == examId)
        .CountAsync();
}

public async Task<ExamAttempt?> GetLastAttemptByStudentAndExamAsync(long studentId, long examId)
{
    return await _context.ExamAttempts
        .Where(a => a.StudentId == studentId && a.ExamId == examId)
        .OrderByDescending(a => a.EndTime ?? a.StartTime)
        .FirstOrDefaultAsync();
}
```

#### Bước 3: Service logic (Added in FIX #3)

Đã thêm logic retake vào `ExamAttemptService.StartAttemptAsync()`

#### Bước 4: Frontend logic

```typescript
// frontend/src/pages/ExamsPage.tsx - StudentExamView

const canRetake = async (exam: ExamResponse, studentAttempt: AttemptInfo | undefined) => {
    if (!studentAttempt) return true; // Haven't attempted yet
    
    if (studentAttempt.status !== 'GRADED' && studentAttempt.status !== 'SUBMITTED')
        return false; // Still in progress
    
    if ((exam.maxAttemptsAllowed ?? 1) <= 1)
        return false; // Only 1 attempt allowed
    
    const attemptCount = studentAttempts.filter(a => a.examId === exam.id).length;
    if (attemptCount >= (exam.maxAttemptsAllowed ?? 1))
        return false; // Exceeded max
    
    if (studentAttempt.score != null && studentAttempt.score >= (exam.passingScore ?? 0)) {
        if (!exam.allowRetakeIfPassed)
            return false; // Passed, no retake allowed
    }
    
    if ((exam.minutesBetweenRetakes ?? 0) > 0) {
        const lastEndTime = new Date(studentAttempt.endTime ?? studentAttempt.startTime);
        const minutesElapsed = (Date.now() - lastEndTime.getTime()) / (1000 * 60);
        if (minutesElapsed < (exam.minutesBetweenRetakes ?? 0))
            return false; // Too soon
    }
    
    return true;
};

// In render
{canRetake(exam, attempt) ? (
    <Button onClick={() => startExam(exam)}>Làm lại</Button>
) : (
    <Button disabled>Không thể làm lại</Button>
)}
```

---

### FEATURE #2: Late Submission (6 giờ)

#### Bước 1: Update entity

```csharp
// AllEntities.cs - Exam
public class Exam
{
    // ... existing ...
    
    // ✅ ADD:
    public bool AllowLateSubmission { get; set; } = false;
    public int LateSubmissionMinutes { get; set; } = 0;
    public decimal LatePenaltyPercent { get; set; } = 0; // 0-100
}

// ExamAttempt
public class ExamAttempt
{
    // ... existing ...
    
    // ✅ ADD:
    public bool IsLateSubmission { get; set; } = false;
    public int? MinutesLate { get; set; }
    public decimal? LatePenaltyApplied { get; set; }
}
```

#### Bước 2: Service logic (Added in FIX #3)

Đã thêm late submission logic vào `SubmitAttemptAsync()`

#### Bước 3: Grading service - apply penalty

```csharp
// GradingService.cs - After grading completed
public async Task<...> MarkAsGradedAsync(long attemptId)
{
    try
    {
        var attempt = await _attemptRepo.GetByIdAsync(attemptId);
        if (attempt == null)
            return (false, "Attempt not found");

        var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
        var gradingResults = await _gradingRepo.GetByAttemptIdAsync(attemptId);
        
        decimal totalScore = gradingResults.Sum(g => g.Score);

        // ✅ Apply late penalty
        if (attempt.IsLateSubmission && exam.LatePenaltyPercent > 0)
        {
            attempt.LatePenaltyApplied = totalScore * (exam.LatePenaltyPercent / 100);
            totalScore -= attempt.LatePenaltyApplied.Value;
        }

        attempt.Status = "GRADED";
        attempt.Score = Math.Max(0, totalScore);  // Don't go negative
        await _attemptRepo.UpdateAsync(attempt);

        return (true, "Graded successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error marking as graded");
        return (false, $"Error: {ex.Message}");
    }
}
```

#### Bước 4: Frontend display

```typescript
// ExamReviewPage.tsx / GradingDetailPage.tsx

{attempt.isLateSubmission && (
    <Alert severity="warning">
        📍 Nộp bài muộn {attempt.minutesLate} phút
        {attempt.latePenaltyApplied > 0 && (
            <> - Trừ {attempt.latePenaltyApplied} điểm ({exam.latePenaltyPercent}%)</>
        )}
    </Alert>
)}
```

---

### FEATURE #3: Grading Audit Trail (7 giờ)

#### Bước 1: Tạo entity mới

```csharp
// AllEntities.cs
public class GradingAuditLog
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public long GradedByUserId { get; set; }
    
    public decimal? OldScore { get; set; }
    public decimal NewScore { get; set; }
    public string? Comment { get; set; }
    
    public DateTime ChangedAt { get; set; }
    public string ChangeReason { get; set; } // AUTO_GRADE, MANUAL_GRADE, OVERRIDE
    
    // Navigation
    public virtual ExamAttempt ExamAttempt { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
    public virtual User GradedBy { get; set; } = null!;
}
```

#### Bước 2: DbContext

```csharp
// ApplicationDbContext.cs
modelBuilder.Entity<GradingAuditLog>()
    .HasOne(gl => gl.ExamAttempt)
    .WithMany()
    .HasForeignKey(gl => gl.ExamAttemptId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<GradingAuditLog>()
    .HasOne(gl => gl.GradedBy)
    .WithMany()
    .HasForeignKey(gl => gl.GradedByUserId)
    .OnDelete(DeleteBehavior.Restrict);
```

#### Bước 3: Repository & Service

```csharp
// IGradingAuditLogRepository
public interface IGradingAuditLogRepository
{
    Task<GradingAuditLog> CreateAsync(GradingAuditLog log);
    Task<List<GradingAuditLog>> GetByAttemptIdAsync(long attemptId);
    Task<List<GradingAuditLog>> GetByQuestionIdAsync(long questionId);
}

// GradingService.cs - Record changes
private async Task LogGradingChangeAsync(
    long attemptId, long questionId, 
    decimal? oldScore, decimal newScore,
    long gradedBy, string reason, string? comment)
{
    var log = new GradingAuditLog
    {
        ExamAttemptId = attemptId,
        QuestionId = questionId,
        GradedByUserId = gradedBy,
        OldScore = oldScore,
        NewScore = newScore,
        Comment = comment,
        ChangedAt = DateTime.UtcNow,
        ChangeReason = reason
    };
    
    await _auditRepo.CreateAsync(log);
}

// Call in ManualGradeAsync / BatchGradeAsync
var oldGrading = await _gradingRepo.GetByAttemptAndQuestionAsync(attemptId, questionId);
await LogGradingChangeAsync(attemptId, questionId, oldGrading?.Score, request.Score, gradedBy, "MANUAL_GRADE", request.Comment);
```

#### Bước 4: Controller endpoint

```csharp
// GradingController.cs
[Authorize(Roles = "TEACHER,ADMIN")]
[HttpGet("attempts/{attemptId}/audit-log")]
public async Task<ActionResult<ResponseResult<List<GradingAuditLogResponse>>>> GetAuditLog(long attemptId)
{
    // ... authorization ...
    
    var logs = await _auditRepo.GetByAttemptIdAsync(attemptId);
    var response = logs.Select(l => new GradingAuditLogResponse
    {
        QuestionId = l.QuestionId,
        OldScore = l.OldScore,
        NewScore = l.NewScore,
        ChangedAt = l.ChangedAt,
        GradedByName = l.GradedBy.FullName,
        Reason = l.ChangeReason,
        Comment = l.Comment
    }).ToList();
    
    return Ok(new ResponseResult<List<GradingAuditLogResponse>> { Success = true, Data = response });
}
```

---

### FEATURE #4 & #5: Notifications + Violations (10 giờ)

[Due to length, these are briefly outlined - see full implementation above in main report]

---

## TUẦN 3: TESTING & POLISH (24 giờ)

### Unit Tests (12 giờ)

```bash
# Add xUnit or NUnit tests
# Test files: *Tests.cs in tests/OnlineExamSystem.Tests/

dotnet test
```

Key test suites:
- ExamAttemptServiceTests
- GradingConcurrencyTests
- AnswerServiceTests
- AuthorizationTests

### Integration Tests (7 giờ)

- Database cascade delete tests
- End-to-end grading workflow
- Concurrent submission handling

### Manual Testing (5 giờ)

- Retake policy scenarios
- Late submission + penalty
- Authorization edge cases

---

## ✅ DEPLOYMENT CHECKLIST

```
Before going to production:

□ All 6 critical fixes implemented & tested
□ Unit test coverage > 80% for services
□ Integration tests pass
□ Authorization tests: all 28 controllers enforcing roles
□ Database migrations: tested on fresh DB
□ Performance: submit-answer < 200ms, grade < 100ms
□ Load test: 100+ concurrent submissions
□ Documentation: updated all APIs
□ Backup strategy: database backup automated
□ Monitoring: logging configured, alerts set
□ Role-based access: tested for each role (admin, teacher, student)
```

---

**FIX CHECKLIST - Track your progress:**

- [ ] FIX #1: Authorization (12h)
- [ ] FIX #2: Race condition (4h)
- [ ] FIX #3: Time validation (6h)
- [ ] FIX #4: Enrollment (Included in #3)
- [ ] FIX #5: Cascade delete (3h)
- [ ] FIX #6: Optimistic locking (8h)
- [ ] FIX #7: Teaching Assignment (15h)
- [ ] FEATURE #1: Retake policy (8h)
- [ ] FEATURE #2: Late submission (6h)
- [ ] FEATURE #3: Audit trail (7h)
- [ ] FEATURE #4: Notifications (5h)
- [ ] FEATURE #5: Violations (8h)
- [ ] Testing & QA (24h)
- [ ] Documentation (4h)

**TOTAL: 111 hours (3 weeks, 2 devs)**
