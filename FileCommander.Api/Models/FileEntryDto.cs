namespace FileCommander.Api.Models;

public class FileEntryDto
{
    public required string Name { get; set; }
    public required string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public long? SizeBytes { get; set; }
    public required string FormattedSize { get; set; }
    public DateTime LastModified { get; set; }
    public required string Type { get; set; }
    public required string IconKey { get; set; }
}

