using FileCommander.Api.Models;

namespace FileCommander.Api.Services;

public interface IFileSystemService
{
    IEnumerable<string> GetDrives();

    IEnumerable<FileEntryDto> GetEntries(string path);

    Task CopyAsync(IEnumerable<string> sourcePaths, string destinationDirectory, CancellationToken cancellationToken = default);

    Task MoveAsync(IEnumerable<string> sourcePaths, string destinationDirectory, CancellationToken cancellationToken = default);

    Task DeleteAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
}

