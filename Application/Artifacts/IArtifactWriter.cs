namespace ConfigGenerator.Application.Artifacts;

public interface IArtifactWriter
{
    void WriteText(string filePath, string content);
}
