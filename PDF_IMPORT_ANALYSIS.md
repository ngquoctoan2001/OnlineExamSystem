# 📊 Phân Tích Triển Khai Import Đề/Câu Hỏi Từ PDF

**Ngày phân tích:** 13/03/2026  
**Trạng thái hiện tại:** Hỗ trợ Excel import ✅ | PDF import chưa triển khai ❌

---

## 1️⃣ KIẾN TRÚC HIỆN TẠI

### 1.1 Luồng Import Excel (Đã Hoàn Thành)

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND (React/TypeScript)              │
│                                                             │
│  QuestionsPage.tsx                                          │
│  ├─ State: importFormat, importResult, importing            │
│  ├─ UI: Import nút dropdown chọn format                     │
│  └─ Handler: handleImport() - gọi API                       │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      │ POST /import/questions
                      │ Content-Type: multipart/form-data
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   BACKEND (ASP.NET Core)                    │
│                                                             │
│  ImportController.cs                                        │
│  ├─ POST /import/questions (ImportQuestions method)        │
│  ├─ Validation: Check file, extension (.xlsx/.xls only)   │
│  └─ Call: _importService.ImportQuestionsAsync()            │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              ImportService.cs (Infrastructure)              │
│                                                             │
│  ImportQuestionsAsync(Stream excelStream, long userId)     │
│  ├─ 1. Parse Excel → List<ImportQuestionRow>              │
│  ├─ 2. Validate từng row (Content, QuestionType, etc)     │
│  ├─ 3. Resolve references (QuestionType, Subject)         │
│  ├─ 4. Create Question entity                             │
│  └─ 5. Create QuestionOption entities                      │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│      ExcelParserService.cs (Infrastructure)                │
│                                                             │
│  ParseExcelAsync<T>(Stream excelStream)                    │
│  ├─ 1. Đọc Excel file bằng EPPlus                          │
│  ├─ 2. Map headers → class properties (reflection)         │
│  └─ 3. Trả về List<T> (T = ImportQuestionRow)            │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              Database (SQL Server)                          │
│                                                             │
│  Questions table                                            │
│  QuestionOptions table                                      │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Các Component Chính

#### **ExcelParserService** (EPPlus)
```csharp
// File: src/OnlineExamSystem.Infrastructure/Services/ExcelParserService.cs
public class ExcelParserService : IExcelParserService
{
    - ParseExcelAsync<T>(): List<T>
      • Mở Excel file bằng EPPlus
      • Đọc header row (row 1)
      • Map columns → properties (reflection)
      • Đọc data rows (từ row 2)
      • Trả về List<T>
      
    - Xử lý type conversion: string → target property type
}
```

#### **ImportService** (Business Logic)
```csharp
// File: src/OnlineExamSystem.Infrastructure/Services/ImportService.cs
public class ImportService : IImportService
{
    - ImportQuestionsAsync(Stream, userId): (bool, ImportResult)
      • Parse Excel → List<ImportQuestionRow>
      • Validate từng row:
        - Content: required
        - QuestionType: must exist in DB
        - Subject: optional, default to first subject
        - Difficulty: EASY/MEDIUM/HARD (default MEDIUM)
      • Create Question:
        - Content, QuestionTypeId, SubjectId
        - CreatedBy (userId), Difficulty
        - IsPublished = false
      • Create QuestionOptions for MCQ/TRUE_FALSE:
        - Label (A, B, C, D)
        - Content (option text)
        - IsCorrect (đáp án đúng)
        - OrderIndex
      • Trả về ImportResult với success/failed count
}
```

#### **ImportController** (API Endpoint)
```csharp
// File: src/OnlineExamSystem.API/Controllers/ImportController.cs
[ApiController]
[Route("api/import")]
[Authorize]
public class ImportController
{
    [HttpPost("questions")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ImportResult>>> ImportQuestions(IFormFile file)
    {
        // Validation
        - Check: file != null && file.Length > 0
        - Check: IsExcelFile(file.FileName) → .xlsx/.xls only
        
        // Process
        - Extract userId from JWT token
        - Call ImportService.ImportQuestionsAsync()
        
        // Response
        - Return ImportResult with counts & errors
    }
}
```

