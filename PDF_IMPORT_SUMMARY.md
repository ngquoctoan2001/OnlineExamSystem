# 🎯 TÓM TẮT PHÂN TÍCH: Import Đề/Câu Hỏi Từ PDF

---

## 📊 TÌNH HÌNH HIỆN TẠI

### ✅ Đã Triển Khai
- **Excel Import**: Hoạt động 100%
- **EndPoint**: `POST /api/import/questions`
- **Parser**: EPPlus library
- **Supported Format**: `.xlsx`, `.xls`

### ❌ Chưa Triển Khai
- **PDF Import**: Chỉ là placeholder (không có logic)
- **DOCX Import**: Placeholder
- **LaTeX Import**: Placeholder
- **OCR Support**: Không hỗ trợ

---

## 🚀 LUỒNG XỬ LÝ (HIỆN TẠI)

### Excel Import Flow
```
User chọn file.xlsx
    ↓
Frontend: QuestionsPage.tsx → Call API
    ↓
Backend: POST /api/import/questions
    ↓
ExcelParserService.ParseExcelAsync<ImportQuestionRow>()
    - Dùng EPPlus đọc file
    - Map columns → class properties
    - Trả về List<ImportQuestionRow>
    ↓
ImportService.ImportQuestionsAsync()
    - Validate dữ liệu từng row
    - Resolve QuestionType, Subject từ DB
    - Create Question + QuestionOptions entities
    - Save vào database
    ↓
Return ImportResult { SuccessCount, FailedCount, Errors }
```

---

## 📋 DỮ LIỆU CẦN NHẬP (Excel Format)

| Cột | Loại | Bắt Buộc | Mô Tả |
|-----|------|---------|-------|
| `Content` | String | ✅ Có | Nội dung câu hỏi |
| `QuestionType` | String | ❌ Không | MCQ, TRUE_FALSE, SHORT_ANSWER, ESSAY, DRAWING (default: MCQ) |
| `Subject` | String | ❌ Không | Tên môn học (nếu không → use first subject) |
| `Difficulty` | String | ❌ Không | EASY, MEDIUM, HARD (default: MEDIUM) |
| `OptionA` | String | ❌ Không | Lựa chọn A (cho MCQ/TRUE_FALSE) |
| `OptionB` | String | ❌ Không | Lựa chọn B |
| `OptionC` | String | ❌ Không | Lựa chọn C |
| `OptionD` | String | ❌ Không | Lựa chọn D |
| `CorrectOption` | String | ❌ Không | Đáp án đúng (A, B, C, D) |

### Ví Dụ Excel
```
Content | QuestionType | Subject | Difficulty | OptionA | OptionB | OptionC | OptionD | CorrectOption
--------|--------------|---------|-----------|---------|---------|---------|---------|---------------
2+2=?   | MCQ          | Math    | EASY      | 3       | 4       | 5       | 6       | B
```

---

## 📄 CẬU TRÚC FILE NGUỒN

### Backend
```
src/OnlineExamSystem.API/
├── Controllers/
│   ├── ImportController.cs (API endpoint - Excel only)
│   └── QuestionsController.cs (PDF/DOCX/LaTeX placeholder endpoints)
│
src/OnlineExamSystem.Infrastructure/
└── Services/
    ├── ExcelParserService.cs (✅ Hoạt động - EPPlus)
    ├── ImportService.cs (✅ Hoạt động - Business logic)
    └── (❌ MISSING) PdfImportService.cs (Cần tạo mới)

src/OnlineExamSystem.Application/DTOs/
└── ImportDtos.cs
    ├── ImportQuestionRow DTO
    ├── ImportResult DTO
    └── ImportError DTO
```

### Frontend
```
frontend/src/
├── pages/QuestionsPage.tsx
│   - State: importFormat (auto, excel, pdf, docx, latex)
│   - Handler: handleImport() gọi API
│   - UI: Dropdown chọn format + Import button
│
└── api/questions.ts
    ├── importFile() (default)
    ├── importExcel() (✅ Hoạt động)
    ├── importPdf() (❌ Placeholder)
    ├── importDocx() (❌ Placeholder)
    └── importLatex() (❌ Placeholder)
```

---

## ⚠️ VẤN ĐỀ CHÍNH

### 1. PDF Format Không Có Cấu Trúc
**Nguyên Nhân:** PDF là format display (dành để in), không phải data format
**Giải Pháp:** 
- Dùng Regex để detect patterns (số thứ tự, options A-D, đáp án)
- Yêu cầu user follow một template chuẩn
- For scanned PDFs → cần OCR (Tesseract)

### 2. PDF Tự Do Không Nhất Quán
**Nguyên Nhân:** User có thể format tùy ý
**Example:**
```
Option 1:
1. Question?
A) Answer A
B) Answer B
Answer: A

Option 2:
Question 1
(A) Answer A
(B) Answer B
Correct: A

Option 3:
Q1: What...?
- Choice 1
- Choice 2
Ans: Choice 1
```

