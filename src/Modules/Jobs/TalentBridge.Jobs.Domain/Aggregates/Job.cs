using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Events;
using TalentBridge.Shared.Common;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Jobs.Domain.Aggregates;

public class Job : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid PostedByHRId { get; private set; }
    public decimal SalaryMin { get; private set; }
    public decimal SalaryMax { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public JobStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    private Job() { }

    public static Result<Job> Create(string title, string description, Guid companyId, Guid postedByHRId, decimal salaryMin, decimal salaryMax, string location)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result<Job>.Failure("Title cannot be empty.");
        if (title.Length > 200) return Result<Job>.Failure("Title cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(description)) return Result<Job>.Failure("Description cannot be empty.");
        if (salaryMin <= 0) return Result<Job>.Failure("SalaryMin must be greater than 0.");
        if (salaryMax < salaryMin) return Result<Job>.Failure("SalaryMax must be >= SalaryMin.");
        if (string.IsNullOrWhiteSpace(location)) return Result<Job>.Failure("Location cannot be empty.");

        var job = new Job
        {
            Title = title,
            Description = description,
            CompanyId = companyId,
            PostedByHRId = postedByHRId,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Location = location,
            Status = JobStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        job.RaiseDomainEvent(new JobCreatedEvent(job.Id, companyId, title));
        return Result<Job>.Success(job);
    }

    public Result Publish()
    {
        if (Status != JobStatus.Draft) return Result.Failure("Only Draft jobs can be published.");
        Status = JobStatus.Active;
        PublishedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new JobPublishedEvent(Id, CompanyId, Title));
        return Result.Success();
    }

    public Result Close()
    {
        if (Status != JobStatus.Active) return Result.Failure("Only Active jobs can be closed.");
        Status = JobStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new JobClosedEvent(Id, DateTime.UtcNow));
        return Result.Success();
    }

    public bool IsAcceptingApplications() => Status == JobStatus.Active && ExpiresAtUtc > DateTime.UtcNow;

    public Result Update(string title, string description, string location, decimal salaryMin, decimal salaryMax)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result.Failure("Title cannot be empty.");
        if (title.Length > 200) return Result.Failure("Title cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(description)) return Result.Failure("Description cannot be empty.");
        if (salaryMin <= 0) return Result.Failure("SalaryMin must be greater than 0.");
        if (salaryMax < salaryMin) return Result.Failure("SalaryMax must be >= SalaryMin.");
        if (string.IsNullOrWhiteSpace(location)) return Result.Failure("Location cannot be empty.");

        Title = title;
        Description = description;
        Location = location;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        return Result.Success();
    }
}