---

## 2️⃣ DỮ LIỆU IMPORT (ImportQuestionRow DTO)

### 2.1 Cấu Trúc Dữ Liệu

```csharp
// File: src/OnlineExamSystem.Application/DTOs/ImportDtos.cs
public class ImportQuestionRow
{
    [Required]
    public string? Content { get; set; }              // Nội dung câu hỏi
    
    public string QuestionType { get; set; }          // MCQ, TRUE_FALSE, SHORT_ANSWER, ESSAY, DRAWING
                                                       // Default: "MCQ"
    
    public string? Subject { get; set; }              // Tên môn học
                                                       // Optional, default to first subject
    
    public string Difficulty { get; set; }            // EASY, MEDIUM, HARD
                                                       // Default: "MEDIUM"
    
    public string? OptionA { get; set; }              // Lựa chọn A (MCQ/TRUE_FALSE)
    public string? OptionB { get; set; }              // Lựa chọn B
    public string? OptionC { get; set; }              // Lựa chọn C
    public string? OptionD { get; set; }              // Lựa chọn D
    
    public string? CorrectOption { get; set; }        // Đáp án đúng: A, B, C, D
}
```

### 2.2 Ví Dụ Excel File

| Content | QuestionType | Subject | Difficulty | OptionA | OptionB | OptionC | OptionD | CorrectOption |
|---------|--------------|---------|-----------|---------|---------|---------|---------|---------------|
| 2 + 2 = ? | MCQ | Math | EASY | 3 | 4 | 5 | 6 | B |
| Việt Nam là nước gì? | TRUE_FALSE | Geography | EASY | Đúng | Sai | | | A |
| Mô tả quá trình quang hợp | ESSAY | Biology | HARD | | | | | |

---

## 3️⃣ PHÂN TÍCH PDF IMPORT (PLACEHOLDER)

### 3.1 Các Endpoint PDF Hiện Tại

```csharp
// File: src/OnlineExamSystem.API/Controllers/QuestionsController.cs

[HttpPost("import/pdf")]
[Authorize(Roles = "ADMIN,TEACHER")]
public async Task<ActionResult<ResponseResult<object>>> ImportFromPdf()
{
    // ❌ PLACEHOLDER ONLY - Không có logic thực tế
    return Ok(new ResponseResult<object>
    {
        Success = true,
        Message = "PDF import endpoint available. Upload PDF file with questions."
    });
}

[HttpPost("import/docx")]
[Authorize(Roles = "ADMIN,TEACHER")]
public async Task<ActionResult<ResponseResult<object>>> ImportFromDocx()
{
    // ❌ PLACEHOLDER ONLY
}

[HttpPost("import/latex")]
[Authorize(Roles = "ADMIN,TEACHER")]
public async Task<ActionResult<ResponseResult<object>>> ImportFromLatex()
{
    // ❌ PLACEHOLDER ONLY
}
```

### 3.2 Frontend Hỗ Trợ Các Format

```typescript
// File: frontend/src/pages/QuestionsPage.tsx

// Supported formats defined in dropdown menu
const formats = [
    ['auto', 'Tự động nhận dạng', '.xlsx,.xls,.pdf,.doc,.docx,.tex,.csv'],
    ['excel', 'Excel (.xlsx, .xls)', '.xlsx,.xls'],
    ['pdf', 'PDF (.pdf)', '.pdf'],
    ['docx', 'Word (.docx)', '.doc,.docx'],
    ['latex', 'LaTeX (.tex)', '.tex,.latex']
]

// API calls (định nghĩa các hàm import)
const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    let res
    if (importFormat === 'pdf') res = await questionsApi.importPdf(file)
    else if (importFormat === 'docx') res = await questionsApi.importDocx(file)
    else if (importFormat === 'latex') res = await questionsApi.importLatex(file)
    else if (importFormat === 'excel') res = await questionsApi.importExcel(file)  // ✅ Hoạt động
    else res = await questionsApi.importFile(file)  // Default
}
```

