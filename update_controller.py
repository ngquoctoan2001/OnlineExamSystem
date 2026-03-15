#!/usr/bin/env python3
"""Update QuestionsController.cs for PDF import with precise edits"""

import sys

file_path = r"src\OnlineExamSystem.API\Controllers\QuestionsController.cs"

#Read file
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Step 1: Find and add OfficeOpenXml using after line 6 (System.Security.Claims)
for i, line in enumerate(lines):
    if line.strip() == "using System.Security.Claims;":
        if i + 1 < len(lines) and lines[i + 1].strip() == "":
            lines[i + 1] = "using OfficeOpenXml;\n"
        else:
            lines.insert(i + 1, "using OfficeOpenXml;\n")
        print(f"Added OfficeOpenXml using at line {i+2}")
        break

# Step 2: Find and add PDF import fields after ITeachingAssignmentRepository line
for i, line in enumerate(lines):
    if "_teachingAssignmentRepository" in line and "private readonly" in line:
        # Found the field, add two new fields after it
        new_fields = [
            "    private readonly IPdfImportService _pdfImportService;\n",
            "    private readonly IImportService _importService;\n"
        ]
        lines[i+1:i+1] = new_fields
        print(f"Added PDF import fields after line {i+1}")
        break

# Step 3: Find constructor and add parameters
constructor_found = False
for i, line in enumerate(lines):
    if "public QuestionsController(" in line:
        constructor_found = True
        # Find the last parameter line (ends with "logger)")
        j = i
        while j < len(lines):
            if "ILogger<QuestionsController> logger)" in lines[j]:
                # Insert new parameters before this line
                new_params = [
                    "        IPdfImportService pdfImportService,\n",
                    "        IImportService importService,\n"
                ]
                lines[j:j] = new_params
                print(f"Added constructor parameters at line {j+1}")
                break
            j += 1
        break

# Step 4: Add field assignments in constructor body
for i, line in enumerate(lines):
    if "_teachingAssignmentRepository = teachingAssignmentRepository;" in line:
        new_assigns = [
            "        _pdfImportService = pdfImportService;\n",
            "        _importService = importService;\n"
        ]
        lines[i+1:i+1] = new_assigns
        print(f"Added field assignments after line {i+1}")
        break

# Step 5: Replace ImportFromPdf method
import_pdf_start = -1
import_pdf_end = -1
for i, line in enumerate(lines):
    if "[HttpPost(\"import/pdf\")]" in line:
        import_pdf_start = i - 2  # Include summary comment
        # Find the end of the method (closing brace)
        for j in range(i, len(lines)):
            if lines[j].strip() == "}" and j > i + 5:
                import_pdf_end = j
                break
        break

if import_pdf_start >= 0 and import_pdf_end >= 0:
    new_method = '''    /// <summary>
    /// Import questions from PDF (MCQ only - 4 options A, B, C, D)
    /// </summary>
    [HttpPost("import/pdf")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ImportResult>>> ImportFromPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "No file uploaded" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf")
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "Only .pdf files are supported" });

        if (file.Length > 50 * 1024 * 1024)
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "File size exceeds 50MB limit" });

        try
        {
            var userId = long.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
            if (userId == 0)
                return Unauthorized(new ResponseResult<ImportResult> { Success = false, Message = "User not authenticated" });

            using var stream = file.OpenReadStream();
            var pdfText = await _pdfImportService.ExtractTextFromPdfAsync(stream);

            if (string.IsNullOrWhiteSpace(pdfText))
                return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "PDF file is empty or cannot be read" });

            var importRows = _pdfImportService.ParseMcqQuestionsFromText(pdfText);

            if (importRows.Count == 0)
                return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "No valid MCQ questions found. Ensure questions have all 4 options (A,B,C,D) and answer specified." });

            var (success, result) = await _importService.ImportQuestionsAsync(ConvertImportRowsToStream(importRows), userId);

            _logger.LogInformation($"PDF import: {result.SuccessCount} success, {result.FailedCount} failed");
            return Ok(new ResponseResult<ImportResult> { Success = success, Message = success ? "PDF import completed successfully" : "PDF import completed with errors", Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing PDF");
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = $"Error: {ex.Message}" });
        }
    }
'''
    lines[import_pdf_start:import_pdf_end+1] = [new_method + "\n"]
    print(f"Replaced ImportFromPdf method (lines {import_pdf_start+1} to {import_pdf_end+1})")

# Step 6: Add helper method before Word import endpoint
for i, line in enumerate(lines):
    if '/// <summary>' in line and i + 1 < len(lines):
        if 'Import questions from Word' in lines[i + 1]:
            helper_method = '''    private Stream ConvertImportRowsToStream(List<ImportQuestionRow> rows)
    {
        var stream = new MemoryStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.Add("Questions");

        worksheet.Cells[1, 1].Value = "Content";
        worksheet.Cells[1, 2].Value = "QuestionType";
        worksheet.Cells[1, 3].Value = "Subject";
        worksheet.Cells[1, 4].Value = "Difficulty";
        worksheet.Cells[1, 5].Value = "OptionA";
        worksheet.Cells[1, 6].Value = "OptionB";
        worksheet.Cells[1, 7].Value = "OptionC";
        worksheet.Cells[1, 8].Value = "OptionD";
        worksheet.Cells[1, 9].Value = "CorrectOption";

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            worksheet.Cells[i + 2, 1].Value = row.Content;
            worksheet.Cells[i + 2, 2].Value = row.QuestionType ?? "MCQ";
            worksheet.Cells[i + 2, 3].Value = row.Subject;
            worksheet.Cells[i + 2, 4].Value = row.Difficulty ?? "MEDIUM";
            worksheet.Cells[i + 2, 5].Value = row.OptionA;
            worksheet.Cells[i + 2, 6].Value = row.OptionB;
            worksheet.Cells[i + 2, 7].Value = row.OptionC;
            worksheet.Cells[i + 2, 8].Value = row.OptionD;
            worksheet.Cells[i + 2, 9].Value = row.CorrectOption;
        }

        package.Save();
        stream.Position = 0;
        return stream;
    }

'''
            lines.insert(i, helper_method)
            print(f"Added ConvertImportRowsToStream helper method at line {i+1}")
            break

# Write back to file
with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)

print("\nQuestionsController.cs updated successfully!")
print("Next step: dotnet build to verify compilation")
