namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Subject service implementation
/// </summary>
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(ISubjectRepository subjectRepository, ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, SubjectResponse? Data)> GetSubjectByIdAsync(long id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            
            if (subject == null)
            {
                _logger.LogWarning("Subject not found: {SubjectId}", id);
                return (false, "Subject not found", null);
            }

            var response = MapToSubjectResponse(subject);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject {SubjectId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, SubjectListResponse? Data)> GetAllSubjectsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (subjects, totalCount) = await _subjectRepository.GetAllAsync(page, pageSize);
            
            var subjectResponses = subjects.Select(MapToSubjectResponse).ToList();
            
            var response = new SubjectListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Subjects = subjectResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subjects");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<SubjectResponse>? Data)> SearchSubjectsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return (false, "Search term cannot be empty", null);
            }

            var subjects = await _subjectRepository.SearchAsync(searchTerm.Trim());
            var responses = subjects.Select(MapToSubjectResponse).ToList();

            return (true, $"Found {responses.Count} subject(s)", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching subjects with term: {SearchTerm}", searchTerm);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, SubjectResponse? Data)> CreateSubjectAsync(CreateSubjectRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Code) || 
                string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Code and Name are required", null);
            }

            // Check if code exists
            var codeExists = await _subjectRepository.CodeExistsAsync(request.Code);
            if (codeExists)
            {
                _logger.LogWarning("Subject code already exists: {Code}", request.Code);
                return (false, "Subject code already exists", null);
            }

            // Create subject
            var subject = new Subject
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description ?? string.Empty
            };

            var createdSubject = await _subjectRepository.CreateAsync(subject);

            _logger.LogInformation("Subject created successfully: {Code}", request.Code);
            
            var response = MapToSubjectResponse(createdSubject);
            return (true, "Subject created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, SubjectResponse? Data)> UpdateSubjectAsync(long id, UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found: {SubjectId}", id);
                return (false, "Subject not found", null);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                subject.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                subject.Description = request.Description;
            }

            var updatedSubject = await _subjectRepository.UpdateAsync(subject);
            var response = MapToSubjectResponse(updatedSubject);

            _logger.LogInformation("Subject updated: {SubjectId}", id);
            return (true, "Subject updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteSubjectAsync(long id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found: {SubjectId}", id);
                return (false, "Subject not found");
            }

            var result = await _subjectRepository.DeleteAsync(id);
            if (!result)
            {
                return (false, "Failed to delete subject");
            }

            _logger.LogInformation("Subject deleted: {SubjectId}", id);
            return (true, "Subject deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    private SubjectResponse MapToSubjectResponse(Subject subject)
    {
        return new SubjectResponse
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            QuestionCount = subject.Questions?.Count ?? 0,
            ExamCount = subject.Exams?.Count ?? 0
        };
    }
}
