using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using iText.Html2pdf;
using iText.StyledXmlParser.Resolver.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using PrintToolAvalonia.Models;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;
using PageSize = iText.Kernel.Geom.PageSize;

namespace PrintToolAvalonia.Services;

public class LabelTemplateService : ILabelTemplateService
{
    private readonly IConfigService _configService;
    private readonly IPrintService _printService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly JsonSerializerOptions _jsonWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private const float LabelPageMarginMm = 0.6f;
    private const float LabelWidthMm = 98f;
    private const float LabelHeightMm = 98f;
    private const float BarcodeFrameOffsetLeftMm = 0.2646f;
    private const float BarcodeFrameOffsetTopMm = 0.2646f;
    private const float BarcodeFrameWidthMm = 97.4708f;
    private const float BarcodeFrameHeightMm = 20.8708f;

    public LabelTemplateService(IConfigService configService, IPrintService printService)
    {
        _configService = configService;
        _printService = printService;
    }

    public async Task<List<LabelTemplateConfig>> GetTemplatesAsync()
    {
        return await Task.Run(() =>
        {
            EnsureTemplateDirectoryInitialized();
            var templateDirectory = GetTemplateDirectoryPath();

            var templates = new List<LabelTemplateConfig>();
            foreach (var filePath in Directory.GetFiles(templateDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var template = ParseTemplate(json);
                    if (template == null) continue;

                    template.SourceFilePath = filePath;
                    templates.Add(template);
                }
                catch { }
            }

            return templates
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public async Task<string> GetTemplateJsonAsync(LabelTemplateConfig template)
    {
        if (!string.IsNullOrWhiteSpace(template.SourceFilePath) && File.Exists(template.SourceFilePath))
        {
            return await File.ReadAllTextAsync(template.SourceFilePath);
        }

        return JsonSerializer.Serialize(template, _jsonWriteOptions);
    }

    public string CreateNewTemplateJson()
    {
        var template = new LabelTemplateConfig
        {
            Id = $"template_{DateTime.Now:yyyyMMddHHmmss}",
            Name = "新模板",
            Representatives = new List<LabelRepresentativeInfo> { new(), new(), new() },
            ImporterInfo = new LabelImporterInfo()
        };

        return JsonSerializer.Serialize(template, _jsonWriteOptions);
    }

    public async Task<LabelTemplateConfig> SaveTemplateAsync(string templateJson, string? originalFilePath = null)
    {
        var template = ParseTemplate(templateJson) ?? throw new InvalidOperationException("模板 JSON 无法解析");
        if (string.IsNullOrWhiteSpace(template.Id)) throw new InvalidOperationException("模板 ID 不能为空");
        if (string.IsNullOrWhiteSpace(template.Name)) throw new InvalidOperationException("模板名称不能为空");

        EnsureTemplateDirectoryInitialized();
        EnsureTemplateIdIsUnique(template.Id, originalFilePath);

        var targetFilePath = !string.IsNullOrWhiteSpace(originalFilePath)
            ? originalFilePath
            : System.IO.Path.Combine(GetTemplateDirectoryPath(), $"{SanitizeFileName(template.Id)}.json");

        await File.WriteAllTextAsync(targetFilePath, JsonSerializer.Serialize(template, _jsonWriteOptions));

        template.SourceFilePath = targetFilePath;
        return template;
    }

    public async Task<string> GeneratePreviewPdfAsync(string templateJson, string? barcodePdfPath, int? barcodePageNumber, bool includeImporterInfo)
    {
        var template = ParseTemplate(templateJson) ?? throw new InvalidOperationException("模板 JSON 无法解析");
        var previewSource = !string.IsNullOrWhiteSpace(barcodePdfPath) && File.Exists(barcodePdfPath)
            ? new List<(string? BarcodePdfPath, int? BarcodePageNumber)> { (barcodePdfPath, barcodePageNumber ?? 1) }
            : new List<(string? BarcodePdfPath, int? BarcodePageNumber)> { (null, null) };

        return await GeneratePdfFileAsync(template, previewSource, includeImporterInfo, true);
    }

    public async Task<string> GeneratePdfAsync(
        LabelTemplateConfig template,
        IReadOnlyList<TemplateLabelQueueItem> queueItems,
        bool includeImporterInfo)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (queueItems.Count == 0) throw new InvalidOperationException("请先添加要打印的条码分组");

        var labelSources = queueItems
            .SelectMany(item => Enumerable.Range(item.BarcodeGroup.StartPage, item.BarcodeGroup.BarcodeCount)
                .Select(pageNumber => (BarcodePdfPath: (string?)item.BarcodePdfPath, BarcodePageNumber: (int?)pageNumber)))
            .ToList();

        return await GeneratePdfFileAsync(template, labelSources, includeImporterInfo, false);
    }

    private async Task<string> GeneratePdfFileAsync(
        LabelTemplateConfig template,
        IReadOnlyList<(string? BarcodePdfPath, int? BarcodePageNumber)> labelSources,
        bool includeImporterInfo,
        bool allowBarcodePlaceholder)
    {
        return await Task.Run(() =>
        {
            var outputDirectory = System.IO.Path.Combine(_configService.GetAppDataPath(), "generated_labels");
            Directory.CreateDirectory(outputDirectory);

            var outputPath = System.IO.Path.Combine(
                outputDirectory,
                $"template-label-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}.pdf");

            var pageSize = new PageSize(MillimetersToPoints(100), MillimetersToPoints(100));
            using var writer = new PdfWriter(outputPath);
            using var pdfDocument = new ITextPdfDocument(writer);

            var footerImageBytes = LoadFooterImageBytes(template.FooterImageFileName);

            foreach (var labelSource in labelSources)
            {
                var hasBarcodeSource = !string.IsNullOrWhiteSpace(labelSource.BarcodePdfPath) && labelSource.BarcodePageNumber.HasValue;
                var html = BuildLabelHtml(template, hasBarcodeSource, includeImporterInfo, allowBarcodePlaceholder, footerImageBytes);
                AddHtmlPage(pdfDocument, pageSize, html);

                if (hasBarcodeSource)
                    AddBarcodeOverlay(pdfDocument, labelSource.BarcodePdfPath!, labelSource.BarcodePageNumber!.Value);
            }

            pdfDocument.Close();
            return outputPath;
        });
    }

    /// <summary>
    /// 将一段完整的 HTML 渲染到一个新的 PDF 页面上
    /// </summary>
    private void AddHtmlPage(ITextPdfDocument pdfDocument, PageSize pageSize, string html)
    {
        pdfDocument.AddNewPage(pageSize);

        var converterProperties = new ConverterProperties();
        var fontProvider = new BasicFontProvider(false, false);
        // 优先使用 Arial，回退到 Segoe UI
        var fontsDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        foreach (var fontFile in new[] { "arial.ttf", "arialbd.ttf", "segoeui.ttf", "segoeuib.ttf", "tahoma.ttf", "tahomabd.ttf", "msyh.ttc", "msyhbd.ttc", "simhei.ttf" })
        {
            var fontPath = System.IO.Path.Combine(fontsDir, fontFile);
            if (File.Exists(fontPath)) fontProvider.AddFont(fontPath);
        }
        converterProperties.SetFontProvider(fontProvider);

        var elements = HtmlConverter.ConvertToElements(html, converterProperties);

        // 在当前页面上用 Canvas 绘制所有 HTML 元素
        var page = pdfDocument.GetLastPage();
        var marginPt = MillimetersToPoints(0.6f);
        var rect = new Rectangle(
            marginPt, marginPt,
            page.GetPageSize().GetWidth() - marginPt * 2,
            page.GetPageSize().GetHeight() - marginPt * 2);

        using var canvas = new iText.Layout.Canvas(page, rect);
        foreach (var element in elements)
        {
            if (element is IBlockElement blockElement)
                canvas.Add(blockElement);
        }
    }

    private void AddBarcodeOverlay(ITextPdfDocument targetPdfDocument, string barcodePdfPath, int pageNumber)
    {
        if (!File.Exists(barcodePdfPath)) throw new FileNotFoundException($"条码 PDF 不存在: {barcodePdfPath}");

        using var barcodeReader = new PdfReader(barcodePdfPath);
        using var barcodeDocument = new ITextPdfDocument(barcodeReader);
        if (pageNumber < 1 || pageNumber > barcodeDocument.GetNumberOfPages())
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"条码页码超出范围: {pageNumber}");

        var barcodePage = barcodeDocument.GetPage(pageNumber);
        var barcodeForm = barcodePage.CopyAsFormXObject(targetPdfDocument);
        var targetPage = targetPdfDocument.GetLastPage();
        var targetRect = GetBarcodePlacementRect(targetPage.GetPageSize(), barcodePage.GetPageSize());

        new PdfCanvas(targetPage).AddXObjectFittedIntoRectangle(barcodeForm, targetRect);
    }

    private Rectangle GetBarcodePlacementRect(Rectangle pageSize, Rectangle barcodePageSize)
    {
        var labelLeft = MillimetersToPoints(LabelPageMarginMm);
        var labelBottom = pageSize.GetHeight() - MillimetersToPoints(LabelPageMarginMm) - MillimetersToPoints(LabelHeightMm);
        var frameX = labelLeft + MillimetersToPoints(BarcodeFrameOffsetLeftMm);
        var frameY = labelBottom + MillimetersToPoints(LabelHeightMm - BarcodeFrameOffsetTopMm - BarcodeFrameHeightMm);
        var frameWidth = MillimetersToPoints(BarcodeFrameWidthMm);
        var frameHeight = MillimetersToPoints(BarcodeFrameHeightMm);
        var sourceWidth = barcodePageSize.GetWidth();
        var sourceHeight = barcodePageSize.GetHeight();

        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return new Rectangle(frameX, frameY, frameWidth, frameHeight);
        }

        var scale = Math.Min(frameWidth / sourceWidth, frameHeight / sourceHeight);
        var scaledWidth = sourceWidth * scale;
        var scaledHeight = sourceHeight * scale;
        var x = frameX + (frameWidth - scaledWidth) / 2f;
        var y = frameY + (frameHeight - scaledHeight) / 2f;

        return new Rectangle(
            x,
            y,
            scaledWidth,
            scaledHeight);
    }

