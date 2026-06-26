namespace TalentBridge.Applications.Application.Services;

public class KeywordMatchingStrategy : IResumeMatchingStrategy
{
    private static readonly char[] Separators = [' ', ',', '.', '\n', '\r', '\t', ';', '/', '|', '-', '_', '(', ')'];

    public Task<MatchResult> CalculateAsync(string resumeText, string jobDescription, string[] requiredSkills)
    {
        if (requiredSkills.Length == 0)
            return Task.FromResult(new MatchResult(0, [], []));

        var resumeWords = resumeText
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var normalizedSkills = requiredSkills
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToArray();

        var matched = normalizedSkills.Where(s => resumeWords.Contains(s)).ToArray();
        var missing = normalizedSkills.Except(matched).ToArray();

        var percentage = normalizedSkills.Length > 0
            ? Math.Round((decimal)matched.Length / normalizedSkills.Length * 100, 2)
            : 0m;

        return Task.FromResult(new MatchResult(percentage, matched, missing));
    }
}
