using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Infrastructure.Telemetry;

internal static class TestRedactors
{
    public const string ProfilePath = @"C:\Users\jane.doe";

    public static SensitiveTextRedactor Create() => new(new UserProfilePathRedactor([new(ProfilePath, "%USERPROFILE%")]));
}
