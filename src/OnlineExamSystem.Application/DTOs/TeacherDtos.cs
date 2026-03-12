namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// DTO for creating a new teacher
/// </summary>
public class CreateTeacherRequest
{
    /// <summary>Username (will create User account)</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password for initial account</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Full name of teacher</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Employee/Staff ID (unique)</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>Department (e.g., Math, English)</summary>
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating teacher information
/// </summary>
public class UpdateTeacherRequest
{
    /// <summary>Full name</summary>
    public string? FullName { get; set; }

    /// <summary>Email address</summary>
    public string? Email { get; set; }

    /// <summary>Department</summary>
    public string? Department { get; set; }

    /// <summary>Active status</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for teacher response
/// </summary>
public class TeacherResponse
{
    /// <summary>Teacher ID</summary>
    public long Id { get; set; }

    /// <summary>User ID</summary>
    public long UserId { get; set; }

    /// <summary>Username</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Full name</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Employee/Staff ID</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>Department</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Active status</summary>
    public bool IsActive { get; set; }

    /// <summary>Created date</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last updated date</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for teacher list with pagination
/// </summary>
public class TeacherListResponse
{
    /// <summary>Total count</summary>
    public int TotalCount { get; set; }

    /// <summary>Page size</summary>
    public int PageSize { get; set; }

    /// <summary>Current page</summary>
    public int CurrentPage { get; set; }

    /// <summary>Total pages</summary>
    public int TotalPages { get; set; }

    /// <summary>Teachers</summary>
    public List<TeacherResponse> Teachers { get; set; } = new();
}

/// <summary>
/// DTO for teacher's assigned classes
/// </summary>
public class TeacherClassAssignmentResponse
{
    /// <summary>Assignment ID</summary>
    public long Id { get; set; }

    /// <summary>Teacher ID</summary>
    public long TeacherId { get; set; }

    /// <summary>Class ID</summary>
    public long ClassId { get; set; }

    /// <summary>Class name</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Class code</summary>
    public string ClassCode { get; set; } = string.Empty;

    /// <summary>Subject ID</summary>
    public long SubjectId { get; set; }

    /// <summary>Subject name</summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>Assignment date</summary>
    public DateTime AssignedAt { get; set; }
}

public class TeacherSubjectResponse
{
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
}
