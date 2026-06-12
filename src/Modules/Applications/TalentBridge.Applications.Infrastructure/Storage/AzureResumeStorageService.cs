using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Infrastructure.Storage;

public class AzureResumeStorageService : IResumeStorageService
{
    private const string ContainerName = "resumes-talentbridge-amey";
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".doc", ".docx"];

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureResumeStorageService> _logger;

    public AzureResumeStorageService(BlobServiceClient blobServiceClient, ILogger<AzureResumeStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadResumeAsync(Guid candidateId, Stream file, string fileName, string contentType, CancellationToken ct)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Only .pdf, .doc, .docx are permitted.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File size {file.Length} exceeds maximum allowed size of {MaxFileSizeBytes} bytes.");

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobName = $"{candidateId}/{Guid.NewGuid()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        _logger.LogInformation("[Storage] Uploading resume for candidate {CandidateId}, size: {Size} bytes", candidateId, file.Length);

        await blobClient.UploadAsync(file, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadResumeAsync(string blobUrl, CancellationToken ct)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        var response = await blobClient.DownloadAsync(ct);
        return response.Value.Content;
    }

    public async Task DeleteResumeAsync(string blobUrl, CancellationToken ct)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
