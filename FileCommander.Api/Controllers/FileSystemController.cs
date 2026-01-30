using FileCommander.Api.Models;
using FileCommander.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileCommander.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemController : ControllerBase
{
    private readonly IFileSystemService _fileSystemService;

    public FileSystemController(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    [HttpGet("drives")]
    public ActionResult<IEnumerable<string>> GetDrives()
    {
        var drives = _fileSystemService.GetDrives();
        return Ok(drives);
    }

    [HttpGet("entries")]
    public ActionResult<IEnumerable<FileEntryDto>> GetEntries([FromQuery] string path)
    {
        try
        {
            var entries = _fileSystemService.GetEntries(path);
            return Ok(entries);
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound($"Directory '{path}' not found.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("copy")]
    public async Task<IActionResult> Copy([FromBody] FileOperationRequest request, CancellationToken cancellationToken)
    {
        await _fileSystemService.CopyAsync(request.SourcePaths, request.DestinationDirectory, cancellationToken);
        return NoContent();
    }

    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] FileOperationRequest request, CancellationToken cancellationToken)
    {
        await _fileSystemService.MoveAsync(request.SourcePaths, request.DestinationDirectory, cancellationToken);
        return NoContent();
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        await _fileSystemService.DeleteAsync(paths, cancellationToken);
        return NoContent();
    }
}

