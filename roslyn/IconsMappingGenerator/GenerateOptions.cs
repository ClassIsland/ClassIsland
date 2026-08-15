namespace IconsMappingGenerator;

public record GenerateOptions(string FilePath)
{
    public string FilePath { get; } = FilePath;
}