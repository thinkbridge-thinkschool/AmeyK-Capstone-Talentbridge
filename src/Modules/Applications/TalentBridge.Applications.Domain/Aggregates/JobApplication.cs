using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Applications.Domain.Events;
using TalentBridge.Shared.Common;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Aggregates;

public class JobApplication : AggregateRoot
{
    public Guid CandidateId { get; private set; }
    public Guid JobId { get; private set; }
    public string CoverLetter { get; private set; } = string.Empty;
    public string ResumeUrl { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime LastUpdatedAtUtc { get; private set; }
    public Guid? ReviewedByHRId { get; private set; }
    public string? ReviewNotes { get; private set; }

    private JobApplication() { }

    public static Result<JobApplication> Create(Guid candidateId, Guid jobId, string coverLetter, string resumeUrl)
    {
        if (string.IsNullOrWhiteSpace(coverLetter)) return Result<JobApplication>.Failure("Cover letter is required.");
        if (string.IsNullOrWhiteSpace(resumeUrl)) return Result<JobApplication>.Failure("Resume URL is required.");

        var application = new JobApplication
        {
            CandidateId = candidateId,
            JobId = jobId,
            CoverLetter = coverLetter,
            ResumeUrl = resumeUrl,
            Status = ApplicationStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        application.RaiseDomainEvent(new ApplicationSubmittedEvent(
            application.Id, candidateId, jobId, coverLetter, resumeUrl, DateTime.UtcNow));
        return Result<JobApplication>.Success(application);
    }

    public Result StartReview(Guid hrId)
    {
        if (Status != ApplicationStatus.Submitted) return Result.Failure("Only Submitted applications can move to review.");
        var old = Status;
        Status = ApplicationStatus.UnderReview;
        ReviewedByHRId = hrId;
        LastUpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ApplicationStatusChangedEvent(Id, old, Status, DateTime.UtcNow));
        return Result.Success();
    }

    public Result Shortlist(Guid hrId)
    {
        if (Status != ApplicationStatus.UnderReview) return Result.Failure("Only UnderReview applications can be shortlisted.");
        var old = Status;
        Status = ApplicationStatus.Shortlisted;
        ReviewedByHRId = hrId;
        LastUpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ApplicationStatusChangedEvent(Id, old, Status, DateTime.UtcNow));
        return Result.Success();
    }

    public Result Accept(Guid hrId)
    {
        if (Status != ApplicationStatus.Shortlisted) return Result.Failure("Only Shortlisted applications can be accepted.");
        Status = ApplicationStatus.Accepted;
        ReviewedByHRId = hrId;
        LastUpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ApplicationAcceptedEvent(Id, CandidateId, JobId, DateTime.UtcNow));
        return Result.Success();
    }

    public Result Reject(Guid hrId, string notes)
    {
        if (Status is ApplicationStatus.Accepted or ApplicationStatus.Withdrawn)
            return Result.Failure("Cannot reject an Accepted or Withdrawn application.");
        var old = Status;
        Status = ApplicationStatus.Rejected;
        ReviewedByHRId = hrId;
        ReviewNotes = notes;
        LastUpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ApplicationStatusChangedEvent(Id, old, Status, DateTime.UtcNow));
        return Result.Success();
    }

    public Result Withdraw()
    {
        if (Status is ApplicationStatus.Accepted or ApplicationStatus.Rejected)
            return Result.Failure("Cannot withdraw an Accepted or Rejected application.");
        Status = ApplicationStatus.Withdrawn;
        LastUpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ApplicationWithdrawnEvent(Id, DateTime.UtcNow));
        return Result.Success();
    }
}
