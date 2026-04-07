using System.Collections.Generic;
using System.Threading.Tasks;
using PrintToolAvalonia.Models;

namespace PrintToolAvalonia.Services;

public interface ILabelTemplateService
{
    Task<List<LabelTemplateConfig>> GetTemplatesAsync();

    Task<string> GetTemplateJsonAsync(LabelTemplateConfig template);

    string CreateNewTemplateJson();

    Task<LabelTemplateConfig> SaveTemplateAsync(string templateJson, string? originalFilePath = null);

    Task<string> GeneratePreviewPdfAsync(string templateJson, string? barcodePdfPath, int? barcodePageNumber, bool includeImporterInfo);

    Task<string> GenerateLabelPdfAsync(
        LabelTemplateConfig template,
        string barcodePdfPath,
        int barcodePageNumber,
        bool includeImporterInfo);
}