**Giải Pháp:**
- Cung cấp template PDF chuẩn để user download + follow
- Implement flexible Regex patterns
- Fallback → manual review & edit page

### 3. Scanned PDF (Hình Ảnh)
**Nguyên Nhân:** Chụp từ cuốn sách/tờ giấy
**Giải Pháp:** Dùng Tesseract OCR để recognize text

---

## 🔧 GIẢI PHÁP TRIỂN KHAI

### Phase 1: MVP (1-2 tuần)

**Mục tiêu:** Support text-based PDFs với format chuẩn

**Cần tạo:**
1. `PdfImportService.cs` với 3 methods:
   - `ExtractTextFromPdfAsync()` - dùng iText7 extract text
   - `ParseQuestionsFromText()` - dùng Regex parse questions
   - `ValidateAndMapToImportRows()` - validate & map sang ImportQuestionRow

2. Update `QuestionsController.ImportFromPdf()` - implement thực tế (không placeholder)

3. Add NuGet: `itext7`

**Expected Format:**
```
1. Question text here?
A) Option A
B) Option B
C) Option C
D) Option D
Answer: B

2. Next question...
```

### Phase 2: Enhancement (2-3 tuần)

**Bổ sung:**
- Improve Regex patterns (handle variations)
- Auto-detect question type (TRUE_FALSE, ESSAY, etc)
- Manual review UI (user approve before save)
- Support DOCX & LaTeX

### Phase 3: Advanced (3-4 tuần)

**Optional:**
- OCR cho scanned PDFs (Tesseract)
- Async processing queue (large files)
- AI/ML detection (ChatGPT API)

---

## 📝 REGEX PATTERNS CẦN DÙNG

```csharp
// Detect số thứ tự + nội dung câu hỏi
var questionPattern = @"^(\d+)\.\s+(.+)$";
// Match: "1. What is 2+2?"

// Detect lựa chọn A-D
var optionPattern = @"^([A-D])\)\s+(.+)$";
// Match: "A) Option text"

// Detect đáp án
var answerPattern = @"^Answer[\s]*:[\s]*([A-D]|True|False)$";
// Match: "Answer: B" hoặc "Answer: True"

// Detect độ khó
var difficultyPattern = @"\[(EASY|MEDIUM|HARD)\]";
// Match: "[EASY]" trong câu hỏi
```

---

## 💻 CODE STRUCTURE (PSEUDOCODE)

```csharp
// 1. Extract text từ PDF
public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
{
    using var pdfReader = new PdfReader(pdfStream);
    using var pdfDocument = new PdfDocument(pdfReader);
    
    var text = StringBuilder();
    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
    {
        var pageText = PdfTextExtractor.GetTextFromPage(...);
        text.Append(pageText);
    }
    return text.ToString();
}

// 2. Parse questions từ text
public List<ParsedQuestion> ParseQuestionsFromText(string text)
{
    var lines = text.Split('\n');
    var questions = new List<ParsedQuestion>();
    var currentQuestion = new ParsedQuestion();
    
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, questionPattern))
        {
            // Nếu có question trước → add vào list
            if (currentQuestion != null) questions.Add(currentQuestion);
            
            // Create new question
            currentQuestion = new ParsedQuestion 
            { 
                Content = ExtractContent(line),
                Options = new List<ParsedOption>()
            };
        }
        else if (Regex.IsMatch(line, optionPattern))
        {
            // Add option vào current question
            currentQuestion.Options.Add(ParseOption(line));
        }
        else if (Regex.IsMatch(line, answerPattern))
        {
            // Set correct answer
            currentQuestion.CorrectOption = ExtractAnswer(line);
        }
    }
    
    if (currentQuestion != null) questions.Add(currentQuestion);
    return questions;
}

// 3. Validate & Map sang ImportQuestionRow
public List<ImportQuestionRow> ValidateAndMapToImportRows(List<ParsedQuestion> parsed)
{
    var result = new List<ImportQuestionRow>();
    
    foreach (var pq in parsed)
    {
        // Validate: content not empty, have options, etc.
        if (Validate(pq) == false) continue;  // Skip invalid
        
        // Detect question type
        var qType = DetectQuestionType(pq);  // MCQ, TRUE_FALSE, etc
        
        // Map sang ImportQuestionRow
        var row = new ImportQuestionRow
        {
            Content = pq.Content,
            QuestionType = qType,
            Subject = DetermineSubject(pq),
            OptionA = pq.GetOption("A")?.Content,
            OptionB = pq.GetOption("B")?.Content,
            OptionC = pq.GetOption("C")?.Content,
            OptionD = pq.GetOption("D")?.Content,
            CorrectOption = pq.CorrectOption
        };
        
        result.Add(row);
    }
    
    return result;
}

// 4. API Endpoint
[HttpPost("import/pdf")]
public async Task<ActionResult<ResponseResult<ImportResult>>> ImportFromPdf(IFormFile file)
{
    // Validate file
    if (!file.FileName.EndsWith(".pdf")) return BadRequest(...);
    
    var userId = GetUserId();
    
    // Extract text
    var pdfText = await _pdfImportService.ExtractTextFromPdfAsync(file.OpenReadStream());
    
    // Parse questions
    var parsedQuestions = _pdfImportService.ParseQuestionsFromText(pdfText);
    
    // Validate & map
    var importRows = _pdfImportService.ValidateAndMapToImportRows(parsedQuestions);
    
    // (REUSE) Import service
    var (success, result) = await _importService.ImportQuestionsAsync(..., userId);
    
    return Ok(new ResponseResult<ImportResult> { Success = success, Data = result });
}
```

