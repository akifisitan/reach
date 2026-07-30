using System.Text.Json;
using System.Text.Json.Nodes;

namespace Reach.Infra.JsonConfig;

public interface IJsonConfigFile
{
    JsonObject Read();
    void SetSection(string sectionName, JsonObject section);
}

public sealed class JsonConfigFile : IJsonConfigFile
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonConfigFile(string filePath)
    {
        _filePath = filePath;
    }

    public JsonObject Read()
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(_filePath)) as JsonObject ?? new JsonObject();
        }
        catch (Exception)
        {
            return new JsonObject();
        }
    }

    public void SetSection(string sectionName, JsonObject section)
    {
        var root = Read();

        root[sectionName] = section;

        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, root.ToJsonString(WriteOptions));
    }
}