### 3.3 API Calls Được Định Nghĩa

```typescript
// File: frontend/src/api/questions.ts

export const questionsApi = {
    // ✅ Excel import (hoạt động)
    importExcel: (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        return apiClient.post<ApiResponse<object>>('/import/questions', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        })
    },
    
    // ❌ PDF import (placeholder)
    importPdf: (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        return apiClient.post<ApiResponse<object>>('/questions/import/pdf', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        })
    },
    
    // ❌ DOCX import (placeholder)
    importDocx: (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        return apiClient.post<ApiResponse<object>>('/questions/import/docx', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        })
    },
    
    // ❌ LaTeX import (placeholder)
    importLatex: (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        return apiClient.post<ApiResponse<object>>('/questions/import/latex', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        })
    }
}
```

---

## 4️⃣ PLAN TRIỂN KHAI PDF IMPORT (PHASE 8)

### 4.1 Kế Hoạch Song Hành Trong PHASE_PLAN.md

```markdown
## 📁 PHASE 8: FILE STORAGE & OCR (Optional Enhancement)

#### 8.3 OCR Import (Document Parsing) (5-7 ngày)
- [ ] Use Tesseract or similar library
- [ ] Parse PDF → extract questions
- [ ] Parse image (JPG/PNG) → extract text
- [ ] Auto-detect MCQ format:
  ```
  1. Question?
  A) Answer 1
  B) Answer 2
  C) Answer 3
  D) Answer 4
  ```
- [ ] Create questions with detected options
- [ ] Manual review & correction
```

### 4.2 Kiến Trúc Đề Xuất Cho PDF Import

```
PDF File (input)
    ↓
[PDF Parser Service] 
    ├─ iTextSharp/PdfSharp: Extract text
    ├─ Regex: Parse question format
    └─ Output: List<ParsedQuestion>
    ↓
[Question Parser Service]
    ├─ Detect question type (MCQ, TRUE_FALSE, etc)
    ├─ Extract options (A, B, C, D)
    ├─ Identify correct answer
    └─ Output: List<ImportQuestionRow>
    ↓
[Import Service] (Reuse existing)
    ├─ Validate data
    ├─ Create Question entities
    ├─ Create QuestionOption entities
    └─ Output: ImportResult
    ↓
Database (Questions + QuestionOptions)
```

### 4.3 Các Library Cần Thiết

| Library | Purpose | Giấy Phép | Status |
|---------|---------|----------|--------|
| **iTextSharp** | PDF text extraction | Free (4.x) / Paid (7.x+) | ✅ Lightweight |
| **PdfSharp** | PDF manipulation | Free (LGPL) | ✅ Open source |
| **Tesseract.Net** | OCR for scanned PDFs | Free (Apache 2.0) | ⚠️ Heavy |
| **Regexes** | Pattern matching for questions | Built-in | ✅ Lightweight |

---

## 5️⃣ CHIA NHỎ QUY TRÌNH PARSE

### 5.1 Định Dạng PDF Tiêu Chuẩn

```
PDF Content Format:

1. What is the capital of France?
A) London
B) Paris
C) Berlin
D) Madrid
Answer: B

2. 2 + 2 = ?
A) 3
B) 4
C) 5
D) 6
Answer: B

True or False: The Earth is flat
Answer: False
```

### 5.2 Parsing Steps

#### **Step 1: Extract Text từ PDF**
```csharp
public string ExtractTextFromPdf(Stream pdfStream)
{
    using var reader = new PdfReader(pdfStream);
    var text = new StringBuilder();
    
    for (int i = 1; i <= reader.NumberOfPages; i++)
    {
        var pageText = PdfTextExtractor.GetTextFromPage(reader, i);
        text.Append(pageText);
    }
    
    return text.ToString();
}
```

