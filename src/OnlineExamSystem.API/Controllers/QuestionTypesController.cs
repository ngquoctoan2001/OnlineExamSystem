using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/question-types")]
[Authorize]
[Produces("application/json")]
[Tags("Question Types")]
public class QuestionTypesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionTypesController> _logger;

    public QuestionTypesController(ApplicationDbContext context, ILogger<QuestionTypesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<List<QuestionTypeResponse>>>> GetAll()
    {
        try
        {
            var types = await _context.QuestionTypes
                .OrderBy(t => t.Id)
                .Select(t => new QuestionTypeResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description
                })
                .ToListAsync();

            return Ok(new ResponseResult<List<QuestionTypeResponse>>
            {
                Success = true,
                Message = "Success",
                Data = types
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving question types");
            return StatusCode(500, new ResponseResult<List<QuestionTypeResponse>>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<QuestionTypeResponse>>> GetById(long id)
    {
        var type = await _context.QuestionTypes.FindAsync(id);
        if (type == null)
            return NotFound(new ResponseResult<QuestionTypeResponse> { Success = false, Message = "Question type not found" });

        return Ok(new ResponseResult<QuestionTypeResponse>
        {
            Success = true,
            Message = "Success",
            Data = new QuestionTypeResponse { Id = type.Id, Name = type.Name, Description = type.Description }
        });
    }
}

public class QuestionTypeResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
