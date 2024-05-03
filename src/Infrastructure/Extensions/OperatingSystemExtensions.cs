namespace Sqlbi.Bravo.Infrastructure.Extensions;

internal static class OperatingSystemExtensions
{
    public static bool IsWindows7OrLower(this OperatingSystem os)
    {
        var major = (double)os.Version.Major;
        var minor = (double)os.Version.Minor;

        return (major + minor / 10.0) <= 6.1;
    }
}
