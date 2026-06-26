using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
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

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            // Private container — resumes accessed via time-limited SAS URLs
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var blobName = $"{candidateId}/{Guid.NewGuid()}-{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation("[Storage] Uploading resume for candidate {CandidateId}, size: {Size} bytes", candidateId, file.Length);
            await blobClient.UploadAsync(file, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

            return blobClient.Uri.ToString();
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "[Storage] Azure Blob upload failed: Status={Status} ErrorCode={ErrorCode}", ex.Status, ex.ErrorCode);
            throw new InvalidOperationException($"Resume upload failed. Please try again. ({ex.ErrorCode})");
        }
    }

    public async Task<Stream> DownloadResumeAsync(string blobUrl, CancellationToken ct)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        var response = await blobClient.DownloadAsync(ct);
        return response.Value.Content;
    }

    public async Task DeleteResumeAsync(string blobUrl, CancellationToken ct)
    {
        if (!Uri.IsWellFormedUriString(blobUrl, UriKind.Absolute)) return;
        var blobClient = new BlobClient(new Uri(blobUrl));
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<ResumeAccessResult?> GenerateSasUrlAsync(string blobUrl, TimeSpan expiry, CancellationToken ct)
    {
        // Old local-path URLs (e.g. /uploads/resumes/...) are not in blob storage
        if (!Uri.IsWellFormedUriString(blobUrl, UriKind.Absolute))
            return null;

        try
        {
            var blobUri = new Uri(blobUrl);
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobName = Uri.UnescapeDataString(blobUri.AbsolutePath).TrimStart('/').Replace($"{ContainerName}/", "");
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(ct))
                return null;

            var fileName = Path.GetFileName(blobName);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var fileType = ext == ".pdf" ? "pdf" : "docx";

            var expiresOn = DateTimeOffset.UtcNow.Add(expiry);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri;
            if (blobClient.CanGenerateSasUri)
            {
                // Connection string / account key available — sign SAS directly (no Azure AD needed)
                sasUri = blobClient.GenerateSasUri(sasBuilder);
            }
            else
            {
                // Managed Identity / Azure AD — use User Delegation Key to sign SAS
                var delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    expiresOn,
                    ct);
                var sasToken = sasBuilder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName);
                sasUri = new Uri($"{blobClient.Uri}?{sasToken}");
            }

            _logger.LogInformation("[Storage] Generated SAS URL for blob {BlobName} (canGenerate={CanGenerate})", blobName, blobClient.CanGenerateSasUri);
            return new ResumeAccessResult(sasUri.ToString(), fileName, fileType);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "[Storage] Failed to generate SAS URL: Status={Status} ErrorCode={ErrorCode}", ex.Status, ex.ErrorCode);
            return null;
        }
    }
}
