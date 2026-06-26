namespace TalentBridge.Applications.Application.Services;

public record MatchResult(decimal Percentage, string[] MatchedSkills, string[] MissingSkills);

public interface IResumeMatchingStrategy
{
    Task<MatchResult> CalculateAsync(string resumeText, string jobDescription, string[] requiredSkills);
}
