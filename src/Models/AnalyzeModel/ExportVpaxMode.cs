namespace Sqlbi.Bravo.Models.AnalyzeModel;

public enum ExportVpaxMode
{
    Default = 0,

    Obfuscated = 1,

    // Incremental obfuscation is not yet supported in Bravo
    // ObfuscateIncremental = 2
}