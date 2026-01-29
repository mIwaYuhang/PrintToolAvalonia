using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace PrintToolAvalonia.ViewModels;

/// <summary>
/// PDF 预览对话框 ViewModel
/// </summary>
public class PdfPreviewViewModel : ViewModelBase
{
    private string _fileName = string.Empty;
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    private string _filePath = string.Empty;
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    private int _currentPage = 1;
    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
    }

    private int _totalPages = 1;
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }
    }

    /// <summary>
    /// 是否可以上一页
    /// </summary>
    public bool CanGoPrevious => CurrentPage > 1;

    /// <summary>
    /// 是否可以下一页
    /// </summary>
    public bool CanGoNext => CurrentPage < TotalPages;

    // ========== 命令 ==========

    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand CloseCommand { get; }

    // ========== 事件 ==========

    /// <summary>
    /// 请求关闭对话框事件
    /// </summary>
    public event EventHandler? RequestClose;

    public PdfPreviewViewModel()
    {
        // 初始化命令
        PreviousPageCommand = new RelayCommand(PreviousPage, () => CanGoPrevious);
        NextPageCommand = new RelayCommand(NextPage, () => CanGoNext);
        CloseCommand = new RelayCommand(Close);
    }

    /// <summary>
    /// 初始化预览
    /// </summary>
    /// <param name="filePath">PDF 文件路径</param>
    /// <param name="fileName">文件名</param>
    public void Initialize(string filePath, string fileName)
    {
        FilePath = filePath;
        FileName = fileName;
        CurrentPage = 1;
        
        // TODO: 加载 PDF 并获取总页数
        // 这里暂时设置为 1
        TotalPages = 1;
    }

    /// <summary>
    /// 上一页
    /// </summary>
    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
            // TODO: 渲染当前页
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    private void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            // TODO: 渲染当前页
        }
    }

    /// <summary>
    /// 关闭对话框
    /// </summary>
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