    // ==================== HTML 模板加载与数据填充 ====================

    private string? _labelLayoutHtmlCache;

    /// <summary>
    /// 加载 label_layout.html 模板文件（带缓存）
    /// </summary>
    private string LoadLabelLayoutHtml()
    {
        if (_labelLayoutHtmlCache != null) return _labelLayoutHtmlCache;

        var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "label_templates", "label_layout.html");
        if (!File.Exists(path)) throw new FileNotFoundException($"标签布局模板文件不存在: {path}");
        _labelLayoutHtmlCache = File.ReadAllText(path);
        return _labelLayoutHtmlCache;
    }

    private string BuildLabelHtml(
        LabelTemplateConfig template,
        bool hasBarcodeSource,
        bool includeImporterInfo,
        bool allowBarcodePlaceholder,
        byte[]? footerImageBytes)
    {
        var showImporter = includeImporterInfo && HasImporterInfo(template.ImporterInfo);
        var showFooter = footerImageBytes != null;
        var showWarning = !string.IsNullOrWhiteSpace(template.WarningText);

        var html = LoadLabelLayoutHtml();
        var layoutClass = BuildLayoutClass(showImporter, showWarning, showFooter);
        html = html.Replace("{{LAYOUT_CLASS}}", layoutClass);

        // 1. 条码
        string barcodeContent;
        if (hasBarcodeSource)
            barcodeContent = "";
        else if (allowBarcodePlaceholder)
            barcodeContent = "<div style='width:96.8mm; height:20.2mm; margin:auto; text-align:center; line-height:20.2mm; font-weight:bold; font-size:6.2pt;'>BARCODE PREVIEW</div>";
        else
            barcodeContent = "";
        html = html.Replace("{{BARCODE_CONTENT}}", barcodeContent);

        // 2. 制造商
        var mfgSb = new StringBuilder();
        var manufacturerItems = new[]
        {
            (Label: template.ManufacturerLabel, Value: template.ManufacturerName),
            (Label: template.AddressLabel, Value: template.ManufacturerAddress),
            (Label: template.ManufacturerEmailLabel, Value: template.ManufacturerEmail),
            (Label: template.BatchNumberLabel, Value: template.BatchNumber)
        }
        .Where(item => !string.IsNullOrWhiteSpace(item.Label) || !string.IsNullOrWhiteSpace(item.Value))
        .ToList();
        for (var index = 0; index < manufacturerItems.Count; index++)
        {
            var item = manufacturerItems[index];
            AppendKvHtml(mfgSb, item.Label, item.Value, index == manufacturerItems.Count - 1);
        }
        html = html.Replace("{{MANUFACTURER_DETAILS}}", mfgSb.ToString());

        // 3. 授权代表
        html = html.Replace("{{REPRESENTATIVES_CONTENT}}", BuildRepresentativesHtml(template));

        // 4. 进口商：标题栏 + 内容行
        if (showImporter)
        {
            var info = template.ImporterInfo;
            var importerRow = $@"
<div class='importer-section section-sep'><div class='importer-inner'>{BuildImporterSectionsHtml(info)}</div></div>";
            html = html.Replace("{{IMPORTER_ROW}}", importerRow);
        }
        else
        {
            html = html.Replace("{{IMPORTER_ROW}}", "");
        }

        // 5. 底部：警告 + 页脚图标
        if (showWarning || showFooter)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='bottom-section'><div class='bottom-inner'>");

            if (showFooter && showWarning)
            {
                sb.Append("<div class='bottom-stack'>");
                sb.Append("<div class='bottom-image-area'><div class='bottom-image-inner'>");
                sb.Append($"<img class='footer-image' src='data:image/png;base64,{Convert.ToBase64String(footerImageBytes!)}' />");
                sb.Append("</div></div>");
                sb.Append("<div class='bottom-warning-area'>");
                sb.Append($"<div class='warning-text'>{Esc(template.WarningText)}</div>");
                sb.Append("</div>");
                sb.Append("</div>");
            }
            else if (showFooter)
            {
                sb.Append("<div class='bottom-footer-full'><div class='bottom-footer-full-inner'>");
                sb.Append($"<img class='footer-image' src='data:image/png;base64,{Convert.ToBase64String(footerImageBytes!)}' />");
                sb.Append("</div></div>");
            }
            else if (showWarning)
            {
                sb.Append("<div class='bottom-warning-full'><div class='bottom-warning-full-inner'>");
                sb.Append("<div class='warning-title'>WARNING:</div>");
                sb.Append($"<div class='warning-text'>{Esc(template.WarningText)}</div>");
                sb.Append("</div></div>");
            }

            sb.Append("</div></div>");
            html = html.Replace("{{BOTTOM_ROW}}", sb.ToString());
        }
        else
        {
            html = html.Replace("{{BOTTOM_ROW}}", "");
        }

        return html;
    }

    private static string BuildLayoutClass(bool showImporter, bool showWarning, bool showFooter)
    {
        var classes = new List<string>
        {
            showImporter ? "layout-with-importer" : "layout-no-importer",
            showWarning ? "layout-with-warning" : "layout-no-warning",
            showFooter ? "layout-with-footer" : "layout-no-footer"
        };

        return string.Join(" ", classes);
    }

    private static void AppendKvHtml(StringBuilder sb, string label, string value, bool isLast = false)
    {
        if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value)) return;
        var cssClass = isLast ? "kv kv-last" : "kv";
        sb.Append($"<div class='{cssClass}'>");
        if (!string.IsNullOrWhiteSpace(label))
            sb.Append($"<span class='kv-label'>{Esc(label)}:</span>");
        if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
            sb.Append(" ");
        if (!string.IsNullOrWhiteSpace(value))
            sb.Append($"<span class='kv-value'>{Esc(value)}</span>");
        sb.Append("</div>");
    }

    private static string BuildRepresentativesHtml(LabelTemplateConfig template)
    {
        var items = template.Representatives
            .Where(r => !string.IsNullOrWhiteSpace(r.RegionCode) || !string.IsNullOrWhiteSpace(r.Name) || !string.IsNullOrWhiteSpace(r.Address) || !string.IsNullOrWhiteSpace(r.Email))
            .Take(3)
            .ToList();

        if (items.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append("<div class='rep-stack'>");
        for (var index = 0; index < items.Count; index++)
        {
            var rep = items[index];
            var rowClass = index == items.Count - 1 ? "rep-row rep-row-last" : "rep-row";
            sb.Append($"<div class='{rowClass}'>");
            sb.Append("<div class='rep-badges-cell'>");
            sb.Append("<table class='badge-grid' cellspacing='0' cellpadding='0'><tr>");
            sb.Append($"<td class='badge' style='width:50%;'>{Esc(rep.RegionCode)}</td>");
            sb.Append($"<td class='badge' style='width:50%;'>{Esc(template.RepresentativeLabel)}</td>");
            sb.Append("</tr></table>");
            sb.Append("</div>");
            sb.Append("<div class='rep-info'>");
            if (!string.IsNullOrWhiteSpace(rep.Name))
                sb.Append($"<div class='rep-name'>{Esc(rep.Name)}</div>");
            if (!string.IsNullOrWhiteSpace(rep.Address))
                sb.Append($"<div class='rep-address'>{Esc(rep.Address)}</div>");
            if (!string.IsNullOrWhiteSpace(rep.Email))
                sb.Append($"<div class='rep-email'>{Esc(rep.Email)}</div>");
            sb.Append("</div>");
            sb.Append("</div>");
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildImporterSectionsHtml(LabelImporterInfo info)
    {
        var blocks = new List<string>();
        if (HasAnyText(info.EuImporterName, info.EuImporterAddress, info.EuImporterElectronicAddress))
            blocks.Add(BuildImporterBlockHtml(
                info.EuTitle,
                (info.EuImporterNameLabel, info.EuImporterName),
                (info.EuImporterAddressLabel, info.EuImporterAddress),
                (info.EuImporterElectronicAddressLabel, info.EuImporterElectronicAddress)));
        if (HasAnyText(info.UkImporterName, info.UkImporterAddress))
            blocks.Add(BuildImporterBlockHtml(
                info.UkTitle,
                (info.UkImporterNameLabel, info.UkImporterName),
                (info.UkImporterAddressLabel, info.UkImporterAddress)));

        var sb = new StringBuilder();
        for (var index = 0; index < blocks.Count; index++)
        {
            var cssClass = index == blocks.Count - 1 ? "importer-block importer-block-last" : "importer-block";
            sb.Append($"<div class='{cssClass}'>{blocks[index]}</div>");
        }
        return sb.ToString();
    }

    private static string BuildImporterBlockHtml(string title, params (string? Label, string? Value)[] fields)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title))
            sb.Append($"<div class='importer-title'>{Esc(title)}</div>");
        foreach (var field in fields)
            AppendImporterLine(sb, field.Label, field.Value);
        return sb.ToString();
    }

    private static void AppendImporterLine(StringBuilder sb, string? label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append("<div class='imp-line'>");
        if (!string.IsNullOrWhiteSpace(label))
            sb.Append($"<span class='imp-inline-label'>{Esc(label)}:</span> ");
        sb.Append($"<span class='imp-inline-value'>{Esc(value)}</span>");
        sb.Append("</div>");
    }

    private static bool HasAnyText(params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value));
    }

    /// <summary>HTML 转义，防止特殊字符破坏结构</summary>
    private static string Esc(string? text) => WebUtility.HtmlEncode(text ?? "");

    private bool HasImporterInfo(LabelImporterInfo importerInfo)
    {
        return !string.IsNullOrWhiteSpace(importerInfo.EuImporterName) ||
               !string.IsNullOrWhiteSpace(importerInfo.EuImporterAddress) ||
               !string.IsNullOrWhiteSpace(importerInfo.EuImporterElectronicAddress) ||
               !string.IsNullOrWhiteSpace(importerInfo.UkImporterName) ||
               !string.IsNullOrWhiteSpace(importerInfo.UkImporterAddress);
    }

    private byte[]? LoadFooterImageBytes(string fileName)
    {
        var imagePath = _printService.GetBuiltinResourcePath(fileName);
        return File.Exists(imagePath) ? File.ReadAllBytes(imagePath) : null;
    }

    private LabelTemplateConfig? ParseTemplate(string templateJson)
    {
        var template = JsonSerializer.Deserialize<LabelTemplateConfig>(templateJson, _jsonOptions);
        if (template == null) return null;

        template.Id = string.IsNullOrWhiteSpace(template.Id) ? $"template_{DateTime.Now:yyyyMMddHHmmss}" : template.Id.Trim();
        template.Name = string.IsNullOrWhiteSpace(template.Name) ? template.Id : template.Name.Trim();
        template.Representatives ??= new List<LabelRepresentativeInfo>();
        template.ImporterInfo ??= new LabelImporterInfo();
        template.FooterImageFileName = string.IsNullOrWhiteSpace(template.FooterImageFileName) ? "环保标识.png" : template.FooterImageFileName;
        return template;
    }

    private void EnsureTemplateIdIsUnique(string templateId, string? originalFilePath)
    {
        var templateDirectory = GetTemplateDirectoryPath();
        foreach (var filePath in Directory.GetFiles(templateDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            if (!string.IsNullOrWhiteSpace(originalFilePath) &&
                string.Equals(filePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var existingTemplate = ParseTemplate(json);
                if (existingTemplate != null &&
                    string.Equals(existingTemplate.Id, templateId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"模板 ID 已存在: {templateId}");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
            }
        }
    }

    private void EnsureTemplateDirectoryInitialized()
    {
        var templateDirectory = GetTemplateDirectoryPath();
        Directory.CreateDirectory(templateDirectory);

        var builtinTemplateDirectory = GetBuiltinTemplateDirectoryPath();
        if (!Directory.Exists(builtinTemplateDirectory)) return;

        foreach (var filePath in Directory.GetFiles(builtinTemplateDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var targetPath = System.IO.Path.Combine(templateDirectory, System.IO.Path.GetFileName(filePath));
            if (!File.Exists(targetPath)) File.Copy(filePath, targetPath);
        }
    }

    private string GetTemplateDirectoryPath() => System.IO.Path.Combine(_configService.GetAppDataPath(), "label_templates");
    private string GetBuiltinTemplateDirectoryPath() => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "label_templates");
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
    }
    private static float MillimetersToPoints(float millimeters) => millimeters * 72f / 25.4f;
}