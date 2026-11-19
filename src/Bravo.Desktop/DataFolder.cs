using System;
using Bravo.Desktop.Development;

namespace Bravo.Desktop;

/// <summary>
/// The folder where the application keeps the data of the user: the settings and the profile of the webview.
/// Chosen once at the start and passed to what needs it.
/// </summary>
internal sealed record DataFolder
{
    public DataFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Path = path;
    }

    public string Path { get; }

    /// <summary>
    /// The folder of a build: the one that the command line of a development build names, otherwise the folder of
    /// the build under the local application data of the user.
    /// </summary>
    public static DataFolder For(BuildIdentity identity, DevelopmentOptions development)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(development);

        if (development.DataFolderPath is not null)
        {
            return new DataFolder(development.DataFolderPath);
        }

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return new DataFolder(System.IO.Path.Combine(localData, identity.DataFolderName));
    }
}
