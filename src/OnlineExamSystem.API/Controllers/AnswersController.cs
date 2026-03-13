using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/attempts/{attemptId}/answers")]
[Authorize]
[Produces("application/json")]
[Tags("Answers")]
public class AnswersController : ControllerBase
{
    private readonly IAnswerService _answerService;
    private readonly ILogger<AnswersController> _logger;

    public AnswersController(IAnswerService answerService, ILogger<AnswersController> logger)
    {
        _answerService = answerService;
        _logger = logger;
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> Submit(long attemptId, [FromBody] SubmitAnswerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _answerService.SubmitAnswerAsync(attemptId, request);
        if (!success)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPut("{questionId}")]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> Update(long attemptId, long questionId, [FromBody] SubmitAnswerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _answerService.UpdateAnswerAsync(attemptId, questionId, request);
        if (!success)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("{questionId}")]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> GetAnswer(long attemptId, long questionId)
    {
        var (success, message, data) = await _answerService.GetAnswerAsync(attemptId, questionId);
        if (!success)
            return NotFound(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("autosave")]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> AutoSave(long attemptId, [FromBody] SubmitAnswerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _answerService.AutoSaveAsync(attemptId, request);
        if (!success)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }
}
