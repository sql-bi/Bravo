using System;
using System.IO;

namespace Bravo.Setup.UserData;

internal sealed class UserDataFolder : IUserDataFolder
{
    /// <summary>
    /// The name of the data folder of the installed application. It is here and not in the package, so
    /// that no argument makes the program remove another folder.
    /// </summary>
    public const string Name = "Bravo";

    public void Remove(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrEmpty(localApplicationData);

        var folder = Path.Combine(localApplicationData, Name);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
