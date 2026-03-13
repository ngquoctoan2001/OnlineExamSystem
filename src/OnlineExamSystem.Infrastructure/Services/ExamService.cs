namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.Services;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Exam service implementation with business logic
/// </summary>
public class ExamService : OnlineExamSystem.Application.Services.IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IExamSettingsRepository _examSettingsRepository;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamRepository examRepository,
        ITeacherRepository teacherRepository,
        ISubjectRepository subjectRepository,
        IExamSettingsRepository examSettingsRepository,
        IExamAttemptRepository examAttemptRepository,
        IActivityLogService activityLog,
        ILogger<ExamService> logger)
    {
        _examRepository = examRepository;
        _teacherRepository = teacherRepository;
        _subjectRepository = subjectRepository;
        _examSettingsRepository = examSettingsRepository;
        _examAttemptRepository = examAttemptRepository;
        _activityLog = activityLog;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> GetExamByIdAsync(long id)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            
            if (exam == null)
            {
                _logger.LogWarning("Exam not found: {ExamId}", id);
                return (false, "Exam not found", null);
            }

            var response = MapToExamResponse(exam);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam {ExamId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamListResponse? Data)> GetAllExamsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (exams, totalCount) = await _examRepository.GetAllAsync(page, pageSize);
            
            var response = new ExamListResponse
            {
                Items = exams.Select(MapToExamResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exams");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> SearchExamsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return (false, "Search term cannot be empty", null);

            var exams = await _examRepository.SearchAsync(searchTerm);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching exams");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsByTeacherAsync(long teacherId)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", teacherId);
                return (false, "Teacher not found", null);
            }

            var exams = await _examRepository.GetByTeacherAsync(teacherId);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams by teacher {TeacherId}", teacherId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsBySubjectAsync(long subjectId)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found: {SubjectId}", subjectId);
                return (false, "Subject not found", null);
            }

            var exams = await _examRepository.GetBySubjectAsync(subjectId);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams by subject {SubjectId}", subjectId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> CreateExamAsync(CreateExamRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
                return (false, "Exam title is required", null);

            if (request.DurationMinutes <= 0)
                return (false, "Duration must be greater than 0", null);

            if (request.StartTime >= request.EndTime)
                return (false, "Start time must be before end time", null);

            if (request.MaxAttemptsAllowed < 0)
                return (false, "Max attempts must be greater than or equal to 0", null);

            if (request.MinutesBetweenRetakes < 0)
                return (false, "Minutes between retakes must be greater than or equal to 0", null);

            if (request.PassingScore < 0 || request.PassingScore > 100)
                return (false, "Passing score must be between 0 and 100", null);

            // Check if exam title already exists
            if (await _examRepository.TitleExistsAsync(request.Title))
                return (false, "An exam with this title already exists", null);

            // Verify teacher exists
            var teacher = await _teacherRepository.GetByIdAsync(request.CreatedBy);
            if (teacher == null)
                return (false, "Teacher not found", null);

            // Verify subject exists
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            var exam = new Exam
            {
                Title = request.Title.Trim(),
                SubjectId = request.SubjectId,
                CreatedBy = request.CreatedBy,
                DurationMinutes = request.DurationMinutes,
                MaxAttemptsAllowed = request.MaxAttemptsAllowed,
                MinutesBetweenRetakes = request.MinutesBetweenRetakes,
                AllowRetakeIfPassed = request.AllowRetakeIfPassed,
                PassingScore = request.PassingScore,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Description = request.Description?.Trim() ?? string.Empty,
                SubjectExamTypeId = request.SubjectExamTypeId,
                Status = "DRAFT"
            };

            var createdExam = await _examRepository.CreateAsync(exam);
            var response = MapToExamResponse(createdExam);

            _logger.LogInformation("Exam created: {ExamId}", createdExam.Id);
            await _activityLog.LogAsync(request.CreatedBy, "EXAM_CREATED", "Exam", createdExam.Id, $"Title: {createdExam.Title}");
            return (true, "Exam created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> UpdateExamAsync(long id, UpdateExamRequest request)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return (false, "Exam not found", null);

            // Can only update if in DRAFT status
            if (exam.Status != "DRAFT")
                return (false, "Cannot update exam in ACTIVE or CLOSED status", null);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
                return (false, "Exam title is required", null);

            if (request.DurationMinutes <= 0)
                return (false, "Duration must be greater than 0", null);

            if (request.StartTime >= request.EndTime)
                return (false, "Start time must be before end time", null);

            if (request.MaxAttemptsAllowed < 0)
                return (false, "Max attempts must be greater than or equal to 0", null);

            if (request.MinutesBetweenRetakes < 0)
                return (false, "Minutes between retakes must be greater than or equal to 0", null);

            if (request.PassingScore < 0 || request.PassingScore > 100)
                return (false, "Passing score must be between 0 and 100", null);

            // Check if new title exists (excluding current exam)
            if (request.Title.Trim() != exam.Title && await _examRepository.TitleExistsAsync(request.Title, id))
                return (false, "An exam with this title already exists", null);

            // Verify subject exists
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            exam.Title = request.Title.Trim();
            exam.SubjectId = request.SubjectId;
            exam.DurationMinutes = request.DurationMinutes;
            exam.MaxAttemptsAllowed = request.MaxAttemptsAllowed;
            exam.MinutesBetweenRetakes = request.MinutesBetweenRetakes;
            exam.AllowRetakeIfPassed = request.AllowRetakeIfPassed;
            exam.PassingScore = request.PassingScore;
            exam.StartTime = request.StartTime;
            exam.EndTime = request.EndTime;
            exam.Description = request.Description?.Trim() ?? string.Empty;
            exam.SubjectExamTypeId = request.SubjectExamTypeId;

            var updatedExam = await _examRepository.UpdateAsync(exam);
            var response = MapToExamResponse(updatedExam);

            _logger.LogInformation("Exam updated: {ExamId}", id);
            return (true, "Exam updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exam {ExamId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteExamAsync(long id)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return (false, "Exam not found");

            // Can only delete DRAFT exams
            if (exam.Status != "DRAFT")
                return (false, "Không thể xóa kỳ thi đang hoạt động hoặc đã đóng");

            // Cannot delete if students have already attempted
            var attempts = await _examAttemptRepository.GetExamAttemptsAsync(id);
            if (attempts.Count > 0)
                return (false, "Không thể xóa kỳ thi đã có học sinh tham gia");

            var deleted = await _examRepository.DeleteAsync(id);
            if (!deleted)
                return (false, "Failed to delete exam");

            _logger.LogInformation("Exam deleted: {ExamId}", id);
            await _activityLog.LogAsync(null, "EXAM_DELETED", "Exam", id);
            return (true, "Exam deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exam {ExamId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, ExamSettingsResponse? Data)> ConfigureSettingsAsync(long examId, ConfigureExamSettingsRequest request)
    {
        try
        {
            // Verify exam exists
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            // Get or create settings
            var settings = await _examSettingsRepository.GetByExamIdAsync(examId);
            var normalizedGraceMinutes = Math.Max(0, request.GracePeriodMinutes);
            var normalizedPenaltyPercent = Math.Clamp(request.LatePenaltyPercent, 0m, 100m);
            
            if (settings == null)
            {
                settings = new ExamSetting
                {
                    ExamId = examId,
                    ShuffleQuestions = request.ShuffleQuestions,
                    ShuffleAnswers = request.ShuffleAnswers,
                    ShowResultImmediately = request.ShowResultImmediately,
                    AllowReview = request.AllowReview,
                    AllowLateSubmission = request.AllowLateSubmission,
                    GracePeriodMinutes = normalizedGraceMinutes,
                    LatePenaltyPercent = normalizedPenaltyPercent
                };
                settings = await _examSettingsRepository.CreateAsync(settings);
            }
            else
            {
                settings.ShuffleQuestions = request.ShuffleQuestions;
                settings.ShuffleAnswers = request.ShuffleAnswers;
                settings.ShowResultImmediately = request.ShowResultImmediately;
                settings.AllowReview = request.AllowReview;
                settings.AllowLateSubmission = request.AllowLateSubmission;
                settings.GracePeriodMinutes = normalizedGraceMinutes;
                settings.LatePenaltyPercent = normalizedPenaltyPercent;
                settings = await _examSettingsRepository.UpdateAsync(settings);
            }

            var response = MapToSettingsResponse(settings);
            _logger.LogInformation("Exam settings configured: {ExamId}", examId);
            return (true, "Settings configured successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring exam settings {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamSettingsResponse? Data)> GetSettingsAsync(long examId)
    {
        try
        {
            // Verify exam exists
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var settings = await _examSettingsRepository.GetByExamIdAsync(examId);
            
            // If no settings exist yet, return defaults
            if (settings == null)
            {
                var defaultSettings = new ExamSetting
                {
                    ExamId = examId,
                    ShuffleQuestions = false,
                    ShuffleAnswers = false,
                    ShowResultImmediately = false,
                    AllowReview = false,
                    AllowLateSubmission = false,
                    GracePeriodMinutes = 0,
                    LatePenaltyPercent = 0m
                };
                return (true, "Default settings", MapToSettingsResponse(defaultSettings));
            }

            var response = MapToSettingsResponse(settings);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam settings {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ActivateExamResponse? Data)> ActivateExamAsync(long examId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            // Can only activate DRAFT exams
            if (exam.Status != "DRAFT")
                return (false, "Only DRAFT exams can be activated", null);

            // Verify exam has start and end times in future or at least valid
            if (exam.EndTime <= DateTime.UtcNow)
                return (false, "Exam end time must be in the future", null);

            exam.Status = "ACTIVE";
            var updatedExam = await _examRepository.UpdateAsync(exam);

            var response = new ActivateExamResponse
            {
                ExamId = updatedExam.Id,
                Status = updatedExam.Status,
                ActivatedAt = DateTime.UtcNow,
                Message = "Exam activated successfully"
            };

            _logger.LogInformation("Exam activated: {ExamId}", examId);
            return (true, "Exam activated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> CloseExamAsync(long examId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found");

            // Can only close ACTIVE exams
            if (exam.Status != "ACTIVE")
                return (false, "Only ACTIVE exams can be closed");

            exam.Status = "CLOSED";
            await _examRepository.UpdateAsync(exam);

            _logger.LogInformation("Exam closed: {ExamId}", examId);
            return (true, "Exam closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ChangeStatusAsync(long examId, string newStatus)
    {
        try
        {
            var validStatuses = new[] { "DRAFT", "ACTIVE", "CLOSED" };
            if (!validStatuses.Contains(newStatus.ToUpper()))
                return (false, "Invalid status. Must be DRAFT, ACTIVE, or CLOSED");

            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found");

            // Validate status transitions
            var currentStatus = exam.Status.ToUpper();
            var targetStatus = newStatus.ToUpper();

            // Allowed transitions: DRAFT -> ACTIVE, ACTIVE -> CLOSED
            if (currentStatus == "DRAFT" && targetStatus == "ACTIVE")
            {
                exam.Status = "ACTIVE";
                await _examRepository.UpdateAsync(exam);
                _logger.LogInformation("Exam status changed DRAFT->ACTIVE: {ExamId}", examId);
                return (true, "Exam activated");
            }
            else if (currentStatus == "ACTIVE" && targetStatus == "CLOSED")
            {
                exam.Status = "CLOSED";
                await _examRepository.UpdateAsync(exam);
                _logger.LogInformation("Exam status changed ACTIVE->CLOSED: {ExamId}", examId);
                return (true, "Exam closed");
            }
            else if (currentStatus == targetStatus)
            {
                return (true, $"Exam already in {targetStatus} status");
            }
            else
            {
                return (false, $"Cannot transition from {currentStatus} to {targetStatus}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing exam status {ExamId}", examId);
            return (false, $"Error: {ex.Message}");
        }
    }

    private ExamResponse MapToExamResponse(Exam exam)
    {
        return new ExamResponse
        {
            Id = exam.Id,
            Title = exam.Title,
            SubjectId = exam.SubjectId,
            SubjectName = exam.Subject?.Name ?? string.Empty,
            CreatedBy = exam.CreatedBy,
            DurationMinutes = exam.DurationMinutes,
            MaxAttemptsAllowed = exam.MaxAttemptsAllowed,
            MinutesBetweenRetakes = exam.MinutesBetweenRetakes,
            AllowRetakeIfPassed = exam.AllowRetakeIfPassed,
            PassingScore = exam.PassingScore,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            Description = exam.Description,
            Status = exam.Status,
            CreatedAt = exam.CreatedAt,
            SubjectExamTypeId = exam.SubjectExamTypeId,
            SubjectExamTypeName = exam.SubjectExamType?.Name,
            SubjectExamTypeCoefficient = exam.SubjectExamType?.Coefficient
        };
    }

    private ExamSettingsResponse MapToSettingsResponse(ExamSetting settings)
    {
        return new ExamSettingsResponse
        {
            Id = settings.Id,
            ExamId = settings.ExamId,
            ShuffleQuestions = settings.ShuffleQuestions,
            ShuffleAnswers = settings.ShuffleAnswers,
            ShowResultImmediately = settings.ShowResultImmediately,
            AllowReview = settings.AllowReview,
            AllowLateSubmission = settings.AllowLateSubmission,
            GracePeriodMinutes = settings.GracePeriodMinutes,
            LatePenaltyPercent = settings.LatePenaltyPercent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
