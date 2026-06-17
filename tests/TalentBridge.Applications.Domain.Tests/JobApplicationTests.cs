using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Applications.Domain.Events;

namespace TalentBridge.Applications.Domain.Tests;

public class JobApplicationTests
{
    private static JobApplication CreateValid()
    {
        var result = JobApplication.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "I am a great candidate",
            "https://storage/resume.pdf");
        return result.Value!;
    }

    [Fact]
    public void Apply_WithValidData_ShouldCreateInSubmittedStatus()
    {
        var result = JobApplication.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "I am a great candidate",
            "https://storage/resume.pdf");

        Assert.True(result.IsSuccess);
        var app = result.Value!;
        Assert.Equal(ApplicationStatus.Submitted, app.Status);
        Assert.NotEqual(Guid.Empty, app.Id);
    }

    [Fact]
    public void Accept_WithoutShortlist_ShouldFail()
    {
        var app = CreateValid();
        var hrId = Guid.NewGuid();
        var result = app.Accept(hrId);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Reject_ShouldSetReviewNotes()
    {
        var app = CreateValid();
        var hrId = Guid.NewGuid();
        var result = app.Reject(hrId, "Not enough experience");
        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.Rejected, app.Status);
        Assert.Equal("Not enough experience", app.ReviewNotes);
    }

    [Fact]
    public void Create_ShouldRaiseApplicationSubmittedEvent()
    {
        var app = CreateValid();
        var evt = app.DomainEvents.OfType<ApplicationSubmittedEvent>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(app.Id, evt.ApplicationId);
    }

    [Fact]
    public void StartReview_FromSubmitted_ShouldSucceed()
    {
        var app = CreateValid();
        var result = app.StartReview(Guid.NewGuid());
        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.UnderReview, app.Status);
    }

    [Fact]
    public void Shortlist_FromUnderReview_ShouldSucceed()
    {
        var app = CreateValid();
        var hrId = Guid.NewGuid();
        app.StartReview(hrId);
        var result = app.Shortlist(hrId);
        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.Shortlisted, app.Status);
    }

    [Fact]
    public void Accept_FromShortlisted_ShouldSucceed()
    {
        var app = CreateValid();
        var hrId = Guid.NewGuid();
        app.StartReview(hrId);
        app.Shortlist(hrId);
        var result = app.Accept(hrId);
        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.Accepted, app.Status);
    }

    [Fact]
    public void Withdraw_FromAccepted_ShouldFail()
    {
        var app = CreateValid();
        var hrId = Guid.NewGuid();
        app.StartReview(hrId);
        app.Shortlist(hrId);
        app.Accept(hrId);
        var result = app.Withdraw();
        Assert.True(result.IsFailure);
    }
}
