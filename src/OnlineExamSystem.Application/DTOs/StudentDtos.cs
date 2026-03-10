using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Application.DTOs;

public class CreateStudentRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RollNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating student information
/// </summary>
public class UpdateStudentRequest
{
    /// <summary>Full name</summary>
    public string? FullName { get; set; }

    /// <summary>Email address</summary>
    public string? Email { get; set; }

    /// <summary>Roll number</summary>
    public string? RollNumber { get; set; }

    /// <summary>Active status</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for student response
/// </summary>
public class StudentResponse
{
    /// <summary>Student ID</summary>
    public long Id { get; set; }

    /// <summary>User ID</summary>
    public long UserId { get; set; }

    /// <summary>Username</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full name</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student code</summary>
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>Roll number</summary>
    public string RollNumber { get; set; } = string.Empty;

    /// <summary>Active status</summary>
    public bool IsActive { get; set; }

    /// <summary>Created date</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last updated date</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for student list with pagination
/// </summary>
public class StudentListResponse
{
    /// <summary>Total count</summary>
    public int TotalCount { get; set; }

    /// <summary>Page size</summary>
    public int PageSize { get; set; }

    /// <summary>Current page</summary>
    public int CurrentPage { get; set; }

    /// <summary>Total pages</summary>
    public int TotalPages { get; set; }

    /// <summary>Students</summary>
    public List<StudentResponse> Students { get; set; } = new();
}

/// <summary>
/// DTO for student's enrolled classes
/// </summary>
public class StudentClassEnrollmentResponse
{
    /// <summary>Enrollment ID</summary>
    public long Id { get; set; }

    /// <summary>Student ID</summary>
    public long StudentId { get; set; }

    /// <summary>Class ID</summary>
    public long ClassId { get; set; }

    /// <summary>Class name</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Class code</summary>
    public string ClassCode { get; set; } = string.Empty;

    /// <summary>Enrollment date</summary>
    public DateTime EnrolledAt { get; set; }
}
