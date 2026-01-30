using FileCommander.Api.Models;

namespace FileCommander.Api.Services;

public class FileSystemService : IFileSystemService
{
    public IEnumerable<string> GetDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable)
            .Select(d => d.Name);
    }

    public IEnumerable<FileEntryDto> GetEntries(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException(path);
        }

        var entries = new List<FileEntryDto>();

        foreach (var dir in directoryInfo.EnumerateDirectories())
        {
            entries.Add(CreateDirectoryDto(dir));
        }

        foreach (var file in directoryInfo.EnumerateFiles())
        {
            entries.Add(CreateFileDto(file));
        }

        return entries.OrderBy(e => !e.IsDirectory).ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase);
    }

    public Task CopyAsync(IEnumerable<string> sourcePaths, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (sourcePaths == null) throw new ArgumentNullException(nameof(sourcePaths));
        if (string.IsNullOrWhiteSpace(destinationDirectory)) throw new ArgumentException("Destination is required.", nameof(destinationDirectory));

        var destDir = new DirectoryInfo(destinationDirectory);
        if (!destDir.Exists)
        {
            throw new DirectoryNotFoundException(destinationDirectory);
        }

        foreach (var path in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(path))
            {
                var sourceDir = new DirectoryInfo(path);
                var targetDirPath = Path.Combine(destDir.FullName, sourceDir.Name);
                CopyDirectory(sourceDir.FullName, targetDirPath);
            }
            else if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var targetPath = Path.Combine(destDir.FullName, fileInfo.Name);
                File.Copy(fileInfo.FullName, targetPath, overwrite: false);
            }
        }

        return Task.CompletedTask;
    }

    public Task MoveAsync(IEnumerable<string> sourcePaths, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (sourcePaths == null) throw new ArgumentNullException(nameof(sourcePaths));
        if (string.IsNullOrWhiteSpace(destinationDirectory)) throw new ArgumentException("Destination is required.", nameof(destinationDirectory));

        var destDir = new DirectoryInfo(destinationDirectory);
        if (!destDir.Exists)
        {
            throw new DirectoryNotFoundException(destinationDirectory);
        }

        foreach (var path in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(path))
            {
                var sourceDir = new DirectoryInfo(path);
                var targetDirPath = Path.Combine(destDir.FullName, sourceDir.Name);
                Directory.Move(sourceDir.FullName, targetDirPath);
            }
            else if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var targetPath = Path.Combine(destDir.FullName, fileInfo.Name);
                File.Move(fileInfo.FullName, targetPath);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        if (paths == null) throw new ArgumentNullException(nameof(paths));

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }

    private static FileEntryDto CreateDirectoryDto(DirectoryInfo dir)
    {
        return new FileEntryDto
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            IsDirectory = true,
            SizeBytes = null,
            FormattedSize = string.Empty,
            LastModified = dir.LastWriteTime,
            Type = "directory",
            IconKey = "folder"
        };
    }

    private static FileEntryDto CreateFileDto(FileInfo file)
    {
        var ext = file.Extension.ToLowerInvariant();
        var type = GetFileType(ext);

        return new FileEntryDto
        {
            Name = file.Name,
            FullPath = file.FullName,
            IsDirectory = false,
            SizeBytes = file.Length,
            FormattedSize = FormatSize(file.Length),
            LastModified = file.LastWriteTime,
            Type = type,
            IconKey = GetIconKey(type, ext)
        };
    }

    private static string GetFileType(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return "file";

        return ext switch
        {
            ".txt" or ".md" or ".log" => "text",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "image",
            ".mp3" or ".wav" or ".flac" or ".aac" => "audio",
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "video",
            ".exe" or ".bat" or ".cmd" => "binary",
            _ => "file"
        };
    }

    private static string GetIconKey(string type, string ext)
    {
        return type switch
        {
            "text" => "file-text",
            "image" => "file-image",
            "audio" => "file-audio",
            "video" => "file-video",
            "binary" => "file-binary",
            _ => "file"
        };
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        if (bytes < 0) return "0 Б";

        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(sourceDir);
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: false);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}

