using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnswerRepository> _logger;

    public AnswerRepository(ApplicationDbContext context, ILogger<AnswerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Answer?> GetByAttemptAndQuestionAsync(long attemptId, long questionId)
    {
        try
        {
            return await _context.Answers
                .Include(a => a.AnswerOptions)
                .FirstOrDefaultAsync(a => a.ExamAttemptId == attemptId && a.QuestionId == questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting answer for attempt {AttemptId}, question {QuestionId}", attemptId, questionId);
            return null;
        }
    }

    public async Task<List<Answer>> GetByAttemptIdAsync(long attemptId)
    {
        try
        {
            return await _context.Answers
                .Include(a => a.AnswerOptions)
                .Where(a => a.ExamAttemptId == attemptId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting answers for attempt {AttemptId}", attemptId);
            return new List<Answer>();
        }
    }

    public async Task<Answer> CreateAsync(Answer answer)
    {
        _context.Answers.Add(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    public async Task<Answer> UpdateAsync(Answer answer)
    {
        _context.Answers.Update(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    public async Task<bool> CreateAnswerOptionAsync(AnswerOption answerOption)
    {
        try
        {
            _context.AnswerOptions.Add(answerOption);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating answer option for answer {AnswerId}, option {OptionId}", answerOption.AnswerId, answerOption.OptionId);
            return false;
        }
    }

    public async Task DeleteAnswerOptionsByAnswerIdAsync(long answerId)
    {
        var options = await _context.AnswerOptions
            .Where(ao => ao.AnswerId == answerId)
            .ToListAsync();
        _context.AnswerOptions.RemoveRange(options);
        await _context.SaveChangesAsync();
    }
}
