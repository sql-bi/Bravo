namespace Bravo.Core.Updates;

/// <summary>
/// How the copy that runs came to its folder, which tells which file of a release is its update.
/// </summary>
public enum Installation
{
    /// <summary>
    /// No package installed the folder: an archive was extracted there, or a build wrote it.
    /// </summary>
    None,

    PerMachine,

    PerUser,
}
