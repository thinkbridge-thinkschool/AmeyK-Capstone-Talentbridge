using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Events;

namespace TalentBridge.Jobs.Domain.Tests;

public class JobTests
{
    private static Job CreateValidJob(string title = "Software Engineer") =>
        Job.Create(
            Guid.NewGuid(),
            title,
            "Build great software",
            "Remote",
            50000m,
            100000m,
            JobType.FullTime,
            ["C#", "Azure"]);

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var job = CreateValidJob();

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("Software Engineer", job.Title);
        Assert.Equal(JobStatus.Draft, job.Status);
        Assert.Equal(2, job.RequiredSkills.Count);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValidJob(""));
        Assert.Contains("Title", ex.Message);
    }

    [Fact]
    public void Create_WithInvalidSalaryRange_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Job.Create(Guid.NewGuid(), "Dev", "Desc", "NYC", 100000m, 50000m, JobType.FullTime, ["C#"]));
        Assert.Contains("SalaryMax", ex.Message);
    }

    [Fact]
    public void Publish_FromDraft_ShouldChangeStatusToActive()
    {
        var job = CreateValidJob();
        job.Publish();
        Assert.Equal(JobStatus.Active, job.Status);
    }

    [Fact]
    public void Publish_FromActive_ShouldThrow()
    {
        var job = CreateValidJob();
        job.Publish();
        Assert.Throws<InvalidOperationException>(() => job.Publish());
    }

    [Fact]
    public void Close_FromActive_ShouldSucceed()
    {
        var job = CreateValidJob();
        job.Publish();
        job.Close();
        Assert.Equal(JobStatus.Closed, job.Status);
    }

    [Fact]
    public void Close_FromDraft_ShouldThrow()
    {
        var job = CreateValidJob();
        Assert.Throws<InvalidOperationException>(() => job.Close());
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
