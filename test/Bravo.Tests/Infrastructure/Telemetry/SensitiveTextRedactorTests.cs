using Xunit;

namespace Bravo.Tests.Infrastructure.Telemetry;

public class SensitiveTextRedactorTests
{
    [Fact]
    public void Redact_TextWithASensitiveTagAndAProfilePath_RedactsBoth()
    {
        var text = $@"Access to '{TestRedactors.ProfilePath}\Bravo' denied for '<pii>john@contoso.com</pii>'";

        var redacted = TestRedactors.Create().Redact(text);

        Assert.Equal(@"Access to '%USERPROFILE%\Bravo' denied for '<pii>[REDACTED]</pii>'", redacted);
    }
}
