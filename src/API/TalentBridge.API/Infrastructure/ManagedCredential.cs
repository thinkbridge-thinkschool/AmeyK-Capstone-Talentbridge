extern alias AzureIdentity;

using Azure.Core;

namespace TalentBridge.API.Infrastructure;

// Azure.Core 1.55 and Azure.Identity 1.14 both export credential types in the same
// namespace, causing CS0433. Isolate the instantiation here with extern alias so
// Program.cs stays clean and the rest of the codebase is unaffected.
internal static class ManagedCredential
{
    internal static TokenCredential Create() =>
        new AzureIdentity::Azure.Identity.DefaultAzureCredential();
}
