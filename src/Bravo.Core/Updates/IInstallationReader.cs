namespace Bravo.Core.Updates;

public interface IInstallationReader
{
    /// <summary>
    /// Reads how the copy that runs was installed. A record of the installation that cannot be read, or that
    /// names another folder, is no installation.
    /// </summary>
    Installation Read();

    /// <summary>
    /// Reads whether the installation of the copy that runs chose to send telemetry: what the settings of a user
    /// who has none start from. Null when the copy was not installed, or its installation recorded no choice
    /// that can be read.
    /// </summary>
    bool? ReadTelemetryEnabled();
}
