namespace VentasETL.Core.Configurations;

public class EtlOptions
{
    public const string SectionName = "ETLSettings";

    public string DataSourcesPath { get; set; } = string.Empty;
    public CsvFilesOptions CsvFiles { get; set; } = new();
    public ApiSettingsOptions ApiSettings { get; set; } = new();
    public ConnectionStringsOptions ConnectionStrings { get; set; } = new();
}

public class CsvFilesOptions
{
    public string ClientesPath { get; set; } = string.Empty;
    public string ProductosPath { get; set; } = string.Empty;
    public string VentasPath { get; set; } = string.Empty;
}

public class ApiSettingsOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class ConnectionStringsOptions
{
    public string ExternalDatabase { get; set; } = string.Empty;
}