#### **Step 2: Parse Questions từ Text**
```csharp
public List<ParsedQuestion> ParseQuestionsFromText(string text)
{
    // Regex patterns để detect questions
    var questionPattern = @"^(\d+)\.\s+(.+?)$";  // "1. Question text?"
    var optionPattern = @"^([A-D])\)\s+(.+?)$";  // "A) Option text"
    var answerPattern = @"^Answer:\s+([A-D]|True|False)$";  // "Answer: B"
    
    var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    var questions = new List<ParsedQuestion>();
    var currentQuestion = new ParsedQuestion();
    
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, questionPattern))
        {
            // Nếu có question trước, thêm vào list
            if (currentQuestion.Content != null)
                questions.Add(currentQuestion);
                
            currentQuestion = new ParsedQuestion 
            { 
                Content = ExtractQuestionContent(line),
                Options = new List<ParsedOption>()
            };
        }
        else if (Regex.IsMatch(line, optionPattern))
        {
            currentQuestion.Options.Add(ParseOption(line));
        }
        else if (Regex.IsMatch(line, answerPattern))
        {
            currentQuestion.CorrectOption = ExtractAnswer(line);
        }
    }
    
    if (currentQuestion.Content != null)
        questions.Add(currentQuestion);
        
    return questions;
}
```

#### **Step 3: Validate & Map sang ImportQuestionRow**
```csharp
public List<ImportQuestionRow> ValidateAndMapQuestions(List<ParsedQuestion> parsed)
{
    var result = new List<ImportQuestionRow>();
    var errors = new List<string>();
    
    foreach (var pq in parsed)
    {
        var errors_for_q = new List<string>();
        
        // Validate content
        if (string.IsNullOrWhiteSpace(pq.Content))
            errors_for_q.Add("Nội dung câu hỏi không thể trống");
            
        // Detect question type
        var qType = DetectQuestionType(pq);
        
        // Validate options (MCQ cần ít nhất 2 options)
        if (qType == "MCQ" && pq.Options.Count < 2)
            errors_for_q.Add($"Câu hỏi MCQ cần ít nhất 2 lựa chọn, nhưng chỉ có {pq.Options.Count}");
            
        // Validate correct answer
        if (qType == "MCQ" && string.IsNullOrWhiteSpace(pq.CorrectOption))
            errors_for_q.Add("Cần chỉ định đáp án đúng");
            
        if (errors_for_q.Count > 0)
        {
            errors.Add($"Câu hỏi '{pq.Content.Substring(0, 50)}...': {string.Join("; ", errors_for_q)}");
            continue;
        }
        
        // Map sang ImportQuestionRow
        var row = new ImportQuestionRow
        {
            Content = pq.Content,
            QuestionType = qType,
            Subject = DetermineSubject(pq),  // Auto-detect or user input
            Difficulty = EstimateDifficulty(pq),  // EASY/MEDIUM/HARD
            OptionA = pq.Options.FirstOrDefault(o => o.Label == "A")?.Content,
            OptionB = pq.Options.FirstOrDefault(o => o.Label == "B")?.Content,
            OptionC = pq.Options.FirstOrDefault(o => o.Label == "C")?.Content,
            OptionD = pq.Options.FirstOrDefault(o => o.Label == "D")?.Content,
            CorrectOption = pq.CorrectOption
        };
        
        result.Add(row);
    }
    
    return result;
}
```

---

## 6️⃣ COMPARISION: Excel vs PDF Import

