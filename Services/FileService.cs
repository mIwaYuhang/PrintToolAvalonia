using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using iText.Kernel.Pdf;

namespace PrintToolAvalonia.Services;

/// <summary>
/// 文件服务实现
/// </summary>
public class FileService : IFileService
{
    private readonly IConfigService _configService;

    public FileService(IConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 验证 PDF 文件
    /// </summary>
    public async Task<PdfValidationResult> ValidatePdfAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new PdfValidationResult
                    {
                        IsValid = false,
                        Error = "文件不存在"
                    };
                }

                // 使用 iText7 验证 PDF
                using var pdfReader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(pdfReader);
                
                var pageCount = pdfDocument.GetNumberOfPages();

                return new PdfValidationResult
                {
                    IsValid = true,
                    PageCount = pageCount
                };
            }
            catch (Exception ex)
            {
                return new PdfValidationResult
                {
                    IsValid = false,
                    Error = $"PDF 验证失败: {ex.Message}"
                };
            }
        });
    }

    /// <summary>
    /// 复制文件到应用数据目录
    /// </summary>
    public async Task<string> CopyToAppDataAsync(string sourcePath, string destName)
    {
        try
        {
            var appDataPath = _configService.GetAppDataPath();
            
            // 创建 eco_codes 子目录
            var ecoCodesDir = Path.Combine(appDataPath, "eco_codes");
            if (!Directory.Exists(ecoCodesDir))
            {
                Directory.CreateDirectory(ecoCodesDir);
            }

            var destPath = Path.Combine(ecoCodesDir, destName);
            
            // 复制文件
            await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: true));
            
            return destPath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"复制文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"删除文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 打开文件选择对话框
    /// </summary>
    public async Task<string[]> OpenFileDialogAsync(string filter)
    {
        return await OpenFileDialogAsync(filter, null);
    }

    /// <summary>
    /// 打开文件选择对话框（带父窗口）
    /// </summary>
    public async Task<string[]> OpenFileDialogAsync(string filter, Window? owner)
    {
        try
        {
            // 获取窗口（优先使用传入的父窗口）
            var window = owner ?? GetMainWindow();
            if (window == null)
            {
                return Array.Empty<string>();
            }

            // 创建文件类型过滤器
            var fileTypeFilter = new FilePickerFileType("PDF Files")
            {
                Patterns = new[] { "*.pdf" }
            };

            // 打开文件选择对话框
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择 PDF 文件",
                AllowMultiple = true,
                FileTypeFilter = new[] { fileTypeFilter }
            });

            // 返回选择的文件路径
            return files.Select(f => f.Path.LocalPath).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"打开文件对话框失败: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// 获取主窗口（用于文件对话框）
    /// </summary>
    private Window? GetMainWindow()
    {
        // 从 Application 获取主窗口
        if (Avalonia.Application.Current?.ApplicationLifetime 
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}
