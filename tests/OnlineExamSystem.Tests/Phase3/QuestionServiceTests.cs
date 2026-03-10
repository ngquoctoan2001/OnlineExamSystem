using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase3;

public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _questionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<ISubjectRepository> _subjectRepoMock = new();
    private readonly Mock<ITagRepository> _tagRepoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();
    private readonly QuestionService _service;

    public QuestionServiceTests()
    {
        _service = new QuestionService(
            _questionRepoMock.Object,
            _optionRepoMock.Object,
            _subjectRepoMock.Object,
            _tagRepoMock.Object,
            _loggerMock.Object);
    }

    // ─── CreateQuestion ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateQuestion_SubjectNotFound_ReturnsFalse()
    {
        _subjectRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Subject?)null);

        var (success, message, data) = await _service.CreateQuestionAsync(
            new CreateQuestionRequest { SubjectId = 99, QuestionTypeId = 1, Content = "Q?" }, createdBy: 1);

        success.Should().BeFalse();
        message.Should().Contain("Subject not found");
        data.Should().BeNull();
    }

    [Fact]
    public async Task CreateQuestion_Valid_ReturnsQuestion()
    {
        _subjectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Subject { Id = 1, Name = "Math" });
        _questionRepoMock.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .Callback<Question>(q => q.Id = 10)
            .ReturnsAsync((Question q) => q);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(10)).ReturnsAsync(new List<QuestionOption>());

        var request = new CreateQuestionRequest
        {
            SubjectId = 1,
            QuestionTypeId = 2,
            Content = "What is 2+2?",
            Difficulty = "EASY"
        };

        var (success, message, data) = await _service.CreateQuestionAsync(request, createdBy: 5);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Content.Should().Be("What is 2+2?");
        data.Difficulty.Should().Be("EASY");
    }

    [Fact]
    public async Task CreateQuestion_WithOptions_CreatesOptionsInBatch()
    {
        _subjectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Subject { Id = 1 });
        _questionRepoMock.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .Callback<Question>(q => q.Id = 20)
            .ReturnsAsync((Question q) => q);
        _optionRepoMock.Setup(r => r.CreateBatchAsync(It.IsAny<List<QuestionOption>>()))
            .ReturnsAsync((List<QuestionOption> opts) => opts);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(20)).ReturnsAsync(new List<QuestionOption>
        {
            new() { Id = 1, QuestionId = 20, Label = "A", Content = "Yes", IsCorrect = true, OrderIndex = 0 }
        });

        var request = new CreateQuestionRequest
        {
            SubjectId = 1, QuestionTypeId = 1, Content = "MCQ?",
            Options = new List<CreateQuestionOptionRequest>
            {
                new() { Label = "A", Content = "Yes", IsCorrect = true, OrderIndex = 0 }
            }
        };

        var (success, _, data) = await _service.CreateQuestionAsync(request, createdBy: 1);

        success.Should().BeTrue();
        data!.Options.Should().HaveCount(1);
        data.Options[0].Label.Should().Be("A");
        _optionRepoMock.Verify(r => r.CreateBatchAsync(It.IsAny<List<QuestionOption>>()), Times.Once);
    }

    // ─── GetQuestionById ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionById_NotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message, data) = await _service.GetQuestionByIdAsync(999);

        success.Should().BeFalse();
        message.Should().Contain("not found");
        data.Should().BeNull();
    }

    [Fact]
    public async Task GetQuestionById_Found_ReturnsDetail()
    {
        var question = new Question { Id = 5, Content = "Test Q", Difficulty = "MEDIUM", SubjectId = 1, QuestionTypeId = 1 };
        _questionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(question);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(5)).ReturnsAsync(new List<QuestionOption>());

        var (success, _, data) = await _service.GetQuestionByIdAsync(5);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Id.Should().Be(5);
    }

    // ─── UpdateQuestion ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateQuestion_NotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message, _) = await _service.UpdateQuestionAsync(1, new UpdateQuestionRequest { Content = "X", Difficulty = "EASY" });

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateQuestion_Valid_UpdatesFields()
    {
        var question = new Question { Id = 3, Content = "Old", Difficulty = "EASY" };
        _questionRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(question);
        _questionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Question>())).ReturnsAsync((Question q) => q);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(3)).ReturnsAsync(new List<QuestionOption>());

        var (success, _, data) = await _service.UpdateQuestionAsync(3, new UpdateQuestionRequest { Content = "New", Difficulty = "HARD" });

        success.Should().BeTrue();
        data!.Content.Should().Be("New");
        data.Difficulty.Should().Be("HARD");
    }

    // ─── DeleteQuestion ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteQuestion_NotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message) = await _service.DeleteQuestionAsync(999);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteQuestion_Valid_DeletesOptionsAndQuestion()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Question { Id = 7 });
        _optionRepoMock.Setup(r => r.DeleteByQuestionIdAsync(7)).ReturnsAsync(true);
        _questionRepoMock.Setup(r => r.DeleteAsync(7)).ReturnsAsync(true);

        var (success, _) = await _service.DeleteQuestionAsync(7);

        success.Should().BeTrue();
        _optionRepoMock.Verify(r => r.DeleteByQuestionIdAsync(7), Times.Once);
        _questionRepoMock.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    // ─── Publish / Unpublish ──────────────────────────────────────────────────

    [Fact]
    public async Task PublishQuestion_NotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, _) = await _service.PublishQuestionAsync(1);

        success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishQuestion_Valid_SetsIsPublishedTrue()
    {
        var question = new Question { Id = 1, IsPublished = false };
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
        _questionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Question>())).ReturnsAsync((Question q) => q);

        var (success, _) = await _service.PublishQuestionAsync(1);

        success.Should().BeTrue();
        question.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task UnpublishQuestion_Valid_SetsIsPublishedFalse()
    {
        var question = new Question { Id = 2, IsPublished = true };
        _questionRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(question);
        _questionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Question>())).ReturnsAsync((Question q) => q);

        var (success, _) = await _service.UnpublishQuestionAsync(2);

        success.Should().BeTrue();
        question.IsPublished.Should().BeFalse();
    }

    // ─── Difficulty validation ────────────────────────────────────────────────

    [Theory]
    [InlineData("EASY")]
    [InlineData("MEDIUM")]
    [InlineData("HARD")]
    public async Task GetByDifficulty_ValidLevel_ReturnsSuccess(string difficulty)
    {
        _questionRepoMock.Setup(r => r.GetByDifficultyAsync(difficulty)).ReturnsAsync(new List<Question>());
        _optionRepoMock.Setup(r => r.GetCountByQuestionIdAsync(It.IsAny<long>())).ReturnsAsync(0);

        var (success, _, _) = await _service.GetQuestionsByDifficultyAsync(difficulty);

        success.Should().BeTrue();
    }

    [Theory]
    [InlineData("VERY_HARD")]
    [InlineData("IMPOSSIBLE")]
    [InlineData("")]
    public async Task GetByDifficulty_InvalidLevel_ReturnsFalse(string difficulty)
    {
        var (success, message, _) = await _service.GetQuestionsByDifficultyAsync(difficulty);

        success.Should().BeFalse();
        message.Should().Contain("Invalid difficulty");
    }

    // ─── Option management ────────────────────────────────────────────────────

    [Fact]
    public async Task AddOption_QuestionNotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message, _) = await _service.AddOptionAsync(99, new CreateQuestionOptionRequest { Label = "A", Content = "X" });

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task AddOption_Valid_CreatesOption()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Question { Id = 1 });
        _optionRepoMock.Setup(r => r.CreateAsync(It.IsAny<QuestionOption>()))
            .Callback<QuestionOption>(o => o.Id = 100)
            .ReturnsAsync((QuestionOption o) => o);

        var request = new CreateQuestionOptionRequest { Label = "A", Content = "Option A", IsCorrect = true, OrderIndex = 0 };
        var (success, _, data) = await _service.AddOptionAsync(1, request);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Label.Should().Be("A");
        data.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOption_WrongQuestion_ReturnsFalse()
    {
        _optionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new QuestionOption { Id = 5, QuestionId = 2 });

        var (success, message, _) = await _service.UpdateOptionAsync(questionId: 1, optionId: 5, new CreateQuestionOptionRequest { Label = "B", Content = "Y" });

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateOption_Valid_UpdatesOption()
    {
        var option = new QuestionOption { Id = 5, QuestionId = 1, Label = "A", Content = "Old" };
        _optionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(option);
        _optionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<QuestionOption>())).ReturnsAsync((QuestionOption o) => o);

        var (success, _, data) = await _service.UpdateOptionAsync(1, 5, new CreateQuestionOptionRequest { Label = "A", Content = "New", IsCorrect = true, OrderIndex = 0 });

        success.Should().BeTrue();
        data!.Content.Should().Be("New");
    }

    [Fact]
    public async Task DeleteOption_WrongQuestion_ReturnsFalse()
    {
        _optionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new QuestionOption { Id = 5, QuestionId = 2 });

        var (success, message) = await _service.DeleteOptionAsync(questionId: 1, optionId: 5);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteOption_Valid_CallsDelete()
    {
        _optionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new QuestionOption { Id = 5, QuestionId = 1 });
        _optionRepoMock.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

        var (success, _) = await _service.DeleteOptionAsync(1, 5);

        success.Should().BeTrue();
        _optionRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    // ─── Tag management ───────────────────────────────────────────────────────

    [Fact]
    public async Task AssignTag_QuestionNotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message) = await _service.AssignTagAsync(99, 1);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task AssignTag_TagNotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Question { Id = 1 });
        _tagRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tag?)null);

        var (success, message) = await _service.AssignTagAsync(1, 99);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task AssignTag_Valid_AssignsTag()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Question { Id = 1 });
        _tagRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Tag { Id = 2, Name = "Chapter1" });
        _tagRepoMock.Setup(r => r.AssignTagToQuestionAsync(1, 2)).ReturnsAsync(true);

        var (success, _) = await _service.AssignTagAsync(1, 2);

        success.Should().BeTrue();
        _tagRepoMock.Verify(r => r.AssignTagToQuestionAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task RemoveTag_NotFound_ReturnsFalse()
    {
        _tagRepoMock.Setup(r => r.RemoveTagFromQuestionAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(false);

        var (success, message) = await _service.RemoveTagAsync(1, 99);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetQuestionTags_QuestionNotFound_ReturnsFalse()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Question?)null);

        var (success, message, _) = await _service.GetQuestionTagsAsync(99);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetQuestionTags_Valid_ReturnsTags()
    {
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Question { Id = 1 });
        _tagRepoMock.Setup(r => r.GetTagsByQuestionAsync(1)).ReturnsAsync(new List<Tag>
        {
            new() { Id = 10, Name = "Chapter1", CreatedAt = DateTime.UtcNow },
            new() { Id = 11, Name = "Chapter2", CreatedAt = DateTime.UtcNow }
        });

        var (success, _, data) = await _service.GetQuestionTagsAsync(1);

        success.Should().BeTrue();
        data.Should().HaveCount(2);
        data!.Select(t => t.Name).Should().Contain("Chapter1");
    }
}
