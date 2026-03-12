using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Application.DTOs;

public class CreateSubjectExamTypeRequest
{
    [Required]
    public long SubjectId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.1, 10)]
    public decimal Coefficient { get; set; } = 1;

    [Range(0, 50)]
    public int RequiredCount { get; set; } = 1;

    public int SortOrder { get; set; }
}

public class UpdateSubjectExamTypeRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [Range(0.1, 10)]
    public decimal? Coefficient { get; set; }

    [Range(0, 50)]
    public int? RequiredCount { get; set; }

    public int? SortOrder { get; set; }
}

public class SubjectExamTypeResponse
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Coefficient { get; set; }
    public int RequiredCount { get; set; }
    public int SortOrder { get; set; }
}
