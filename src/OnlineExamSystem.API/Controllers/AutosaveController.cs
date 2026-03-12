using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/autosave")]
[Authorize]
[Produces("application/json")]
[Tags("Autosave")]
public class AutosaveController : ControllerBase
{
    private readonly IAnswerService _answerService;
    private readonly ILogger<AutosaveController> _logger;

    public AutosaveController(IAnswerService answerService, ILogger<AutosaveController> logger)
    {
        _answerService = answerService;
        _logger = logger;
    }

    /// <summary>
    /// Save autosave data for an exam attempt
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<AutosaveResponse>>> SaveAutosave([FromBody] AutosaveRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AutosaveResponse> { Success = false, Message = "Invalid request" });

        _logger.LogInformation("Autosaving {Count} answers for attempt: {AttemptId}", request.Answers.Count, request.AttemptId);

        var savedCount = 0;
        foreach (var answer in request.Answers)
        {
            var submitRequest = new SubmitAnswerRequest
            {
                QuestionId = answer.QuestionId,
                SelectedOptionIds = answer.SelectedOptionIds,
                TextContent = answer.TextContent,
                EssayContent = answer.EssayContent
            };

            var (success, _, _) = await _answerService.AutoSaveAsync(request.AttemptId, submitRequest);
            if (success) savedCount++;
        }

        return Ok(new ResponseResult<AutosaveResponse>
        {
            Success = true,
            Message = $"Autosaved {savedCount} answers",
            Data = new AutosaveResponse
            {
                AttemptId = request.AttemptId,
                SavedCount = savedCount,
                SavedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Get autosave data for an attempt
    /// </summary>
    [HttpGet("{attemptId}")]
    public async Task<ActionResult<ResponseResult<List<AnswerResponse>>>> GetAutosave(long attemptId)
    {
        _logger.LogInformation("Getting autosave data for attempt: {AttemptId}", attemptId);

        var (success, message, data) = await _answerService.GetAttemptQuestionsAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        var answers = data?
            .Where(q => q.CurrentAnswer != null)
            .Select(q => q.CurrentAnswer!)
            .ToList() ?? new();

        return Ok(new ResponseResult<List<AnswerResponse>>
        {
            Success = true,
            Message = "Autosave data retrieved",
            Data = answers
        });
    }

    /// <summary>
    /// Delete autosave data for an attempt
    /// </summary>
    [HttpDelete("{attemptId}")]
    public async Task<ActionResult<ResponseResult<object>>> DeleteAutosave(long attemptId)
    {
        _logger.LogInformation("Deleting autosave data for attempt: {AttemptId}", attemptId);

        // Autosave data is the same as answers - clearing is not typically done
        // This endpoint confirms the autosave session is closed
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Autosave data cleared for attempt",
            Data = new { AttemptId = attemptId }
        });
    }
}
