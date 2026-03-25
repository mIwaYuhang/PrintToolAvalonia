using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PrintToolAvalonia.Models;

public partial class TemplateLabelQueueItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string BarcodePdfPath { get; set; } = string.Empty;

    public BarcodeGroup BarcodeGroup { get; set; } = new();

    [ObservableProperty]
    private string _templateName = string.Empty;

    public int LabelCount => BarcodeGroup.BarcodeCount;

    public string GroupDisplay => $"第{BarcodeGroup.StartPage}-{BarcodeGroup.EndPage}页";

    public string SourceFileName => Path.GetFileName(BarcodePdfPath);
}
