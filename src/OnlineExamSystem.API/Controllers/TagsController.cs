using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
[Produces("application/json")]
[Tags("Tags")]
public class TagsController : ControllerBase
{
    private readonly ITagRepository _tagRepository;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagRepository tagRepository, ILogger<TagsController> logger)
    {
        _tagRepository = tagRepository;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<List<TagResponse>>>> GetAll([FromQuery] string? search)
    {
        try
        {
            var tags = string.IsNullOrWhiteSpace(search)
                ? await _tagRepository.GetAllAsync()
                : await _tagRepository.SearchAsync(search);

            var result = tags.Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Ok(new ResponseResult<List<TagResponse>> { Success = true, Message = "Success", Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags");
            return StatusCode(500, new ResponseResult<List<TagResponse>> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<TagResponse>>> GetById(long id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
            return NotFound(new ResponseResult<TagResponse> { Success = false, Message = "Tag not found" });

        return Ok(new ResponseResult<TagResponse>
        {
            Success = true,
            Message = "Success",
            Data = new TagResponse { Id = tag.Id, Name = tag.Name, Description = tag.Description, CreatedAt = tag.CreatedAt }
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResult<TagResponse>>> Create([FromBody] CreateTagRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<TagResponse> { Success = false, Message = "Invalid request" });

        try
        {
            if (await _tagRepository.NameExistsAsync(request.Name))
                return BadRequest(new ResponseResult<TagResponse> { Success = false, Message = "Tag name already exists" });

            var tag = new Tag
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _tagRepository.CreateAsync(tag);

            return Ok(new ResponseResult<TagResponse>
            {
                Success = true,
                Message = "Tag created successfully",
                Data = new TagResponse { Id = tag.Id, Name = tag.Name, Description = tag.Description, CreatedAt = tag.CreatedAt }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return StatusCode(500, new ResponseResult<TagResponse> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<TagResponse>>> Update(long id, [FromBody] CreateTagRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<TagResponse> { Success = false, Message = "Invalid request" });

        try
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
                return NotFound(new ResponseResult<TagResponse> { Success = false, Message = "Tag not found" });

            if (await _tagRepository.NameExistsAsync(request.Name, id))
                return BadRequest(new ResponseResult<TagResponse> { Success = false, Message = "Tag name already exists" });

            tag.Name = request.Name.Trim();
            tag.Description = request.Description?.Trim();

            await _tagRepository.UpdateAsync(tag);

            return Ok(new ResponseResult<TagResponse>
            {
                Success = true,
                Message = "Tag updated successfully",
                Data = new TagResponse { Id = tag.Id, Name = tag.Name, Description = tag.Description, CreatedAt = tag.CreatedAt }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag");
            return StatusCode(500, new ResponseResult<TagResponse> { Success = false, Message = "An error occurred" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<string>>> Delete(long id)
    {
        try
        {
            var deleted = await _tagRepository.DeleteAsync(id);
            if (!deleted)
                return NotFound(new ResponseResult<string> { Success = false, Message = "Tag not found" });

            return Ok(new ResponseResult<string> { Success = true, Message = "Tag deleted successfully", Data = "Tag deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag");
            return StatusCode(500, new ResponseResult<string> { Success = false, Message = "An error occurred" });
        }
    }
}
