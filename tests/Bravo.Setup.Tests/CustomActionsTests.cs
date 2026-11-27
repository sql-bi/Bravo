using System.Globalization;
using Xunit;

namespace Bravo.Setup.Tests
{
    /// <summary>
    /// Windows Installer formats a record as the log gets it: no installation is needed for that.
    /// </summary>
    public sealed class CustomActionsTests
    {
        [Theory]
        [InlineData(@"Access to the path 'C:\[ProgramFilesFolder]\bravo.pbitool.json' is denied.")]
        [InlineData(@"[1] [\[] [~] [%TEMP] {{[2]}}")]
        public void CreateLogRecord_AMessageWithBrackets_IsWrittenAsItIs(string message)
        {
            using (var record = CustomActions.CreateLogRecord(message))
            {
                Assert.Equal(message, record.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
