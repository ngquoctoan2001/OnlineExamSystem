using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Repositories;
using Xunit;

namespace OnlineExamSystem.Tests.Phase2;

public class ExamSettingsRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExamSettingsRepository _repo;

    public ExamSettingsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repo = new ExamSettingsRepository(_context);

        SeedData();
    }

    private void SeedData()
    {
        // Seed a subject
        var subject = new Subject { Id = 1, Name = "Math", Code = "MATH" };
        _context.Subjects.Add(subject);

        // Seed an exam
        var exam = new Exam
        {
            Id = 1,
            SubjectId = 1,
            CreatedBy = 1,
            Title = "Test Exam",
            DurationMinutes = 60,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = "DRAFT"
        };
        _context.Exams.Add(exam);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByExamIdAsync_NoSettings_ReturnsNull()
    {
        var result = await _repo.GetByExamIdAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidSettings_PersistsAndReturns()
    {
        var settings = new ExamSetting
        {
            ExamId = 1,
            ShuffleQuestions = true,
            ShuffleAnswers = true,
            ShowResultImmediately = false,
            AllowReview = true
        };

        var created = await _repo.CreateAsync(settings);

        created.Id.Should().BeGreaterThan(0);
        created.ShuffleQuestions.Should().BeTrue();
        created.AllowReview.Should().BeTrue();
    }

    [Fact]
    public async Task GetByExamIdAsync_SettingsExist_ReturnsSettings()
    {
        var settings = new ExamSetting { ExamId = 1, ShuffleQuestions = true };
        _context.ExamSettings.Add(settings);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByExamIdAsync(1);

        result.Should().NotBeNull();
        result!.ShuffleQuestions.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ExistingSettings_UpdatesValues()
    {
        var settings = new ExamSetting { ExamId = 1, ShuffleQuestions = false, AllowReview = false };
        _context.ExamSettings.Add(settings);
        await _context.SaveChangesAsync();

        settings.ShuffleQuestions = true;
        settings.AllowReview = true;
        var updated = await _repo.UpdateAsync(settings);

        updated.ShuffleQuestions.Should().BeTrue();
        updated.AllowReview.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ExistingSettings_RemovesFromDb()
    {
        var settings = new ExamSetting { ExamId = 1 };
        _context.ExamSettings.Add(settings);
        await _context.SaveChangesAsync();

        var result = await _repo.DeleteAsync(1);

        result.Should().BeTrue();
        var check = await _repo.GetByExamIdAsync(1);
        check.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSettings_ReturnsFalse()
    {
        var result = await _repo.DeleteAsync(999);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
