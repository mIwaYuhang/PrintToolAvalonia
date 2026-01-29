using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 环保码管理 ViewModel
/// </summary>
public class EcoCodeViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IDatabaseService _databaseService;
    
    /// <summary>
    /// 父窗口引用（用于显示对话框）
    /// </summary>
    public Avalonia.Controls.Window? OwnerWindow { get; set; }

    // ========== 环保码列表 ==========
    
    /// <summary>
    /// 环保码列表
    /// </summary>
    public ObservableCollection<EcoCodeItem> EcoCodes { get; } = new();

    // ========== 命令 ==========
    
    public ICommand AddEcoCodeCommand { get; }
    public ICommand DeleteEcoCodeCommand { get; }
    public ICommand RenameEcoCodeCommand { get; }

    public EcoCodeViewModel(
        IFileService fileService,
        IDatabaseService databaseService)
    {
        _fileService = fileService;
        _databaseService = databaseService;

        // 初始化命令
        AddEcoCodeCommand = new AsyncRelayCommand(AddEcoCodeAsync);
        DeleteEcoCodeCommand = new AsyncRelayCommand<EcoCodeItem>(DeleteEcoCodeAsync);
        RenameEcoCodeCommand = new AsyncRelayCommand<EcoCodeItem>(RenameEcoCodeAsync);

        // 加载数据
        _ = LoadEcoCodesAsync();
    }

    /// <summary>
    /// 加载环保码列表
    /// </summary>
    private async Task LoadEcoCodesAsync()
    {
        try
        {
            var ecoCodes = await _databaseService.GetAllEcoCodesAsync();
            
            EcoCodes.Clear();
            foreach (var item in ecoCodes)
            {
                EcoCodes.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载环保码列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加环保码
    /// </summary>
    private async Task AddEcoCodeAsync()
    {
        try
        {
            // 打开文件选择对话框
            var files = await _fileService.OpenFileDialogAsync("PDF Files|*.pdf");
            
            if (files.Length == 0)
            {
                return;
            }

            var filePath = files[0];
            
            // 验证 PDF
            var validation = await _fileService.ValidatePdfAsync(filePath);
            if (!validation.IsValid)
            {
                await ShowErrorAsync($"PDF 验证失败: {validation.Error}");
                return;
            }

            // 使用文件名作为默认名称（不含扩展名）
            var defaultName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // 显示命名对话框让用户输入名称
            var ecoCodeName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入环保码名称：",
                defaultName,
                "环保码名称"
            );
            
            // 用户取消输入
            if (string.IsNullOrWhiteSpace(ecoCodeName))
            {
                return;
            }

            // 生成唯一文件名
            var fileName = $"eco_code_{Guid.NewGuid()}.pdf";
            
            string? destPath = null;
            EcoCodeItem? ecoCode = null;
            
            try
            {
                // 复制文件到应用数据目录
                destPath = await _fileService.CopyToAppDataAsync(filePath, fileName);

                // 创建环保码项
                ecoCode = new EcoCodeItem
                {
                    Name = ecoCodeName,
                    FileName = fileName
                };

                // 保存到数据库
                await _databaseService.AddEcoCodeAsync(ecoCode);
                
                // 添加到列表
                EcoCodes.Add(ecoCode);

                await ShowInfoAsync($"环保码添加成功: {ecoCode.Name}");
            }
            catch (Exception ex)
            {
                // 如果数据库保存失败，删除已复制的文件以保持一致性
                if (destPath != null && System.IO.File.Exists(destPath))
                {
                    try
                    {
                        await _fileService.DeleteFileAsync(destPath);
                    }
                    catch
                    {
                        // 忽略删除失败
                    }
                }
                
                throw new InvalidOperationException($"添加环保码失败: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"添加环保码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除环保码
    /// </summary>
    private async Task DeleteEcoCodeAsync(EcoCodeItem? ecoCode)
    {
        if (ecoCode == null) return;

        try
        {
            // TODO: 添加确认对话框
            // 这里暂时直接删除
            var confirmed = true; // await ShowConfirmAsync($"确定要删除环保码"{ecoCode.Name}"吗？");
            
            if (!confirmed)
            {
                return;
            }

            // 先从数据库删除
            await _databaseService.DeleteEcoCodeAsync(ecoCode.Id);
            
            // 从列表移除
            EcoCodes.Remove(ecoCode);

            // 最后删除文件（即使失败也不影响数据一致性）
            try
            {
                var appDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrintToolAvalonia",
                    "eco_codes",
                    ecoCode.FileName
                );
                
                await _fileService.DeleteFileAsync(appDataPath);
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"删除文件失败（已从数据库删除）: {fileEx.Message}");
            }

            await ShowInfoAsync($"环保码删除成功: {ecoCode.Name}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"删除环保码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 重命名环保码
    /// </summary>
    private async Task RenameEcoCodeAsync(EcoCodeItem? ecoCode)
    {
        if (ecoCode == null) return;

        try
        {
            // 显示输入对话框获取新名称
            var newName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入新的环保码名称：",
                ecoCode.Name,
                "环保码名称"
            );
            
            // 验证名称不为空
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }
            
            // 如果名称没有变化，直接返回
            if (newName == ecoCode.Name)
            {
                return;
            }

            // 更新名称
            var oldName = ecoCode.Name;
            ecoCode.Name = newName;
            
            // 更新数据库
            await _databaseService.UpdateEcoCodeAsync(ecoCode.Id, ecoCode);

            await ShowInfoAsync($"环保码重命名成功: {oldName} → {newName}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"重命名环保码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算环保码数量
    /// 每 5 个主单需要 1 个环保码
    /// </summary>
    public int CalculateQuantity(int mainOrderCount)
    {
        if (mainOrderCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(mainOrderCount / 5.0);
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private async Task ShowErrorAsync(string message)
    {
        await Views.MessageDialog.ShowErrorAsync(OwnerWindow, message);
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    private async Task ShowInfoAsync(string message)
    {
        await Views.MessageDialog.ShowInfoAsync(OwnerWindow, message);
    }
}
