# Update QuestionsController.cs for PDF Import
$filePath = "src\OnlineExamSystem.API\Controllers\QuestionsController.cs"
$content = Get-Content -Path $filePath -Raw

# Replace the ImportFromPdf placeholder method
$oldMethod = '    /// <summary>
    /// Import questions from PDF
    /// </summary>
    [HttpPost("import/pdf")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> ImportFromPdf()
    {
        // PDF import would require a PDF parser - placeholder for now
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "PDF import endpoint available. Upload PDF file with questions."
        });
    }'

$newMethod = '    /// <summary>
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
    }'

$content = $content -replace [regex]::Escape($oldMethod), $newMethod

# Add helper method before Word import endpoint
$beforeWordImport = '    /// <summary>
    /// Import questions from Word document
    /// </summary>'

$helperMethod = '    private Stream ConvertImportRowsToStream(List<ImportQuestionRow> rows)
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

    /// <summary>
    /// Import questions from Word document
    /// </summary>'

$content = $content -replace [regex]::Escape($beforeWordImport), $helperMethod

# Write back
$content | Set-Content -Path $filePath -Encoding UTF8

Write-Host "QuestionsController.cs updated successfully with PDF import methods"
