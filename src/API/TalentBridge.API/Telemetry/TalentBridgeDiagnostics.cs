using System.Diagnostics;

namespace TalentBridge.API.Telemetry;

public static class TalentBridgeDiagnostics
{
    public const string SourceName = "TalentBridge.Application";

    public static readonly ActivitySource Source =
        new(SourceName, "1.0.0");
}
