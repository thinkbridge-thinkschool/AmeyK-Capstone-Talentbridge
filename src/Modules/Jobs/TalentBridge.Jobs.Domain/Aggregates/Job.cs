using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Events;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Jobs.Domain.Aggregates;

public class Job : AggregateRoot
{
    private readonly List<string> _requiredSkills = new();

    public Guid CompanyId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public decimal SalaryMin { get; private set; }
    public decimal SalaryMax { get; private set; }
    public JobStatus Status { get; private set; }
    public JobType Type { get; private set; }
    public DateTime? ClosingDate { get; private set; }
    public IReadOnlyList<string> RequiredSkills => _requiredSkills.AsReadOnly();

    private Job() { }

    public static Job Create(
        Guid companyId,
        string title,
        string description,
        string location,
        decimal salaryMin,
        decimal salaryMax,
        JobType type,
        List<string> skills)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (salaryMin < 0)
            throw new ArgumentException("SalaryMin must be >= 0.", nameof(salaryMin));

        if (salaryMax <= salaryMin)
            throw new ArgumentException("SalaryMax must be > SalaryMin.", nameof(salaryMax));

        if (skills == null || skills.Count == 0)
            throw new ArgumentException("Skills list cannot be empty.", nameof(skills));

        var job = new Job
        {
            CompanyId = companyId,
            Title = title,
            Description = description,
            Location = location,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Type = type,
            Status = JobStatus.Draft
        };

        job._requiredSkills.AddRange(skills);
        job.AddDomainEvent(new JobCreatedEvent(job.Id, companyId, title));

        return job;
    }

    public void Publish()
    {
        if (Status != JobStatus.Draft)
            throw new InvalidOperationException("Only Draft jobs can be published.");

        Status = JobStatus.Active;
        AddDomainEvent(new JobPublishedEvent(Id, CompanyId, Title, Location, [.. _requiredSkills]));
    }

    public void Close()
    {
        if (Status != JobStatus.Active)
            throw new InvalidOperationException("Only Active jobs can be closed.");

        Status = JobStatus.Closed;
        AddDomainEvent(new JobClosedEvent(Id, DateTime.UtcNow));
    }

    public void Update(string title, string description, string location)
    {
        if (Status != JobStatus.Draft)
            throw new InvalidOperationException("Only Draft jobs can be updated.");

        Title = title;
        Description = description;
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }
}
