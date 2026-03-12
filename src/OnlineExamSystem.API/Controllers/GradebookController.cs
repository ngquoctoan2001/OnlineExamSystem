namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Gradebook")]
public class GradebookController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public GradebookController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ResponseResult<StudentFullGradebookResponse>>> GetStudentGradebook(long studentId)
    {
        var student = await _db.Students.AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Student not found" });

        var classIds = await _db.ClassStudents.AsNoTracking()
            .Where(cs => cs.StudentId == studentId)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        var examIds = await _db.ExamClasses.AsNoTracking()
            .Where(ec => classIds.Contains(ec.ClassId))
            .Select(ec => ec.ExamId)
            .Distinct()
            .ToListAsync();

        var exams = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Include(e => e.ExamQuestions)
            .Where(e => examIds.Contains(e.Id))
            .ToListAsync();

        var attempts = await _db.ExamAttempts.AsNoTracking()
            .Where(a => a.StudentId == studentId && examIds.Contains(a.ExamId))
            .ToListAsync();

        var subjectGroups = exams.GroupBy(e => e.SubjectId);
        var subjects = new List<SubjectGradeSummary>();

        foreach (var group in subjectGroups)
        {
            var entries = BuildEntries(group.ToList(), attempts.Where(a => group.Any(e => e.Id == a.ExamId)).ToList());
            subjects.Add(new SubjectGradeSummary
            {
                SubjectId = group.Key,
                SubjectName = group.First().Subject?.Name ?? string.Empty,
                Entries = entries,
                WeightedAverage = CalculateWeightedAverage(entries)
            });
        }

        var response = new StudentFullGradebookResponse
        {
            StudentId = student.Id,
            StudentName = student.User?.FullName ?? string.Empty,
            StudentCode = student.StudentCode,
            Subjects = subjects.OrderBy(s => s.SubjectName).ToList(),
            OverallAverage = subjects.Any(s => s.WeightedAverage.HasValue)
                ? Math.Round(subjects.Where(s => s.WeightedAverage.HasValue).Average(s => s.WeightedAverage!.Value), 2)
                : null
        };

        return Ok(new ResponseResult<StudentFullGradebookResponse> { Success = true, Data = response });
    }

    [HttpGet("student/{studentId}/subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<StudentSubjectGradebookResponse>>> GetStudentSubjectGradebook(long studentId, long subjectId)
    {
        var student = await _db.Students.AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Student not found" });

        var classIds = await _db.ClassStudents.AsNoTracking()
            .Where(cs => cs.StudentId == studentId)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        var examIds = await _db.ExamClasses.AsNoTracking()
            .Where(ec => classIds.Contains(ec.ClassId))
            .Select(ec => ec.ExamId)
            .Distinct()
            .ToListAsync();

        var exams = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Include(e => e.ExamQuestions)
            .Where(e => examIds.Contains(e.Id) && e.SubjectId == subjectId)
            .ToListAsync();

        var examIdsList = exams.Select(e => e.Id).ToList();
        var attempts = await _db.ExamAttempts.AsNoTracking()
            .Where(a => a.StudentId == studentId && examIdsList.Contains(a.ExamId))
            .ToListAsync();

        var entries = BuildEntries(exams, attempts);
        var subject = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subjectId);

        return Ok(new ResponseResult<StudentSubjectGradebookResponse>
        {
            Success = true,
            Data = new StudentSubjectGradebookResponse
            {
                StudentId = student.Id,
                StudentName = student.User?.FullName ?? string.Empty,
                StudentCode = student.StudentCode,
                SubjectId = subjectId,
                SubjectName = subject?.Name ?? string.Empty,
                Entries = entries,
                WeightedAverage = CalculateWeightedAverage(entries)
            }
        });
    }

    [HttpGet("class/{classId}/subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<ClassSubjectGradebookResponse>>> GetClassSubjectGradebook(long classId, long subjectId)
    {
        var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Class not found" });

        var subject = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subjectId);
        if (subject == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Subject not found" });

        var examTypes = await _db.SubjectExamTypes.AsNoTracking()
            .Where(e => e.SubjectId == subjectId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        var classStudents = await _db.ClassStudents.AsNoTracking()
            .Include(cs => cs.Student).ThenInclude(s => s.User)
            .Where(cs => cs.ClassId == classId)
            .ToListAsync();

        var examIdsForClass = await _db.ExamClasses.AsNoTracking()
            .Where(ec => ec.ClassId == classId)
            .Select(ec => ec.ExamId)
            .ToListAsync();

        var exams = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Include(e => e.ExamQuestions)
            .Where(e => examIdsForClass.Contains(e.Id) && e.SubjectId == subjectId)
            .ToListAsync();

        var studentIds = classStudents.Select(cs => cs.StudentId).ToList();
        var examIdsList = exams.Select(e => e.Id).ToList();
        var allAttempts = await _db.ExamAttempts.AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentId) && examIdsList.Contains(a.ExamId))
            .ToListAsync();

        var studentGradebooks = classStudents
            .OrderBy(cs => cs.Student?.User?.FullName)
            .Select(cs =>
            {
                var studentAttempts = allAttempts.Where(a => a.StudentId == cs.StudentId).ToList();
                var entries = BuildEntries(exams, studentAttempts);
                return new StudentSubjectGradebookResponse
                {
                    StudentId = cs.StudentId,
                    StudentName = cs.Student?.User?.FullName ?? string.Empty,
                    StudentCode = cs.Student?.StudentCode ?? string.Empty,
                    SubjectId = subjectId,
                    SubjectName = subject.Name,
                    Entries = entries,
                    WeightedAverage = CalculateWeightedAverage(entries)
                };
            }).ToList();

        return Ok(new ResponseResult<ClassSubjectGradebookResponse>
        {
            Success = true,
            Data = new ClassSubjectGradebookResponse
            {
                ClassId = classId,
                ClassName = cls.Name,
                SubjectId = subjectId,
                SubjectName = subject.Name,
                Students = studentGradebooks,
                ExamTypes = examTypes.Select(et => new SubjectExamTypeResponse
                {
                    Id = et.Id,
                    SubjectId = et.SubjectId,
                    Name = et.Name,
                    Coefficient = et.Coefficient,
                    RequiredCount = et.RequiredCount,
                    SortOrder = et.SortOrder
                }).ToList()
            }
        });
    }

    private static List<GradebookEntryResponse> BuildEntries(
        List<OnlineExamSystem.Domain.Entities.Exam> exams,
        List<OnlineExamSystem.Domain.Entities.ExamAttempt> attempts)
    {
        return exams
            .OrderBy(e => e.SubjectExamType?.SortOrder ?? 999)
            .ThenBy(e => e.StartTime)
            .Select(exam =>
            {
                var attempt = attempts
                    .Where(a => a.ExamId == exam.Id && (a.Status == "GRADED" || a.Status == "SUBMITTED" || a.Status == "PUBLISHED"))
                    .OrderByDescending(a => a.Score)
                    .FirstOrDefault();

                var totalPoints = exam.ExamQuestions?.Sum(eq => eq.MaxScore);
                var scoreOn10 = (attempt?.Score != null && totalPoints > 0)
                    ? Math.Round(attempt.Score.Value / totalPoints.Value * 10, 2)
                    : (decimal?)null;

                return new GradebookEntryResponse
                {
                    ExamId = exam.Id,
                    ExamTitle = exam.Title,
                    SubjectExamTypeId = exam.SubjectExamTypeId,
                    SubjectExamTypeName = exam.SubjectExamType?.Name,
                    Coefficient = exam.SubjectExamType?.Coefficient ?? 1,
                    Score = attempt?.Score,
                    TotalPoints = totalPoints,
                    ScoreOn10 = scoreOn10,
                    Status = attempt?.Status ?? "NOT_ATTEMPTED",
                    CompletedAt = attempt?.EndTime
                };
            }).ToList();
    }

    private static decimal? CalculateWeightedAverage(List<GradebookEntryResponse> entries)
    {
        var graded = entries.Where(e => e.ScoreOn10.HasValue && e.SubjectExamTypeId.HasValue).ToList();
        if (!graded.Any()) return null;

        var totalWeight = graded.Sum(e => e.Coefficient);
        if (totalWeight <= 0) return null;

        var weightedSum = graded.Sum(e => e.ScoreOn10!.Value * e.Coefficient);
        return Math.Round(weightedSum / totalWeight, 2);
    }
}
