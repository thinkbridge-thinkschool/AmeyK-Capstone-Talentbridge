using TalentBridge.Companies.Domain.Events;
using TalentBridge.Shared.Common;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Companies.Domain.Entities;

public class Company : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public bool IsApproved { get; private set; }
    public Guid? ApprovedByAdminId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid OwnerId { get; private set; }

    private Company() { }

    public static Result<Company> Create(string name, string description, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result<Company>.Failure("Name is required.");
        if (name.Length > 200) return Result<Company>.Failure("Name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(description)) return Result<Company>.Failure("Description is required.");

        var company = new Company
        {
            Name = name,
            Description = description,
            OwnerId = ownerId,
            IsApproved = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        company.RaiseDomainEvent(new CompanyCreatedEvent(company.Id, name, ownerId, DateTime.UtcNow));
        return Result<Company>.Success(company);
    }

    public Result Approve(Guid adminId)
    {
        if (IsApproved) return Result.Failure("Company is already approved.");
        IsApproved = true;
        ApprovedByAdminId = adminId;
        RaiseDomainEvent(new CompanyApprovedEvent(Id, adminId, DateTime.UtcNow));
        return Result.Success();
    }

    public Result UpdateProfile(string name, string description, string? website)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure("Name is required.");
        if (name.Length > 200) return Result.Failure("Name cannot exceed 200 characters.");
        Name = name;
        Description = description;
        Website = website;
        return Result.Success();
    }
}
