using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Reflection;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Service for parsing Excel files
/// </summary>
public interface IExcelParserService
{
    Task<List<T>> ParseExcelAsync<T>(Stream excelStream) where T : class, new();
    Task<(List<T> Data, List<string> Errors)> ParseExcelWithValidationAsync<T>(Stream excelStream) where T : class, new();
}

/// <summary>
/// Excel parser service implementation using EPPlus
/// </summary>
public class ExcelParserService : IExcelParserService
{
    private readonly ILogger<ExcelParserService> _logger;

    public ExcelParserService(ILogger<ExcelParserService> logger)
    {
        _logger = logger;
        // Set EPPlus license context (required for EPPlus 5.0+)
        try
        {
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }
        catch
        {
            // License context may already be set
        }
    }

    /// <summary>
    /// Parse Excel file to list of objects
    /// </summary>
    public async Task<List<T>> ParseExcelAsync<T>(Stream excelStream) where T : class, new()
    {
        var result = new List<T>();

        if (excelStream == null || excelStream.Length == 0)
        {
            _logger.LogWarning("Excel stream is null or empty");
            return result;
        }

        try
        {
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
            {
                _logger.LogWarning("Excel worksheet is empty");
                return result;
            }

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            // Get properties of T to map columns
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

            // Read header row (first row)
            var headerRow = new Dictionary<int, PropertyInfo>();
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim() ?? string.Empty;
                var matchingProperty = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, headerValue, StringComparison.OrdinalIgnoreCase));

                if (matchingProperty != null)
                {
                    headerRow[col] = matchingProperty;
                }
            }

            // Read data rows (starting from row 2)
            for (int row = 2; row <= rowCount; row++)
            {
                var item = new T();
                bool hasData = false;

                foreach (var kvp in headerRow)
                {
                    var cellValue = worksheet.Cells[row, kvp.Key].Value;
                    if (cellValue != null)
                    {
                        hasData = true;
                        try
                        {
                            var property = kvp.Value;
                            var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                            property.SetValue(item, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to convert cell value at row {row}, column {kvp.Key}: {ex.Message}");
                        }
                    }
                }

                if (hasData)
                {
                    result.Add(item);
                }
            }

            _logger.LogInformation($"Successfully parsed {result.Count} rows from Excel file");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing Excel file: {ex.Message}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Parse Excel file with validation
    /// </summary>
    public async Task<(List<T> Data, List<string> Errors)> ParseExcelWithValidationAsync<T>(Stream excelStream) where T : class, new()
    {
        var data = await ParseExcelAsync<T>(excelStream);
        var errors = new List<string>();

        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(new T());
        var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();

        foreach (var item in data)
        {
            validationContext.DisplayName = null;
            validationContext.MemberName = null;
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(item, validationContext, results, true);

            foreach (var error in results)
            {
                errors.Add($"Validation error: {error.ErrorMessage}");
            }
            results.Clear();
        }

        return (data, errors);
    }
}
