using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Repositories;
using Xunit;

namespace OnlineExamSystem.Tests.Phase3;

public class TagRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<(ApplicationDbContext ctx, Subject subject, Question question)> SeedAsync(ApplicationDbContext ctx)
    {
        var subject = new Subject { Code = "MATH", Name = "Math" };
        ctx.Subjects.Add(subject);

        var qt = new QuestionType { Name = "MCQ", Description = "Multiple choice" };
        ctx.QuestionTypes.Add(qt);

        await ctx.SaveChangesAsync();

        var question = new Question
        {
            SubjectId = subject.Id,
            QuestionTypeId = qt.Id,
            Content = "What is 2+2?",
            CreatedBy = 1,
            Difficulty = "EASY"
        };
        ctx.Questions.Add(question);
        await ctx.SaveChangesAsync();

        return (ctx, subject, question);
    }

    [Fact]
    public async Task CreateTag_PersistsToDatabase()
    {
        using var ctx = CreateInMemoryContext(nameof(CreateTag_PersistsToDatabase));
        var repo = new TagRepository(ctx);

        var tag = await repo.CreateAsync(new Tag { Name = "Chapter1", Description = "First chapter" });

        tag.Id.Should().BeGreaterThan(0);
        var inDb = await ctx.Tags.FindAsync(tag.Id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be("Chapter1");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTag_ReturnsTag()
    {
        using var ctx = CreateInMemoryContext(nameof(GetByIdAsync_ExistingTag_ReturnsTag));
        ctx.Tags.Add(new Tag { Name = "TestTag" });
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        var tag = await repo.GetByIdAsync(1);

        tag.Should().NotBeNull();
        tag!.Name.Should().Be("TestTag");
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        using var ctx = CreateInMemoryContext(nameof(GetByIdAsync_NonExisting_ReturnsNull));
        var repo = new TagRepository(ctx);

        var tag = await repo.GetByIdAsync(999);

        tag.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive_ReturnsTag()
    {
        using var ctx = CreateInMemoryContext(nameof(GetByNameAsync_CaseInsensitive_ReturnsTag));
        ctx.Tags.Add(new Tag { Name = "Topic" });
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        var tag = await repo.GetByNameAsync("topic");

        tag.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSortedByName()
    {
        using var ctx = CreateInMemoryContext(nameof(GetAllAsync_ReturnsSortedByName));
        ctx.Tags.AddRange(new Tag { Name = "Zebra" }, new Tag { Name = "Alpha" }, new Tag { Name = "Middle" });
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        var tags = await repo.GetAllAsync();

        tags.Select(t => t.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task UpdateTag_PersistsChanges()
    {
        using var ctx = CreateInMemoryContext(nameof(UpdateTag_PersistsChanges));
        var tag = new Tag { Name = "OldName" };
        ctx.Tags.Add(tag);
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        tag.Name = "NewName";
        await repo.UpdateAsync(tag);

        var updated = await ctx.Tags.FindAsync(tag.Id);
        updated!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_ExistingTag_RemovesFromDb()
    {
        using var ctx = CreateInMemoryContext(nameof(DeleteAsync_ExistingTag_RemovesFromDb));
        var tag = new Tag { Name = "ToDelete" };
        ctx.Tags.Add(tag);
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        var result = await repo.DeleteAsync(tag.Id);

        result.Should().BeTrue();
        (await ctx.Tags.FindAsync(tag.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ReturnsFalse()
    {
        using var ctx = CreateInMemoryContext(nameof(DeleteAsync_NonExisting_ReturnsFalse));
        var repo = new TagRepository(ctx);

        var result = await repo.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_ExistingName_ReturnsTrue()
    {
        using var ctx = CreateInMemoryContext(nameof(NameExistsAsync_ExistingName_ReturnsTrue));
        ctx.Tags.Add(new Tag { Name = "Existing" });
        await ctx.SaveChangesAsync();
        var repo = new TagRepository(ctx);

        var exists = await repo.NameExistsAsync("existing");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AssignTagToQuestion_Twice_DoesNotDuplicate()
    {
        using var ctx = CreateInMemoryContext(nameof(AssignTagToQuestion_Twice_DoesNotDuplicate));
        var (_, _, question) = await SeedAsync(ctx);

        var tag = new Tag { Name = "Physics" };
        ctx.Tags.Add(tag);
        await ctx.SaveChangesAsync();

        var repo = new TagRepository(ctx);

        // First assignment
        await repo.AssignTagToQuestionAsync(question.Id, tag.Id);
        // Second assignment — should be idempotent
        await repo.AssignTagToQuestionAsync(question.Id, tag.Id);

        var assignments = await ctx.QuestionTags.Where(qt => qt.QuestionId == question.Id).ToListAsync();
        assignments.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveTagFromQuestion_ExistingAssignment_Removes()
    {
        using var ctx = CreateInMemoryContext(nameof(RemoveTagFromQuestion_ExistingAssignment_Removes));
        var (_, _, question) = await SeedAsync(ctx);

        var tag = new Tag { Name = "Remove" };
        ctx.Tags.Add(tag);
        await ctx.SaveChangesAsync();
        ctx.QuestionTags.Add(new QuestionTag { QuestionId = question.Id, TagId = tag.Id });
        await ctx.SaveChangesAsync();

        var repo = new TagRepository(ctx);
        var removed = await repo.RemoveTagFromQuestionAsync(question.Id, tag.Id);

        removed.Should().BeTrue();
        (await ctx.QuestionTags.AnyAsync(qt => qt.QuestionId == question.Id && qt.TagId == tag.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetTagsByQuestion_ReturnsOnlyAssignedTags()
    {
        using var ctx = CreateInMemoryContext(nameof(GetTagsByQuestion_ReturnsOnlyAssignedTags));
        var (_, _, question) = await SeedAsync(ctx);

        var tag1 = new Tag { Name = "Tag1" };
        var tag2 = new Tag { Name = "Tag2" };
        var otherTag = new Tag { Name = "OtherTag" };
        ctx.Tags.AddRange(tag1, tag2, otherTag);
        await ctx.SaveChangesAsync();

        ctx.QuestionTags.AddRange(
            new QuestionTag { QuestionId = question.Id, TagId = tag1.Id },
            new QuestionTag { QuestionId = question.Id, TagId = tag2.Id }
        );
        await ctx.SaveChangesAsync();

        var repo = new TagRepository(ctx);
        var tags = await repo.GetTagsByQuestionAsync(question.Id);

        tags.Should().HaveCount(2);
        tags.Select(t => t.Name).Should().Contain("Tag1").And.Contain("Tag2").And.NotContain("OtherTag");
    }
}
