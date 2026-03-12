namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

[ApiController]
[Route("api/subject-exam-types")]
[Authorize]
[Produces("application/json")]
[Tags("SubjectExamTypes")]
public class SubjectExamTypesController : ControllerBase
{
    private readonly ISubjectExamTypeRepository _repository;
    private readonly ISubjectRepository _subjectRepository;

    public SubjectExamTypesController(
        ISubjectExamTypeRepository repository,
        ISubjectRepository subjectRepository)
    {
        _repository = repository;
        _subjectRepository = subjectRepository;
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<List<SubjectExamTypeResponse>>>> GetBySubject(long subjectId)
    {
        var items = await _repository.GetBySubjectIdAsync(subjectId);
        var responses = items.Select(MapToResponse).ToList();
        return Ok(new ResponseResult<List<SubjectExamTypeResponse>> { Success = true, Data = responses });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<SubjectExamTypeResponse>>> GetById(long id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Not found" });
        return Ok(new ResponseResult<SubjectExamTypeResponse> { Success = true, Data = MapToResponse(item) });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResult<SubjectExamTypeResponse>>> Create([FromBody] CreateSubjectExamTypeRequest request)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
        if (subject == null)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Subject not found" });

        var entity = new SubjectExamType
        {
            SubjectId = request.SubjectId,
            Name = request.Name.Trim(),
            Coefficient = request.Coefficient,
            RequiredCount = request.RequiredCount,
            SortOrder = request.SortOrder
        };

        var created = await _repository.CreateAsync(entity);
        var response = MapToResponse(created);
        response.SubjectName = subject.Name;
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            new ResponseResult<SubjectExamTypeResponse> { Success = true, Data = response, Message = "Created successfully" });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<SubjectExamTypeResponse>>> Update(long id, [FromBody] UpdateSubjectExamTypeRequest request)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Not found" });

        if (request.Name != null) entity.Name = request.Name.Trim();
        if (request.Coefficient.HasValue) entity.Coefficient = request.Coefficient.Value;
        if (request.RequiredCount.HasValue) entity.RequiredCount = request.RequiredCount.Value;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;

        var updated = await _repository.UpdateAsync(entity);
        return Ok(new ResponseResult<SubjectExamTypeResponse> { Success = true, Data = MapToResponse(updated) });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Not found" });
        return Ok(new ResponseResult<object> { Success = true, Message = "Deleted successfully" });
    }

    private static SubjectExamTypeResponse MapToResponse(SubjectExamType e) => new()
    {
        Id = e.Id,
        SubjectId = e.SubjectId,
        SubjectName = e.Subject?.Name ?? string.Empty,
        Name = e.Name,
        Coefficient = e.Coefficient,
        RequiredCount = e.RequiredCount,
        SortOrder = e.SortOrder
    };
}
