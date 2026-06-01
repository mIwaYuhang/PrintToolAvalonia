using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PrintToolAvalonia.Models;

public class LabelTemplateConfig
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 布局变体：temu 或 shein
    /// </summary>
    public string LayoutVariant { get; set; } = "temu";

    // ========== 商品名称（希音专用，打印时动态传入） ==========

    public string ProductNameLabel { get; set; } = "Product Name";

    // ========== 制造商信息 ==========

    public string BatchNumberLabel { get; set; } = "Batch Number/Parti Numaras";

    public string BatchNumber { get; set; } = string.Empty;

    public string ManufacturerLabel { get; set; } = "Manufacturer/Uretici";

    public string ManufacturerName { get; set; } = string.Empty;

    public string AddressLabel { get; set; } = "Address/Adres";

    public string ManufacturerAddress { get; set; } = string.Empty;

    public string ManufacturerEmailLabel { get; set; } = "Manufacturer E-mail";

    public string ManufacturerEmail { get; set; } = string.Empty;

    // ========== 授权代表 ==========

    public string RepresentativeLabel { get; set; } = "REP";

    public List<LabelRepresentativeInfo> Representatives { get; set; } = new();

    // ========== 进口商信息 ==========

    public LabelImporterInfo ImporterInfo { get; set; } = new();

    // ========== 底部信息（希音专用） ==========

    /// <summary>
    /// 产地标签（如 "Made in China"）
    /// </summary>
    public string MadeInText { get; set; } = string.Empty;

    /// <summary>
    /// 包装材质标识（如 "PP 5   Raccolta Plastica"）
    /// </summary>
    public string PackagingMaterialText { get; set; } = string.Empty;

    // ========== 页脚 ==========

    public string FooterImageFileName { get; set; } = "环保标识.png";

    public string WarningText { get; set; } = string.Empty;

    [JsonIgnore]
    public string SourceFilePath { get; set; } = string.Empty;
}

public class LabelRepresentativeInfo
{
    public string RegionCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class LabelImporterInfo
{
    public string EuTitle { get; set; } = "For EU";

    public string EuImporterNameLabel { get; set; } = "EU Importer Name";

    public string EuImporterName { get; set; } = string.Empty;

    public string EuImporterAddressLabel { get; set; } = "EU Importer Address";

    public string EuImporterAddress { get; set; } = string.Empty;

    public string EuImporterElectronicAddressLabel { get; set; } = "EU Importer Electronic Address";

    public string EuImporterElectronicAddress { get; set; } = string.Empty;

    public string UkTitle { get; set; } = "For UK";

    public string UkImporterNameLabel { get; set; } = "UK Importer Name";

    public string UkImporterName { get; set; } = string.Empty;

    public string UkImporterAddressLabel { get; set; } = "UK Importer Address";

    public string UkImporterAddress { get; set; } = string.Empty;

    public string UkImporterElectronicAddressLabel { get; set; } = "UK Importer Electronic Address";

    public string UkImporterElectronicAddress { get; set; } = string.Empty;
}
