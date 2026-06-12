using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Applications.Domain.Events;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Applications.Domain.Aggregates;

public class JobApplication : AggregateRoot
{
    public Guid JobId { get; private set; }
    public Guid CandidateId { get; private set; }
    public string CoverLetter { get; private set; } = string.Empty;
    public string ResumeUrl { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public DateTime AppliedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private JobApplication() { }

    public static JobApplication Create(Guid jobId, Guid candidateId, string coverLetter, string resumeUrl)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("JobId cannot be empty.", nameof(jobId));
        if (candidateId == Guid.Empty) throw new ArgumentException("CandidateId cannot be empty.", nameof(candidateId));
        if (string.IsNullOrWhiteSpace(coverLetter)) throw new ArgumentException("CoverLetter cannot be empty.", nameof(coverLetter));
        if (string.IsNullOrWhiteSpace(resumeUrl)) throw new ArgumentException("ResumeUrl cannot be empty.", nameof(resumeUrl));

        var application = new JobApplication
        {
            JobId = jobId,
            CandidateId = candidateId,
            CoverLetter = coverLetter,
            ResumeUrl = resumeUrl,
            Status = ApplicationStatus.Submitted,
            AppliedAt = DateTime.UtcNow
        };

        application.AddDomainEvent(new ApplicationSubmittedEvent(application.Id, jobId, candidateId));

        return application;
    }

    public void MoveToReview()
    {
        if (Status != ApplicationStatus.Submitted)
            throw new InvalidOperationException("Only Submitted applications can be moved to review.");

        Status = ApplicationStatus.UnderReview;
        AddDomainEvent(new ApplicationStatusChangedEvent(Id, CandidateId, Status));
    }

    public void Accept()
    {
        if (Status != ApplicationStatus.UnderReview)
            throw new InvalidOperationException("Only applications UnderReview can be accepted.");

        Status = ApplicationStatus.Accepted;
        AddDomainEvent(new ApplicationAcceptedEvent(Id, CandidateId, JobId));
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason cannot be empty.", nameof(reason));

        Status = ApplicationStatus.Rejected;
        RejectionReason = reason;
        AddDomainEvent(new ApplicationRejectedEvent(Id, CandidateId, reason));
    }
}
