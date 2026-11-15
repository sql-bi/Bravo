namespace Bravo.Setup.ExternalTools;

/// <summary>
/// The registration of the application as an external tool of Power BI Desktop: a file that names the
/// executable to start, in the folder that Power BI Desktop reads when it starts. The caller says where
/// the file is.
/// </summary>
internal interface IExternalToolRegistration
{
    /// <summary>
    /// Writes the registration file, with its folder when it is missing. A file that is already there is
    /// replaced.
    /// </summary>
    void Register(string toolFile, string executable);

    /// <summary>
    /// Removes the registration file. A file that is not there is not an error, and the folder stays: other
    /// tools register in it.
    /// </summary>
    void Unregister(string toolFile);
}
