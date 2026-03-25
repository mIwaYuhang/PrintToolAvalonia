using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PrintToolAvalonia.ViewModels;

namespace PrintToolAvalonia.Views;

public partial class TemplateLabelPrintDialog : Window
{
    public TemplateLabelPrintDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TemplateLabelPrintViewModel viewModel)
        {
            viewModel.OwnerWindow = this;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