---

## 🎁 CHIA NHỎ CÔNG VIỆC

### Week 1: Xây dựng Core PDF Service
```
Day 1-2: PdfImportService.cs
  - ExtractTextFromPdfAsync()
  - ParseQuestionsFromText()
  - ValidateAndMapToImportRows()

Day 3: Unit tests
  - Test text extraction
  - Test parsing logic
  - Test validation

Day 4-5: API Integration
  - Update QuestionsController
  - Add dependency injection
  - Test with sample PDFs
```

### Week 2: Enhancement & Testing
```
Day 1-2: Improve Regex patterns
Day 3: Manual review UI
Day 4: Documentation & examples
Day 5: QA / Bug fixes
```

---

## 📦 DEPENDENCIES

### NuGet Packages Cần Thêm
```xml
<!-- PDF text extraction -->
<PackageReference Include="itext7" Version="7.2.5" />

<!-- (Optional for OCR) -->
<PackageReference Include="Tesseract" Version="5.2.0" />

<!-- DOCX support (future) -->
<PackageReference Include="DocumentFormat.OpenXml" Version="2.20.0" />
```

---

## 🔍 COMPARISION: Excel vs PDF

| Aspect | Excel | PDF |
|--------|-------|-----|
| **Status** | ✅ Hoạt động | ❌ Placeholder |
| **Parser** | EPPlus (lib) | iText7 + Regex |
| **Structured?** | ✅ Có (bảng) | ❌ Không (text flow) |
| **Accuracy** | 99% | 70-80% |
| **Effort** | 240 lines | 600-800 lines |
| **Performance** | < 1s (1000 rows) | 2-5s (100 Qs) |
| **Error Handling** | Dễ (row/col) | Khó (unstructured) |

---

## 🎯 QUICK START CHECKLIST

- [ ] Add `itext7` NuGet package
- [ ] Create `PdfImportService.cs`
- [ ] Implement `ExtractTextFromPdfAsync()`
- [ ] Implement `ParseQuestionsFromText()`
- [ ] Implement `ValidateAndMapToImportRows()`
- [ ] Update `QuestionsController.ImportFromPdf()`
- [ ] Add dependency injection
- [ ] Write unit tests
- [ ] Test with sample PDF
- [ ] Create template PDF for users
- [ ] Documentation & examples

---

## 📚 RELATED FILES

**Để Phân Tích Chi Tiết:**
- [PDF_IMPORT_ANALYSIS.md](./PDF_IMPORT_ANALYSIS.md) - Phân tích toàn diện
- [PDF_IMPORT_IMPLEMENTATION.md](./PDF_IMPORT_IMPLEMENTATION.md) - Code examples

**Existing Code:**
- [ImportService.cs](src/OnlineExamSystem.Infrastructure/Services/ImportService.cs)
- [ExcelParserService.cs](src/OnlineExamSystem.Infrastructure/Services/ExcelParserService.cs)
- [QuestionsController.cs](src/OnlineExamSystem.API/Controllers/QuestionsController.cs)
- [QuestionsPage.tsx](frontend/src/pages/QuestionsPage.tsx)

---

## ✨ SUMMARY

### Hiện Tại
- ✅ Excel import hoạt động 100%
- ❌ PDF import chỉ placeholder (0%)

### Đề Xuất Triển Khai
1. Create `PdfImportService` để extract & parse PDF
2. Implement Regex patterns để detect questions
3. Reuse existing `ImportService` để save vào DB
4. Add Unit tests

### Timeline
- **MVP** (text PDFs): 1-2 tuần
- **Full support** (with OCR): 3-4 tuần

### Effort
- Backend: 600-800 lines code
- Frontend: 0 (reuse existing)
- Tests: 300+ lines

### Complexity
- **Low** để extract text
- **Medium** để parse unstructured text
- **Medium** để handle edge cases & validation
