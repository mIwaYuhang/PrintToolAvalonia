using System;
using Avalonia.Controls;
using PrintToolAvalonia.ViewModels;

namespace PrintToolAvalonia.Views;

public partial class LabelTemplateEditorDialog : Window
{
    public LabelTemplateEditorDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is LabelTemplateEditorViewModel viewModel)
        {
            viewModel.OwnerWindow = this;
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
