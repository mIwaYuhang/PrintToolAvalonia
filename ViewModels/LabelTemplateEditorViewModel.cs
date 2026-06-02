using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

public partial class LabelTemplateEditorViewModel : ViewModelBase
{
    private readonly ILabelTemplateService _labelTemplateService;
    private readonly IPdfRenderService _pdfRenderService;
    private CancellationTokenSource? _previewCts;
    private string? _originalFilePath;
    private string? _previewBarcodePdfPath;
    private int? _previewBarcodePageNumber;
    private bool _isInitializing;

    public event EventHandler? CloseRequested;

    public Window? OwnerWindow { get; set; }

    public LabelTemplateConfig? SavedTemplate { get; private set; }

    [ObservableProperty]
    private string _dialogTitle = "新建模板";

    [ObservableProperty]
    private string _templateJsonText = string.Empty;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private bool _isPreviewLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _previewStatusMessage = "编辑模板后将实时显示最终打印效果";

    [ObservableProperty]
    private bool _includeImporterInfo = true;

    [ObservableProperty]
    private string _labelSizeText = "100mm × 100mm";

    public LabelTemplateEditorViewModel(ILabelTemplateService labelTemplateService, IPdfRenderService pdfRenderService)
    {
        _labelTemplateService = labelTemplateService;
        _pdfRenderService = pdfRenderService;
    }

    public async Task InitializeAsync(
        LabelTemplateConfig? template,
        string? previewBarcodePdfPath,
        int? previewBarcodePageNumber,
        bool includeImporterInfo,
        string newTemplateLayoutVariant = "temu")
    {
        _isInitializing = true;
        _previewBarcodePdfPath = previewBarcodePdfPath;
        _previewBarcodePageNumber = previewBarcodePageNumber;
        IncludeImporterInfo = includeImporterInfo;
        _originalFilePath = template?.SourceFilePath;
        DialogTitle = template == null ? "新建模板" : $"编辑模板 - {template.Name}";
        TemplateJsonText = template == null
            ? _labelTemplateService.CreateNewTemplateJson(newTemplateLayoutVariant)
            : await _labelTemplateService.GetTemplateJsonAsync(template);
        UpdateLabelSizeText();
        _isInitializing = false;
        await RefreshPreviewAsync();
    }

    /// <summary>
    /// 根据当前 JSON 中的 layoutVariant 更新标签尺寸提示
    /// 冷希音特供款(shein_special)为 60mm × 80mm，其余为 100mm × 100mm
    /// </summary>
    private void UpdateLabelSizeText()
    {
        var variant = ExtractLayoutVariant(TemplateJsonText);
        LabelSizeText = string.Equals(variant, "shein_special", StringComparison.OrdinalIgnoreCase)
            ? "60mm × 80mm"
            : "100mm × 100mm";
    }

    private static string ExtractLayoutVariant(string templateJson)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
        {
            return "temu";
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(templateJson);
            if (doc.RootElement.TryGetProperty("layoutVariant", out var element) &&
                element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = element.GetString();
                return string.IsNullOrWhiteSpace(value) ? "temu" : value.Trim().ToLowerInvariant();
            }
        }
        catch
        {
            // JSON 编辑过程中可能不合法，忽略并保持默认
        }

        return "temu";
    }

    partial void OnTemplateJsonTextChanged(string value)
    {
        if (_isInitializing)
        {
            return;
        }

        SchedulePreviewRefresh();
    }

    partial void OnIncludeImporterInfoChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        SchedulePreviewRefresh();
    }

    [RelayCommand]
    private async Task RefreshPreviewAsync()
    {
        try
        {
            IsPreviewLoading = true;
            PreviewStatusMessage = "正在生成打印预览...";
            UpdateLabelSizeText();

            var previewPdfPath = await _labelTemplateService.GeneratePreviewPdfAsync(
                TemplateJsonText,
                _previewBarcodePdfPath,
                _previewBarcodePageNumber,
                IncludeImporterInfo);

            PreviewImage = await _pdfRenderService.RenderPageAsync(previewPdfPath, 1, 450);
            PreviewStatusMessage = string.IsNullOrWhiteSpace(_previewBarcodePdfPath)
                ? "当前预览为无条码占位效果"
                : "当前预览已合并真实条码内容";
        }
        catch (Exception ex)
        {
            PreviewImage = null;
            PreviewStatusMessage = $"预览生成失败: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            IsPreviewLoading = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;
            var template = await _labelTemplateService.SaveTemplateAsync(TemplateJsonText, _originalFilePath);
            SavedTemplate = template;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"保存模板失败: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave()
    {
        return !IsSaving && !string.IsNullOrWhiteSpace(TemplateJsonText);
    }

    private void SchedulePreviewRefresh()
    {
        _previewCts?.Cancel();
        var cts = new CancellationTokenSource();
        _previewCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(async () => await RefreshPreviewAsync());
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    private async Task ShowErrorAsync(string message)
    {
        await Views.MessageDialog.ShowErrorAsync(OwnerWindow, message);
    }
}
