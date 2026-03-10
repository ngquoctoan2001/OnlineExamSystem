using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(long id)
        => await _context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tag?> GetByNameAsync(string name)
        => await _context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

    public async Task<List<Tag>> GetAllAsync()
        => await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<List<Tag>> SearchAsync(string searchTerm)
        => await _context.Tags
            .AsNoTracking()
            .Where(t => t.Name.Contains(searchTerm) || (t.Description != null && t.Description.Contains(searchTerm)))
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<Tag> CreateAsync(Tag tag)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag)
    {
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);
        if (tag == null) return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> NameExistsAsync(string name, long? excludeId = null)
        => await _context.Tags.AnyAsync(t => t.Name.ToLower() == name.ToLower() && (excludeId == null || t.Id != excludeId));

    public async Task<List<Tag>> GetTagsByQuestionAsync(long questionId)
        => await _context.QuestionTags
            .AsNoTracking()
            .Where(qt => qt.QuestionId == questionId)
            .Select(qt => qt.Tag)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<List<Question>> GetQuestionsByTagAsync(long tagId)
        => await _context.QuestionTags
            .AsNoTracking()
            .Where(qt => qt.TagId == tagId)
            .Select(qt => qt.Question)
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .ToListAsync();

    public async Task<bool> AssignTagToQuestionAsync(long questionId, long tagId)
    {
        if (await IsTagAssignedToQuestionAsync(questionId, tagId))
            return true;

        _context.QuestionTags.Add(new QuestionTag
        {
            QuestionId = questionId,
            TagId = tagId,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTagFromQuestionAsync(long questionId, long tagId)
    {
        var qt = await _context.QuestionTags
            .FirstOrDefaultAsync(x => x.QuestionId == questionId && x.TagId == tagId);
        if (qt == null) return false;

        _context.QuestionTags.Remove(qt);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsTagAssignedToQuestionAsync(long questionId, long tagId)
        => await _context.QuestionTags
            .AnyAsync(qt => qt.QuestionId == questionId && qt.TagId == tagId);
}
