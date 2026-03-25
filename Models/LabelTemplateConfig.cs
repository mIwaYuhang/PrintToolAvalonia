using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PrintToolAvalonia.Models;

public class LabelTemplateConfig
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string BatchNumberLabel { get; set; } = "Batch Number/Parti Numaras";

    public string BatchNumber { get; set; } = string.Empty;

    public string ManufacturerLabel { get; set; } = "Manufacturer/Uretici";

    public string ManufacturerName { get; set; } = string.Empty;

    public string AddressLabel { get; set; } = "Address/Adres";

    public string ManufacturerAddress { get; set; } = string.Empty;

    public string ManufacturerEmailLabel { get; set; } = "Manufacturer E-mail";

    public string ManufacturerEmail { get; set; } = string.Empty;

    public string RepresentativeLabel { get; set; } = "REP";

    public List<LabelRepresentativeInfo> Representatives { get; set; } = new();

    public LabelImporterInfo ImporterInfo { get; set; } = new();

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
}
