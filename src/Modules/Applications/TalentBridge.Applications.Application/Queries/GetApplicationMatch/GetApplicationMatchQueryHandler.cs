using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Application.Services;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.Applications.Application.Queries.GetApplicationMatch;

public class GetApplicationMatchQueryHandler : IRequestHandler<GetApplicationMatchQuery, MatchResult?>
{
    private readonly IApplicationsDbContext _db;
    private readonly IResumeStorageService _storage;
    private readonly IJobLookupService _jobLookup;
    private readonly IResumeMatchingStrategy _matcher;

    public GetApplicationMatchQueryHandler(
        IApplicationsDbContext db,
        IResumeStorageService storage,
        IJobLookupService jobLookup,
        IResumeMatchingStrategy matcher)
    {
        _db = db;
        _storage = storage;
        _jobLookup = jobLookup;
        _matcher = matcher;
    }

    public async Task<MatchResult?> Handle(GetApplicationMatchQuery request, CancellationToken cancellationToken)
    {
        var application = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null) return null;

        // Return cached percentage if already calculated
        if (application.MatchPercentage.HasValue)
        {
            return new MatchResult(application.MatchPercentage.Value, [], []);
        }

        var job = await _jobLookup.GetByIdAsync(application.JobId, cancellationToken);
        if (job is null) return new MatchResult(0, [], []);

        // Extract keywords from job description as "required skills"
        var separators = new[] { ' ', ',', '.', '\n', '\r', '\t', ';', '/', '|', '-', '(', ')' };
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "are", "you", "our", "will", "have",
            "that", "this", "from", "your", "they", "been", "also", "must"
        };
        var jobSkills = job.Description
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant().Trim())
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .Distinct()
            .ToArray();

        // Download resume and read text
        var resumeText = string.Empty;
        try
        {
            await using var stream = await _storage.DownloadResumeAsync(application.ResumeUrl, cancellationToken);
            using var reader = new StreamReader(stream);
            resumeText = await reader.ReadToEndAsync(cancellationToken);
        }
        catch
        {
            // If download fails, use cover letter text as fallback
            resumeText = application.CoverLetter;
        }

        var result = await _matcher.CalculateAsync(resumeText, job.Description, jobSkills);

        // Persist for future calls
        application.SetMatchPercentage(result.Percentage);
        await _db.SaveChangesAsync(cancellationToken);

        return result;
    }
}
