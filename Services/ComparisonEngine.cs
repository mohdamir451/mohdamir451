using System.Globalization;
using PDFComparisonUI.Dtos;

namespace PDFComparisonUI.Services;

public class ComparisonEngine : IComparisonEngine
{
    public ComparisonResultResponse Compare(ComparisonRequest request)
    {
        var results = new List<ComparisonFieldResult>();

        foreach (var apiField in request.ApiValues)
        {
            var pdfField = request.PdfValues.FirstOrDefault(x => x.Key == apiField.Key);
            var comparison = CompareField(apiField, pdfField);
            results.Add(comparison);
        }

        foreach (var pdfOnly in request.PdfValues.Where(p => request.ApiValues.All(a => a.Key != p.Key)))
        {
            results.Add(new ComparisonFieldResult
            {
                Key = pdfOnly.Key,
                Label = pdfOnly.Label,
                ApiValue = null,
                PdfValue = pdfOnly.Value,
                CorrectedValue = pdfOnly.Value,
                IsMatch = false,
                ConfidenceScore = 0,
                ReasonCode = "MISSING_IN_API",
                ReasonDescription = "Field exists in PDF but is missing from API payload."
            });
        }

        var mismatchCount = results.Count(x => !x.IsMatch);
        var averageConfidence = results.Count == 0 ? 0 : results.Average(x => x.ConfidenceScore);

        return new ComparisonResultResponse
        {
            ComparedAtUtc = DateTime.UtcNow,
            Fields = results,
            Summary = new ComparisonSummary
            {
                TotalFields = results.Count,
                MatchCount = results.Count(x => x.IsMatch),
                MismatchCount = mismatchCount,
                AverageConfidence = Math.Round(averageConfidence, 2)
            }
        };
    }

    private static ComparisonFieldResult CompareField(ComparisonField apiField, ComparisonField? pdfField)
    {
        if (pdfField is null)
        {
            return new ComparisonFieldResult
            {
                Key = apiField.Key,
                Label = apiField.Label,
                ApiValue = apiField.Value,
                PdfValue = null,
                CorrectedValue = apiField.Value,
                IsMatch = false,
                ConfidenceScore = 0,
                ReasonCode = "MISSING_IN_PDF",
                ReasonDescription = "Field exists in API payload but is missing in PDF extraction."
            };
        }

        var apiNormalized = Normalize(apiField.Value, apiField.DataType);
        var pdfNormalized = Normalize(pdfField.Value, pdfField.DataType ?? apiField.DataType);

        if (string.Equals(apiField.Value, pdfField.Value, StringComparison.Ordinal))
        {
            return Build(apiField, pdfField, true, 1, "EXACT_MATCH", "Values are an exact match.");
        }

        if (string.Equals(apiNormalized, pdfNormalized, StringComparison.Ordinal))
        {
            return Build(apiField, pdfField, true, 0.88m, "NORMALIZED_MATCH", "Values match after normalization (formatting/casing/spacing). ");
        }

        var reason = GetMismatchReason(apiField, pdfField, apiNormalized, pdfNormalized);
        return Build(apiField, pdfField, false, 0.35m, reason.code, reason.description);
    }

    private static ComparisonFieldResult Build(ComparisonField apiField, ComparisonField pdfField, bool isMatch, decimal confidence, string code, string description)
    {
        return new ComparisonFieldResult
        {
            Key = apiField.Key,
            Label = apiField.Label,
            ApiValue = apiField.Value,
            PdfValue = pdfField.Value,
            CorrectedValue = isMatch ? apiField.Value : pdfField.Value,
            IsMatch = isMatch,
            ConfidenceScore = confidence,
            ReasonCode = code,
            ReasonDescription = description
        };
    }

    private static string Normalize(string? value, string? dataType)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var cleaned = string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (string.Equals(dataType, "number", StringComparison.OrdinalIgnoreCase) || decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric.ToString("0.################", CultureInfo.InvariantCulture);
        }

        if (string.Equals(dataType, "date", StringComparison.OrdinalIgnoreCase) || DateTime.TryParse(cleaned, out var date))
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return cleaned.ToLowerInvariant();
    }

    private static (string code, string description) GetMismatchReason(ComparisonField api, ComparisonField pdf, string apiNorm, string pdfNorm)
    {
        if (string.IsNullOrWhiteSpace(api.Value) || string.IsNullOrWhiteSpace(apiNorm))
        {
            return ("EMPTY_API_VALUE", "API value is empty while PDF contains a value.");
        }

        if (string.IsNullOrWhiteSpace(pdf.Value) || string.IsNullOrWhiteSpace(pdfNorm))
        {
            return ("EMPTY_PDF_VALUE", "PDF extracted value is empty while API contains a value.");
        }

        return ("VALUE_MISMATCH", "Normalized values differ between API and PDF.");
    }
}
