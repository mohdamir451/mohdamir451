using System.ComponentModel.DataAnnotations;

namespace PDFComparisonUI.Dtos;

public class ComparisonField
{
    [Required]
    public string Key { get; set; } = string.Empty;
    [Required]
    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DataType { get; set; }
}

public class ComparisonRequest
{
    public List<ComparisonField> ApiValues { get; set; } = new();
    public List<ComparisonField> PdfValues { get; set; } = new();
}

public class ComparisonFieldResult
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? ApiValue { get; set; }
    public string? PdfValue { get; set; }
    public string? CorrectedValue { get; set; }
    public bool IsMatch { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
}

public class ComparisonSummary
{
    public int TotalFields { get; set; }
    public int MatchCount { get; set; }
    public int MismatchCount { get; set; }
    public decimal AverageConfidence { get; set; }
}

public class ComparisonResultResponse
{
    public DateTime ComparedAtUtc { get; set; }
    public List<ComparisonFieldResult> Fields { get; set; } = new();
    public ComparisonSummary Summary { get; set; } = new();
}

public class UploadPdfResponse
{
    public string FileToken { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public List<ComparisonField> PdfValues { get; set; } = new();
}

public class CorrectionAuditEntry
{
    public string FieldKey { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
}

public class ValidationSubmissionRequest
{
    public string FileToken { get; set; } = string.Empty;
    public string ValidatedBy { get; set; } = string.Empty;
    public List<ComparisonFieldResult> Fields { get; set; } = new();
    public List<CorrectionAuditEntry> AuditTrail { get; set; } = new();
}
