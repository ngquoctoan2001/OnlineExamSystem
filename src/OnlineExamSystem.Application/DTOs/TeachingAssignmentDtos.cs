namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// Request DTO for creating a teaching assignment
/// </summary>
public class CreateTeachingAssignmentRequest
{
    /// <summary>
    /// Class ID
    /// </summary>
    public long ClassId { get; set; }

    /// <summary>
    /// Teacher ID
    /// </summary>
    public long TeacherId { get; set; }

    /// <summary>
    /// Subject ID
    /// </summary>
    public long SubjectId { get; set; }

    /// <summary>
    /// Academic year (e.g., 2023-2024)
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Semester (1 or 2)
    /// </summary>
    public int Semester { get; set; }
}

/// <summary>
/// Request DTO for updating a teaching assignment
/// </summary>
public class UpdateTeachingAssignmentRequest
{
    /// <summary>
    /// Subject ID
    /// </summary>
    public long SubjectId { get; set; }

    /// <summary>
    /// Academic year (e.g., 2023-2024)
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Semester (1 or 2)
    /// </summary>
    public int Semester { get; set; }
}

/// <summary>
/// Response DTO for teaching assignment details
/// </summary>
public class TeachingAssignmentResponse
{
    /// <summary>
    /// Teaching assignment ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Class ID
    /// </summary>
    public long ClassId { get; set; }

    /// <summary>
    /// Class name
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Teacher ID
    /// </summary>
    public long TeacherId { get; set; }

    /// <summary>
    /// Teacher name
    /// </summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>
    /// Subject ID
    /// </summary>
    public long SubjectId { get; set; }

    /// <summary>
    /// Subject name
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Subject code
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Academic year (e.g., 2023-2024)
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Semester (1 or 2)
    /// </summary>
    public int Semester { get; set; }

    /// <summary>
    /// Assignment date
    /// </summary>
    public DateTime AssignedDate { get; set; }
}

/// <summary>
/// Response DTO for list of teaching assignments
/// </summary>
public class TeachingAssignmentListResponse
{
    /// <summary>
    /// List of teaching assignments
    /// </summary>
    public List<TeachingAssignmentResponse> Items { get; set; } = new();

    /// <summary>
    /// Total count of all assignments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// Response DTO for teacher assignments in a class
/// </summary>
public class TeacherAssignmentResponse
{
    /// <summary>
    /// Teaching assignment ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Teacher ID
    /// </summary>
    public long TeacherId { get; set; }

    /// <summary>
    /// Teacher name
    /// </summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>
    /// Subject ID
    /// </summary>
    public long SubjectId { get; set; }

    /// <summary>
    /// Subject name
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Subject code
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Academic year
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Semester
    /// </summary>
    public int Semester { get; set; }

    /// <summary>
    /// Assignment date
    /// </summary>
    public DateTime AssignedDate { get; set; }
}

/// <summary>
/// Response DTO for subject assignments for a teacher
/// </summary>
public class SubjectAssignmentResponse
{
    /// <summary>
    /// Teaching assignment ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Class ID
    /// </summary>
    public long ClassId { get; set; }

    /// <summary>
    /// Class name
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Subject ID
    /// </summary>
    public long SubjectId { get; set; }

    /// <summary>
    /// Subject name
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Subject code
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Academic year
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Semester
    /// </summary>
    public int Semester { get; set; }

    /// <summary>
    /// Assignment date
    /// </summary>
    public DateTime AssignedDate { get; set; }
}
