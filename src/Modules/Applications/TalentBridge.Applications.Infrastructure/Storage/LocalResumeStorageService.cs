using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Infrastructure.Storage;

public class LocalResumeStorageService : IResumeStorageService
{
    private readonly ILogger<LocalResumeStorageService> _logger;

    public LocalResumeStorageService(ILogger<LocalResumeStorageService> logger)
    {
        _logger = logger;
    }

    private static string ResolvePath(string relative)
    {
        // When running with `dotnet run`, cwd is the API project folder
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        return Path.Combine(wwwroot, relative);
    }

    public async Task<string> UploadResumeAsync(
        Guid candidateId, Stream file, string fileName, string contentType, CancellationToken ct)
    {
        var safeFile = Path.GetFileName(fileName);
        var uniqueName = $"{Guid.NewGuid()}-{safeFile}";
        var relativeDir = Path.Combine("uploads", "resumes", candidateId.ToString());
        var absoluteDir = ResolvePath(relativeDir);

        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, uniqueName);
        await using var fs = File.Create(absolutePath);
        await file.CopyToAsync(fs, ct);

        var url = $"/uploads/resumes/{candidateId}/{uniqueName}";
        _logger.LogInformation("[Storage:Local] Resume saved to {Path}, URL: {Url}", absolutePath, url);
        return url;
    }

    public Task<Stream> DownloadResumeAsync(string blobUrl, CancellationToken ct)
    {
        var localPath = ResolvePath(blobUrl.TrimStart('/'));
        Stream stream = File.OpenRead(localPath);
        return Task.FromResult(stream);
    }

    public Task DeleteResumeAsync(string blobUrl, CancellationToken ct)
    {
        var localPath = ResolvePath(blobUrl.TrimStart('/'));
        if (File.Exists(localPath))
            File.Delete(localPath);
        return Task.CompletedTask;
    }
}
