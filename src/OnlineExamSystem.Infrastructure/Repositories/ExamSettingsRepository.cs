using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamSettingsRepository : IExamSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public ExamSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExamSetting?> GetByExamIdAsync(long examId)
    {
        return await _context.ExamSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ExamId == examId);
    }

    public async Task<ExamSetting> CreateAsync(ExamSetting settings)
    {
        _context.ExamSettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<ExamSetting> UpdateAsync(ExamSetting settings)
    {
        _context.ExamSettings.Update(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<bool> DeleteAsync(long examId)
    {
        var settings = await _context.ExamSettings
            .FirstOrDefaultAsync(s => s.ExamId == examId);
        if (settings == null)
            return false;

        _context.ExamSettings.Remove(settings);
        await _context.SaveChangesAsync();
        return true;
    }
}
