using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Applications.Domain.Enums;
using TalentBridge.Applications.Domain.Events;

namespace TalentBridge.Applications.Domain.Tests;

public class JobApplicationTests
{
    private static JobApplication CreateValid() =>
        JobApplication.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "I am a great candidate",
            "https://storage/resume.pdf");

    [Fact]
    public void Apply_WithValidData_ShouldCreateInSubmittedStatus()
    {
        var app = CreateValid();
        Assert.Equal(ApplicationStatus.Submitted, app.Status);
        Assert.NotEqual(Guid.Empty, app.Id);
    }

    [Fact]
    public void Accept_WithoutReview_ShouldThrow()
    {
        var app = CreateValid();
        Assert.Throws<InvalidOperationException>(() => app.Accept());
    }

    [Fact]
    public void Reject_ShouldSetRejectionReason()
    {
        var app = CreateValid();
        app.Reject("Not enough experience");
        Assert.Equal(ApplicationStatus.Rejected, app.Status);
        Assert.Equal("Not enough experience", app.RejectionReason);
    }

    [Fact]
    public void Create_ShouldRaiseApplicationSubmittedEvent()
    {
        var app = CreateValid();
        var evt = app.DomainEvents.OfType<ApplicationSubmittedEvent>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(app.Id, evt.ApplicationId);
    }
}
