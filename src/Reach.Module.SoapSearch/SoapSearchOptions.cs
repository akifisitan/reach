namespace Reach.Module.SoapSearch;

internal sealed class SoapSearchOptions
{
    public const string SectionName = "SoapSearch";

    public string MetadataFilePath { get; set; } = "cached-metadata.json";
    public string LiveServerBasePath { get; set; } = string.Empty;
    public string ArchiveServerBasePath { get; set; } = string.Empty;
    public string LogPathPrefix { get; set; } = string.Empty;
    public int CreationTimeBufferInSeconds { get; set; }
    public int ZipCreationTimeBufferInSeconds { get; set; } = 5;
}