| Khía cạnh | Excel Import | PDF Import |
|-----------|--------------|-----------|
| **Trạng thái** | ✅ Hoàn thành | ❌ Placeholder |
| **Parser** | EPPlus (structured) | PDF library + Regex (unstructured) |
| **Định dạng dữ liệu** | Bảng tính có cấu trúc | Text không có cấu trúc |
| **Độ chính xác** | 99% (do user định dạng đúng) | 70-80% (cần OCR cho scanned PDFs) |
| **Xử lý lỗi** | Dễ (dòng/cột rõ ràng) | Khó (text unstructured) |
| **OCR Support** | ❌ Không cần | ⚠️ Cần cho PDF scan |
| **Complexity** | Thấp (240 dòng code) | Cao (600-800 dòng code) |
| **Performance** | Nhanh (< 1s cho 1000 rows) | Chậm (2-5s do parsing) |

---

## 7️⃣ FLOW DIAGRAM: PDF Import Chi Tiết

```
Frontend: QuestionsPage.tsx
    │
    ├─ User chọn file PDF
    ├─ Call: questionsApi.importPdf(file)
    └─ POST /questions/import/pdf
            │
            ▼
Backend: QuestionsController.ImportFromPdf()
    │
    ├─ Validate file (reject nếu > 50MB)
    ├─ Extract userId from JWT
    └─ Call: _pdfImportService.ImportQuestionsFromPdfAsync(stream, userId)
            │
            ▼
PdfImportService.cs (NEW - cần tạo)
    │
    ├─ ExtractTextusingPdfReader()
    │  └─ Output: raw string text
    │
    ├─ ParseQuestionsFromText(string text)
    │  ├─ Split by question patterns
    │  ├─ Extract options (A, B, C, D)
    │  ├─ Identify correct answer
    │  └─ Output: List<ParsedQuestion>
    │
    ├─ ValidateAndMapToDto(List<ParsedQuestion>)
    │  ├─ Check required fields
    │  ├─ Auto-detect question type
    │  ├─ Estimate difficulty
    │  └─ Output: List<ImportQuestionRow>
    │
    └─ Call: _importService.ImportQuestionsAsync(List<ImportQuestionRow>, userId)
            │
            ▼
ImportService.ImportQuestionsAsync() (REUSE)
    │
    ├─ Validate & create Question entities
    ├─ Create QuestionOption entities
    └─ Return ImportResult
            │
            ▼
Database: Insert Questions + QuestionOptions
            │
            ▼
Response: ImportResult { SuccessCount, FailedCount, Errors }
```

---

## 8️⃣ THÁCH THỨC & GIẢI PHÁP

| Thách Thức | Nguyên Nhân | Giải Pháp |
|-----------|-----------|----------|
| **PDF không có cấu trúc** | PDF là format display, không data format | Dùng Regex, OCR cho scanned PDFs |
| **Scanned PDFs (images)** | Ảnh chụp từ cuốn sách/tờ giấy | Dùng Tesseract OCR |
| **Định dạng không nhất quán** | Người dùng có nhiều cách format khác nhau | Cung cấp template PDF + manual verification |
| **Performance (OCR slow)** | Tesseract xử lý chậm | Async processing + queue system |
| **False detection rate cao** | Regex không catches all cases | Machine learning / manual review step |

---

## 9️⃣ EXAMPLE DATA FLOW

### Ví Dụ 1: Excel Import (Hiện Tại - Hoạt Động)

**Input Excel:**
| Content | QuestionType | Subject | Difficulty | OptionA | OptionB | OptionC | OptionD | CorrectOption |
|---------|--------------|---------|-----------|---------|---------|---------|---------|---------------|
| 2+2=? | MCQ | Math | EASY | 3 | 4 | 5 | 6 | B |

**Processing:**
1. ExcelParserService.ParseExcelAsync → ImportQuestionRow
2. ImportService validates → ✅ Passed
3. Create Question + 4 QuestionOptions
4. Return ImportResult { SuccessCount: 1, FailedCount: 0 }

**Output:**
- Question tạo thành công
- Options tạo: A=3, B=4 (correct), C=5, D=6

---

### Ví Dụ 2: PDF Import (Cần Triển Khai)

**Input PDF Content:**
```
1. What is 2+2?
A) 3
B) 4
C) 5  
D) 6
Answer: B
```

