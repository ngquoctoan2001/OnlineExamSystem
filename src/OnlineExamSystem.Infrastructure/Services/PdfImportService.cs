using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Service để parse PDF files và extract câu hỏi trắc nghiệm (MCQ)
/// </summary>
public interface IPdfImportService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    List<ImportQuestionRow> ParseMcqQuestionsFromText(string text);
}

/// <summary>
/// Implementation của PDF import service - chỉ hỗ trợ câu hỏi trắc nghiệm với 4 đáp án (A, B, C, D)
/// </summary>
public class PdfImportService : IPdfImportService
{
    private readonly ILogger<PdfImportService> _logger;

    public PdfImportService(ILogger<PdfImportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extract text từ PDF file
    /// </summary>
    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        if (pdfStream == null || pdfStream.Length == 0)
            throw new ArgumentException("PDF stream is null or empty");

        return await Task.Run(() =>
        {
            var text = new System.Text.StringBuilder();

            try
            {
                using var pdfReader = new PdfReader(pdfStream);
                using var pdfDocument = new PdfDocument(pdfReader);

                var pageCount = pdfDocument.GetNumberOfPages();
                _logger.LogInformation($"PDF có {pageCount} trang");

                for (int i = 1; i <= pageCount; i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new LocationTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.Append(pageText);
                    text.Append("\n");
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting text from PDF: {ex.Message}");
                throw;
            }
        });
    }

    /// <summary>
    /// Parse text từ PDF thành List<ImportQuestionRow> 
    /// Chỉ hỗ trợ câu hỏi trắc nghiệm (MCQ) với 4 đáp án (A, B, C, D)
    /// 
    /// Expected format:
    /// 1. Question text here?
    /// A) Option A
    /// B) Option B
    /// C) Option C
    /// D) Option D
    /// Answer: B
    /// 
    /// Hoặc:
    /// 1) What is 2+2?
    /// A. 3
    /// B. 4
    /// C. 5
    /// D. 6
    /// Answer: B
    /// </summary>
    public List<ImportQuestionRow> ParseMcqQuestionsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<ImportQuestionRow>();

        var questions = new List<ImportQuestionRow>();
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        ImportQuestionRow currentQuestion = null;
        var optionsDict = new Dictionary<string, string>();
        string currentCorrectOption = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Detect câu hỏi (pattern: "1." hoặc "1)" theo sau bởi text)
            var questionMatch = Regex.Match(trimmedLine, @"^(\d+)[.)\s]+\s+(.+)$");
            if (questionMatch.Success && !trimmedLine.StartsWith("["))
            {
                // Save previous question nếu có
                if (currentQuestion != null && optionsDict.Count == 4 && currentCorrectOption != null)
                {
                    currentQuestion.OptionA = optionsDict.ContainsKey("A") ? optionsDict["A"] : null;
                    currentQuestion.OptionB = optionsDict.ContainsKey("B") ? optionsDict["B"] : null;
                    currentQuestion.OptionC = optionsDict.ContainsKey("C") ? optionsDict["C"] : null;
                    currentQuestion.OptionD = optionsDict.ContainsKey("D") ? optionsDict["D"] : null;
                    currentQuestion.CorrectOption = currentCorrectOption ?? "";
                    questions.Add(currentQuestion);
                }

                // Create new question
                var questionText = questionMatch.Groups[2].Value.Trim();
                currentQuestion = new ImportQuestionRow
                {
                    Content = questionText,
                    QuestionType = "MCQ",
                    Difficulty = "MEDIUM"  // Default difficulty
                };

                optionsDict = new Dictionary<string, string>();
                currentCorrectOption = null;
                continue;
            }

            if (currentQuestion == null)
                continue;

            // Detect đáp án (pattern: "A)" hoặc "A." theo sau bởi text)
            var optionMatch = Regex.Match(trimmedLine, @"^([A-D])[.)\s]+\s*(.+)$");
            if (optionMatch.Success && !trimmedLine.Contains("Answer"))
            {
                var optionLabel = optionMatch.Groups[1].Value;
                var optionContent = optionMatch.Groups[2].Value.Trim();

                // Remove leading characters like "- " hoặc "* "
                optionContent = Regex.Replace(optionContent, @"^[\s\-\*]+", "").Trim();

                if (!string.IsNullOrWhiteSpace(optionContent))
                {
                    optionsDict[optionLabel] = optionContent;
                }
                continue;
            }

            // Detect đáp án đúng (pattern: "Answer: B" hoặc "Đáp án: B" hoặc "Correct: B")
            var answerMatch = Regex.Match(trimmedLine, @"(Answer|Đáp án|Correct)[\s]*:?\s*([A-D])\s*$", RegexOptions.IgnoreCase);
            if (answerMatch.Success)
            {
                var answerValue = answerMatch.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(answerValue))
                {
                    currentCorrectOption = answerValue.ToUpper();
                }
                continue;
            }

            // Detect difficulty (pattern: "[EASY]" hoặc "[1 mark]")
            var difficultyMatch = Regex.Match(trimmedLine, @"\[(EASY|MEDIUM|HARD|dễ|trung bình|khó)\]", RegexOptions.IgnoreCase);
            if (difficultyMatch.Success && currentQuestion != null)
            {
                var diffText = difficultyMatch.Groups[1].Value.ToUpper();
                currentQuestion.Difficulty = diffText switch
                {
                    "DỄ" => "EASY",
                    "TRUNG BÌNH" => "MEDIUM",
                    "KHÓ" => "HARD",
                    _ => diffText
                };
                continue;
            }
        }

        // Add last question
        if (currentQuestion != null && optionsDict.Count == 4 && currentCorrectOption != null)
        {
            currentQuestion.OptionA = optionsDict.ContainsKey("A") ? optionsDict["A"] : null;
            currentQuestion.OptionB = optionsDict.ContainsKey("B") ? optionsDict["B"] : null;
            currentQuestion.OptionC = optionsDict.ContainsKey("C") ? optionsDict["C"] : null;
            currentQuestion.OptionD = optionsDict.ContainsKey("D") ? optionsDict["D"] : null;
            currentQuestion.CorrectOption = currentCorrectOption ?? "";
            questions.Add(currentQuestion);
        }

        _logger.LogInformation($"Parsed {questions.Count} MCQ questions from PDF text");
        
        // Validate all questions have 4 options
        var validQuestions = questions.Where(q => 
            !string.IsNullOrWhiteSpace(q.OptionA) &&
            !string.IsNullOrWhiteSpace(q.OptionB) &&
            !string.IsNullOrWhiteSpace(q.OptionC) &&
            !string.IsNullOrWhiteSpace(q.OptionD) &&
            !string.IsNullOrWhiteSpace(q.CorrectOption)).ToList();

        _logger.LogInformation($"Valid MCQ questions (with all 4 options): {validQuestions.Count}");

        return validQuestions;
    }
}
