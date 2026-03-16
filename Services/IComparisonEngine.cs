using PDFComparisonUI.Dtos;

namespace PDFComparisonUI.Services;

public interface IComparisonEngine
{
    ComparisonResultResponse Compare(ComparisonRequest request);
}