**Processing:**
1. PdfImportService.ExtractTextFromPdf() → text
2. PdfImportService.ParseQuestionsFromText() → List<ParsedQuestion>
   ```
   {
       ContentSets: "What is 2+2?",
       QuestionNumber: 1,
       Options: [
           {Label: "A", Content: "3"},
           {Label: "B", Content: "4"},
           {Label: "C", Content: "5"},
           {Label: "D", Content: "6"}
       ],
       CorrectOption: "B"
   }
   ```
3. ValidateAndMapToDto() → ImportQuestionRow
4. (REUSE) ImportService.ImportQuestionsAsync()
5. Return ImportResult { SuccessCount: 1, FailedCount: 0 }

**Output:**
- Tương tự Example 1

---

## 🔟 MỘT SỐ PATTERN REGEX ĐỂ DETECT QUESTIONS

```csharp
// Detect câu trắc nghiệm
var questionPattern = @"^(\d+)\.\s+(.+?)(?:\?)?$";
// Match: "1. What is the capital of France?"
//        "2. Which is largest country"

// Detect lựa chọn (A, B, C, D)
var optionPattern = @"^[\s]*([A-D])\)[\s]+(.+)$";
// Match: "A) London"
//        "  B) Paris  " (with extra spaces)

// Detect True/False
var trueFalsePattern = @"^(True|False)[\s]*$";

// Detect đáp án
var answerPattern = @"^Answer[\s]*:[\s]*([A-D]|True|False)";
// Match: "Answer: B"
//        "Answer : A"
//        "Answer: True"

// Detect điểm (khó)
var markPattern = @"^\[(\d+)\s+marks?\]?$";
// Match: "[1 mark]"
//        "[5 marks]"
//        "[2]"
```

---

## 1️⃣1️⃣ KHUYẾN NGHỊ TRIỂN KHAI

### Phase 1: MiniMum Viable (1-2 tuần)
1. Implement `PdfImportService` với PDF text extraction
2. Implement basic Regex parser cho MCQ format
3. Reuse existing `ImportService`
4. Support: simple PDFs (text-based, không scan)

### Phase 2: Enhancement (2-3 tuần)
1. Improve Regex patterns (handle variations)
2. Add OCR support (Tesseract) cho scanned PDFs
3. Auto-detect question type & difficulty
4. Manual review & correction UI

### Phase 3: Advanced (3-4 tuần)
1. AI/ML model để detect questions (ChatGPT, Claude API)
2. Async processing queue (xử lý PDF lớn)
3. Batch import history & rollback
4. Template-based import (user define patterns)

---

## 1️⃣2️⃣ DEPENDENCIES ĐỀ XUẤT

### Cần Cài Đặt

```xml
<!-- Package.json cho PDF extraction -->
<ItemGroup>
    <!-- Option 1: iTextSharp (Free) -->
    <PackageReference Include="itext7" Version="7.2.5" />
    
    <!-- Option 2: PdfSharp (LGPL) -->
    <PackageReference Include="PdfSharpCore" Version="1.3.67" />
</ItemGroup>

<!-- OCR Support (Optional but Recommended) -->
<ItemGroup>
    <PackageReference Include="Tesseract" Version="5.2.0" />
</ItemGroup>
```

---

## CONCLUSION

### Trạng Thái Hiện Tại
- ✅ **Excel Import**: Hoàn toàn hoạt động
- ❌ **PDF/DOCX/LaTeX Import**: Chỉ là placeholder

### Cần Tìm Hiểu Thêm
- Chọn PDF library (iTextSharp vs PdfSharp)
- Thiết kế Regex patterns cho question parsing
- Quyết định có dùng OCR cho scanned PDFs không
- UX cho manual review step (user xác nhận parsed questions)

### Tiếp Theo
Tạo `PdfImportService.cs` với:
1. PDF text extraction
2. Question parsing (Regex + patterns)
3. Validation & mapping
4. Integration với existingImportService
