using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PrintToolAvalonia.Models;
using PrintToolAvalonia.Services;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// 商品名称管理 ViewModel
/// </summary>
public class ProductNameViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;

    /// <summary>
    /// 父窗口引用（用于显示对话框）
    /// </summary>
    public Avalonia.Controls.Window? OwnerWindow { get; set; }

    /// <summary>
    /// 商品名称列表
    /// </summary>
    public ObservableCollection<ProductNameItem> ProductNames { get; } = new();

    public ICommand AddProductNameCommand { get; }
    public ICommand DeleteProductNameCommand { get; }
    public ICommand EditProductNameCommand { get; }

    public ProductNameViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;

        AddProductNameCommand = new AsyncRelayCommand(AddProductNameAsync);
        DeleteProductNameCommand = new AsyncRelayCommand<ProductNameItem>(DeleteProductNameAsync);
        EditProductNameCommand = new AsyncRelayCommand<ProductNameItem>(EditProductNameAsync);

        _ = LoadProductNamesAsync();
    }

    private async Task LoadProductNamesAsync()
    {
        try
        {
            var items = await _databaseService.GetAllProductNamesAsync();
            ProductNames.Clear();
            foreach (var item in items)
            {
                ProductNames.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载商品名称列表失败: {ex.Message}");
        }
    }

    private async Task AddProductNameAsync()
    {
        try
        {
            // 输入中文名称
            var chineseName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入商品名称（中文）：",
                "",
                "中文商品名称，如：一串大蒜"
            );

            if (string.IsNullOrWhiteSpace(chineseName))
            {
                return;
            }

            // 输入英文名称
            var englishName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入商品名称（英文）：\n此名称将打印到标签上",
                "",
                "英文商品名称，如：Artificial Plants"
            );

            if (string.IsNullOrWhiteSpace(englishName))
            {
                return;
            }

            var item = new ProductNameItem
            {
                ChineseName = chineseName.Trim(),
                EnglishName = englishName.Trim()
            };

            await _databaseService.AddProductNameAsync(item);
            ProductNames.Add(item);

            await ShowInfoAsync($"商品名称添加成功: {item.DisplayName}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"添加商品名称失败: {ex.Message}");
        }
    }

    private async Task DeleteProductNameAsync(ProductNameItem? item)
    {
        if (item == null) return;

        try
        {
            await _databaseService.DeleteProductNameAsync(item.Id);
            ProductNames.Remove(item);
            await ShowInfoAsync($"商品名称删除成功: {item.DisplayName}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"删除商品名称失败: {ex.Message}");
        }
    }

    private async Task EditProductNameAsync(ProductNameItem? item)
    {
        if (item == null) return;

        try
        {
            // 编辑中文名称
            var chineseName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入商品名称（中文）：",
                item.ChineseName,
                "中文商品名称"
            );

            if (chineseName == null) return; // 用户取消

            // 编辑英文名称
            var englishName = await Views.InputDialog.ShowAsync(
                OwnerWindow,
                "请输入商品名称（英文）：\n此名称将打印到标签上",
                item.EnglishName,
                "英文商品名称"
            );

            if (englishName == null) return; // 用户取消

            item.ChineseName = chineseName.Trim();
            item.EnglishName = englishName.Trim();

            await _databaseService.UpdateProductNameAsync(item.Id, item);

            // 刷新列表
            await LoadProductNamesAsync();

            await ShowInfoAsync($"商品名称修改成功: {item.DisplayName}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"修改商品名称失败: {ex.Message}");
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        await Views.MessageDialog.ShowErrorAsync(OwnerWindow, message);
    }

    private async Task ShowInfoAsync(string message)
    {
        await Views.MessageDialog.ShowInfoAsync(OwnerWindow, message);
    }
}
