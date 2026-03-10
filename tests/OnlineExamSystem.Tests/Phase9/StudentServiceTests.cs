using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase9;

public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<ILogger<StudentService>> _loggerMock = new();
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _service = new StudentService(
            _studentRepoMock.Object,
            _userRepoMock.Object,
            _hasherMock.Object,
            _loggerMock.Object);
    }

    private static Student BuildStudent(long id = 1) => new()
    {
        Id = id,
        StudentCode = "SV001",
        RollNumber = "R001",
        User = new User { Id = 10, Username = "student1", Email = "s1@test.com", FullName = "Student One", IsActive = true }
    };

    // ===== GetStudentByIdAsync =====

    [Fact]
    public async Task GetStudentById_NotFound_ReturnsFalse()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Student?)null);

        var (success, message, _) = await _service.GetStudentByIdAsync(99);

        success.Should().BeFalse();
        message.Should().Be("Student not found");
    }

    [Fact]
    public async Task GetStudentById_Found_ReturnsStudent()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildStudent());

        var (success, _, data) = await _service.GetStudentByIdAsync(1);

        success.Should().BeTrue();
        data!.StudentCode.Should().Be("SV001");
    }

    // ===== GetAllStudentsAsync =====

    [Fact]
    public async Task GetAllStudents_InvalidPage_ReturnsFalse()
    {
        var (success, message, _) = await _service.GetAllStudentsAsync(0, 20);

        success.Should().BeFalse();
        message.Should().Contain("greater than 0");
    }

    [Fact]
    public async Task GetAllStudents_ValidPaging_ReturnsList()
    {
        _studentRepoMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync((new List<Student> { BuildStudent() }, 1));

        var (success, _, data) = await _service.GetAllStudentsAsync(1, 20);

        success.Should().BeTrue();
        data!.TotalCount.Should().Be(1);
        data.Students.Should().HaveCount(1);
    }

    // ===== SearchStudentsAsync =====

    [Fact]
    public async Task SearchStudents_EmptyTerm_ReturnsFalse()
    {
        var (success, message, _) = await _service.SearchStudentsAsync("  ");

        success.Should().BeFalse();
        message.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task SearchStudents_ValidTerm_ReturnsResults()
    {
        _studentRepoMock.Setup(r => r.SearchAsync("SV001")).ReturnsAsync(new List<Student> { BuildStudent() });

        var (success, _, data) = await _service.SearchStudentsAsync("SV001");

        success.Should().BeTrue();
        data.Should().HaveCount(1);
    }

    // ===== CreateStudentAsync =====

    [Fact]
    public async Task CreateStudent_MissingFields_ReturnsFalse()
    {
        var (success, message, _) = await _service.CreateStudentAsync(new CreateStudentRequest
        {
            Username = "", Email = "e@t.com", Password = "pass", FullName = "Name", StudentCode = "S1"
        });

        success.Should().BeFalse();
        message.Should().Contain("required");
    }

    [Fact]
    public async Task CreateStudent_DuplicateUsername_ReturnsFalse()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync("student1"))
            .ReturnsAsync(new User { Id = 1, Username = "student1" });

        var (success, message, _) = await _service.CreateStudentAsync(new CreateStudentRequest
        {
            Username = "student1", Email = "new@test.com", Password = "pass123",
            FullName = "New Student", StudentCode = "S999"
        });

        success.Should().BeFalse();
        message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateStudent_DuplicateStudentCode_ReturnsFalse()
    {
        _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _studentRepoMock.Setup(r => r.StudentCodeExistsAsync("SV001")).ReturnsAsync(true);

        var (success, message, _) = await _service.CreateStudentAsync(new CreateStudentRequest
        {
            Username = "newuser", Email = "new@test.com", Password = "pass123",
            FullName = "New Student", StudentCode = "SV001"
        });

        success.Should().BeFalse();
        message.Should().Contain("Student code already exists");
    }

    [Fact]
    public async Task CreateStudent_Valid_ReturnsSuccess()
    {
        var newUser = new User { Id = 99, Username = "newstudent", Email = "n@t.com", FullName = "New Student", IsActive = true };
        var newStudent = new Student { Id = 5, UserId = 99, StudentCode = "SV999", User = newUser };

        _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _studentRepoMock.Setup(r => r.StudentCodeExistsAsync("SV999")).ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword("secure123")).Returns("hashed");
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync(newUser);
        _studentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Student>())).ReturnsAsync(newStudent);

        var (success, message, data) = await _service.CreateStudentAsync(new CreateStudentRequest
        {
            Username = "newstudent", Email = "n@t.com", Password = "secure123",
            FullName = "New Student", StudentCode = "SV999"
        });

        success.Should().BeTrue();
        message.Should().Contain("successfully");
        data!.StudentCode.Should().Be("SV999");
    }
}
