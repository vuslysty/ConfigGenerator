using System.IO;

namespace ConfigGenerator.Application.Artifacts;

public sealed class FileArtifactWriter : IArtifactWriter
{
    public void WriteText(string filePath, string content)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
    }
}
