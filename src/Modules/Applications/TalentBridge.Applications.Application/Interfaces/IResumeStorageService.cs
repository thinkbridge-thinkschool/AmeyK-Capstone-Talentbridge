namespace TalentBridge.Applications.Application.Interfaces;

public interface IResumeStorageService
{
    Task<string> UploadResumeAsync(Guid candidateId, Stream file, string fileName, string contentType, CancellationToken ct);
    Task<Stream> DownloadResumeAsync(string blobUrl, CancellationToken ct);
    Task DeleteResumeAsync(string blobUrl, CancellationToken ct);
}
