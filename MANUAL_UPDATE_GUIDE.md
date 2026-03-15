# 📝 HƯỚNG DẪN UPDATE QuestionsController.cs

**CẢNH BÁO:** File này rất lớn và phức tạp. Để tránh corruption, hãy update từng phần cẩn thận theo hướng dẫn này.

---

## ✅ NHỮNG GÌ ĐÃ HOÀN THÀNH

### 1. ✅ PdfImportService.cs - HOÀN THÀNH
- File: `src/OnlineExamSystem.Infrastructure/Services/PdfImportService.cs`
- Status: **HOÀN TOÀN HOÀN THÀNH**
- Chức năng:
  - `ExtractTextFromPdfAsync()` - Extract text từ PDF
  - `ParseMcqQuestionsFromText()` - Parse MCQ questions từ text

### 2. ✅ Program.cs - HOÀN THÀNH
- File: `src/OnlineExamSystem.API/Program.cs`
- Line: ~152
- Change: Thêm `builder.Services.AddScoped<IPdfImportService, PdfImportService>();`
- Status: **ĐÃ HOÀN THÀNH**

### 3. ✅ NuGet Package - HOÀN THÀNH
- File: `src/OnlineExamSystem.Infrastructure/OnlineExamSystem.Infrastructure.csproj`
- Change: Thêm `<PackageReference Include="itext7" Version="7.2.5" />`
- Status: **ĐÃ HOÀN THÀNH**

### 4. ❌ QuestionsController.cs - CẦN CẬP NHẬT

---

## 🔧 HƯỚNG DẪN UPDATE QuestionsController.cs (CẬP NHẬT THỦ CÔNG)

### STEP 1: Thêm Dependencies vào Constructor

**File:** `src/OnlineExamSystem.API/Controllers/QuestionsController.cs`

**Tìm dòng ~25:**
```csharp
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IQuestionOptionRepository _questionOptionRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly ILogger<QuestionsController> _logger;
```

**Thay đổi thành:**
```csharp
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IQuestionOptionRepository _questionOptionRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly IPdfImportService _pdfImportService;
    private readonly IImportService _importService;
    private readonly ILogger<QuestionsController> _logger;
```

### STEP 2: Update Constructor Parameters

**Tìm dòng ~35:**
```csharp
public QuestionsController(
    IQuestionService questionService,
    IQuestionOptionRepository questionOptionRepository,
    ITeacherRepository teacherRepository,
    ITeachingAssignmentRepository teachingAssignmentRepository,
    ILogger<QuestionsController> logger)
{
    _questionService = questionService;
    _questionOptionRepository = questionOptionRepository;
    _teacherRepository = teacherRepository;
    _teachingAssignmentRepository = teachingAssignmentRepository;
    _logger = logger;
}
```

**Thay đổi thành:**
```csharp
public QuestionsController(
    IQuestionService questionService,
    IQuestionOptionRepository questionOptionRepository,
    ITeacherRepository teacherRepository,
    ITeachingAssignmentRepository teachingAssignmentRepository,
    IPdfImportService pdfImportService,
    IImportService importService,
    ILogger<QuestionsController> logger)
{
    _questionService = questionService;
    _questionOptionRepository = questionOptionRepository;
    _teacherRepository = teacherRepository;
    _teachingAssignmentRepository = teachingAssignmentRepository;
    _pdfImportService = pdfImportService;
    _importService = importService;
    _logger = logger;
}
```

### STEP 3: Thay đổi Endpoint ImportFromPdf

**Tìm dòng ~352 (search "ImportFromPdf"):**

```csharp
/// <summary>
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
}
```

**Xóa hết đoạn trên và thay thế bằng:**

```csharp
/// <summary>
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

private Stream ConvertImportRowsToStream(List<ImportQuestionRow> rows)
{
    var stream = new MemoryStream();
    using var package = new OfficeOpenXml.ExcelPackage(stream);
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
```

---

## ✅ SAU KHI UPDATE

1. **Build project:**
   ```bash
   cd src/OnlineExamSystem.API
   dotnet build
   ```

2. **Kiểm tra có error không**

3. **Test API:**
   ```bash
   dotnet run
   ```

4. **POST request test:**
   ```
   POST /api/questions/import/pdf
   Content-Type: multipart/form-data
   Authorization: Bearer YOUR_JWT_TOKEN
   Body: file=sample.pdf
   ```

---

## 📝 CHECKLIST HOÀN THÀNH

- [ ] STEP 1: Thêm fields IPdfImportService & IImportService
- [ ] STEP 2: Update constructor parameters
- [ ] STEP 3: Thay đổi endpoint ImportFromPdf
- [ ] STEP 4: Thêm helper method ConvertImportRowsToStream
- [ ] STEP 5: dotnet build (kiểm tra error)
- [ ] STEP 6: Test API

---

## 🎯 TÓMO KẾT 

### ✅ Đã Hoàn Thành:
1. **PdfImportService.cs** - Fully implemented
2. **Program.cs** - DI registered
3. **NuGet packages** - itext7 installed
4. **PDF_IMPORT_GUIDE.md** - Hướng dẫn format PDF
5. **PDF_IMPORT_SUMMARY.md** - Tóm tắt chi tiết

### ⏳ Cần Update Thủ Công:
1. **QuestionsController.cs** - Follow hướng dẫn trên

### 🚀 Khi Hoàn Thành:
- Upload PDF file với câu hỏi trắc nghiệm
- Hệ thống sẽ:
  - Extract text từ PDF
  - Parse câu hỏi (detect A, B, C, D options)
  - Save vào database
  - Return import result (success/failed counts)

---

## 💡 ỚNS

- **File size limit:** 50MB
- **Supported:** PDF text-based files
- **Not supported:** Scanned PDFs (need OCR)
- **Question type:** MCQ only (4 options)
