namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// DTO for creating a new class
/// </summary>
public class CreateClassRequest
{
    /// <summary>School ID</summary>
    public long SchoolId { get; set; }

    /// <summary>Class code (unique)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Class name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Grade level (10, 11, 12)</summary>
    public int Grade { get; set; }
}

/// <summary>
/// DTO for updating class information
/// </summary>
public class UpdateClassRequest
{
    /// <summary>Class name</summary>
    public string? Name { get; set; }

    /// <summary>Grade level</summary>
    public int? Grade { get; set; }
}

/// <summary>
/// DTO for class response
/// </summary>
public class ClassResponse
{
    /// <summary>Class ID</summary>
    public long Id { get; set; }

    /// <summary>School ID</summary>
    public long SchoolId { get; set; }

    /// <summary>Class code</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Class name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Grade level</summary>
    public int Grade { get; set; }

    /// <summary>Number of students</summary>
    public int StudentCount { get; set; }

    /// <summary>Number of teachers assigned</summary>
    public int TeacherCount { get; set; }
}

/// <summary>
/// DTO for class list with pagination
/// </summary>
public class ClassListResponse
{
    /// <summary>Total count</summary>
    public int TotalCount { get; set; }

    /// <summary>Page size</summary>
    public int PageSize { get; set; }

    /// <summary>Current page</summary>
    public int CurrentPage { get; set; }

    /// <summary>Total pages</summary>
    public int TotalPages { get; set; }

    /// <summary>Classes</summary>
    public List<ClassResponse> Classes { get; set; } = new();
}

/// <summary>
/// DTO for adding student to class
/// </summary>
public class AddStudentToClassRequest
{
    /// <summary>Student ID</summary>
    public long StudentId { get; set; }
}

/// <summary>
/// DTO for student in class
/// </summary>
public class ClassStudentResponse
{
    /// <summary>Student ID</summary>
    public long StudentId { get; set; }

    /// <summary>Username</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Full name</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student code</summary>
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>Roll number</summary>
    public string RollNumber { get; set; } = string.Empty;

    /// <summary>Enrollment date</summary>
    public DateTime EnrolledAt { get; set; }
}
