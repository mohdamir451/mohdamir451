using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PDFComparisonUI.Controllers;

[Authorize(Policy = "ReviewerOrAdmin")]
public class ComparisonController(IMemoryCache cache) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("comparison/file/{token}")]
    public IActionResult File(string token)
    {
        if (!cache.TryGetValue($"pdf:{token}", out StoredPdfFile? stored) || stored is null)
        {
            return NotFound();
        }

        return File(stored.Content, "application/pdf", stored.FileName, enableRangeProcessing: true);
    }
}

public class StoredPdfFile
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
