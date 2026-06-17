using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Events;

namespace TalentBridge.Jobs.Domain.Tests;

public class JobTests
{
    private static Job CreateValidJob(string title = "Software Engineer")
    {
        var result = Job.Create(
            title,
            "Build great software",
            Guid.NewGuid(),
            Guid.NewGuid(),
            50000m,
            100000m,
            "Remote");
        return result.Value!;
    }

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Job.Create("Software Engineer", "Build great software", Guid.NewGuid(), Guid.NewGuid(), 50000m, 100000m, "Remote");

        Assert.True(result.IsSuccess);
        var job = result.Value!;
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("Software Engineer", job.Title);
        Assert.Equal(JobStatus.Draft, job.Status);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldFail()
    {
        var result = Job.Create("", "Desc", Guid.NewGuid(), Guid.NewGuid(), 50000m, 100000m, "Remote");
        Assert.True(result.IsFailure);
        Assert.Contains("Title", result.Error);
    }

    [Fact]
    public void Create_WithInvalidSalaryRange_ShouldFail()
    {
        var result = Job.Create("Dev", "Desc", Guid.NewGuid(), Guid.NewGuid(), 100000m, 50000m, "NYC");
        Assert.True(result.IsFailure);
        Assert.Contains("SalaryMax", result.Error);
    }

    [Fact]
    public void Publish_FromDraft_ShouldChangeStatusToActive()
    {
        var job = CreateValidJob();
        var result = job.Publish();
        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.Active, job.Status);
    }

    [Fact]
    public void Publish_FromActive_ShouldFail()
    {
        var job = CreateValidJob();
        job.Publish();
        var result = job.Publish();
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Close_FromActive_ShouldSucceed()
    {
        var job = CreateValidJob();
        job.Publish();
        var result = job.Close();
        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
    }

    [Fact]
    public void Close_FromDraft_ShouldFail()
    {
        var job = CreateValidJob();
        var result = job.Close();
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Publish_ShouldRaiseJobPublishedEvent()
    {
        var job = CreateValidJob();
        job.Publish();

        var evt = job.DomainEvents.OfType<JobPublishedEvent>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(job.Id, evt.JobId);
    }
}
