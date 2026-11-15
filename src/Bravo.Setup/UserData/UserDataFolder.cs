using System;
using System.IO;

namespace Bravo.Setup.UserData;

internal sealed class UserDataFolder : IUserDataFolder
{
    public void Remove(string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
