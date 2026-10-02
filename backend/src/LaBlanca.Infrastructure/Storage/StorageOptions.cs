namespace LaBlanca.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Pasta dos arquivos enviados; relativa à raiz da API quando não for absoluta. Nunca dentro de wwwroot.</summary>
    public string RootPath { get; set; } = "storage";
}
