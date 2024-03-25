namespace Sqlbi.Bravo.Models.AnalyzeModel;

public enum ExportVpaxMode
{
    Default = 0,
    Obfuscate = 1,
    ObfuscateIncremental = 2
}

internal static class ExportVpaxModeExtensions
{
    public static bool IsObfuscate(this ExportVpaxMode mode)
    {
        return mode == ExportVpaxMode.Obfuscate || mode == ExportVpaxMode.ObfuscateIncremental;
    }
}