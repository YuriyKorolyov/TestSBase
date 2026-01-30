namespace FileCommander.Api.Models;

public class FileOperationRequest
{
    public List<string> SourcePaths { get; set; } = new();
    public required string DestinationDirectory { get; set; }
}

