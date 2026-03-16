using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using PDFComparisonUI.Dtos;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Controllers;

[ApiController]
[Authorize(Policy = "ReviewerOrAdmin")]
[Route("api/comparison")]
public class ComparisonApiController(
    IMemoryCache cache,
    IComparisonEngine comparisonEngine,
    ILogger<ComparisonApiController> logger) : ControllerBase
{
    [EnableRateLimiting("auth-login")]
    [HttpPost("upload-pdf")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadPdf([FromForm] IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "Empty file is not allowed." });
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only PDF files are supported." });
        }

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var token = Guid.NewGuid().ToString("N");
        var bytes = ms.ToArray();

        cache.Set($"pdf:{token}", new StoredPdfFile
        {
            FileName = file.FileName,
            Content = bytes
        }, TimeSpan.FromMinutes(45));

        var extracted = BuildMockPdfExtraction(file.FileName, bytes.Length);

        logger.LogInformation("PDF uploaded for comparison token {Token}, file {FileName}", token, file.FileName);

        return Ok(new UploadPdfResponse
        {
            FileToken = token,
            FileName = file.FileName,
            FileUrl = $"/comparison/file/{token}",
            PdfValues = extracted
        });
    }

    [HttpPost("compare")]
    public IActionResult Compare([FromBody] ComparisonRequest request)
    {
        if (request.ApiValues.Count == 0 || request.PdfValues.Count == 0)
        {
            return BadRequest(new { message = "Both API and PDF values are required for comparison." });
        }

        var result = comparisonEngine.Compare(request);
        return Ok(result);
    }

    [HttpPost("submit")]
    public IActionResult SubmitValidated([FromBody] ValidationSubmissionRequest request)
    {
        if (request.Fields.Count == 0)
        {
            return BadRequest(new { message = "No validated fields were submitted." });
        }

        logger.LogInformation("Validation submitted by {User}. Corrections: {Count}", request.ValidatedBy, request.AuditTrail.Count);

        return Ok(new
        {
            request.FileToken,
            submittedAtUtc = DateTime.UtcNow,
            request.ValidatedBy,
            mismatchCount = request.Fields.Count(x => !x.IsMatch),
            auditCount = request.AuditTrail.Count
        });
    }

    [HttpPost("export-excel")]
    public IActionResult ExportExcel([FromBody] ValidationSubmissionRequest request)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Field,API Value,PDF Value,Corrected Value,Is Match,Confidence,Reason Code,Reason");

        foreach (var field in request.Fields)
        {
            csv.AppendLine(string.Join(',',
                Escape(field.Label),
                Escape(field.ApiValue),
                Escape(field.PdfValue),
                Escape(field.CorrectedValue),
                field.IsMatch,
                field.ConfidenceScore,
                Escape(field.ReasonCode),
                Escape(field.ReasonDescription)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "validated-comparison-export.csv");
    }

    private static List<ComparisonField> BuildMockPdfExtraction(string fileName, int fileSize)
    {
        return new List<ComparisonField>
        {
            new() { Key = "invoice_number", Label = "Invoice Number", Value = Path.GetFileNameWithoutExtension(fileName), DataType = "string" },
            new() { Key = "invoice_date", Label = "Invoice Date", Value = DateTime.UtcNow.ToString("yyyy-MM-dd"), DataType = "date" },
            new() { Key = "amount_due", Label = "Amount Due", Value = (fileSize / 100m).ToString("0.00"), DataType = "number" },
            new() { Key = "currency", Label = "Currency", Value = "USD", DataType = "string" }
        };
    }

    private static string Escape(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "\"\"";
        return $"\"{input.Replace("\"", "\"\"")}\"";
    }
}
