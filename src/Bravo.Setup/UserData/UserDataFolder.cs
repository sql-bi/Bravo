using System;
using System.IO;

namespace Bravo.Setup.UserData
{
    /// <summary>
    /// The folder where the application keeps the data of a user: settings, the profile of the webview, logs. Its
    /// name is fixed, and the caller says which folder of local application data it is in.
    /// </summary>
    internal static class UserDataFolder
    {
        /// <summary>
        /// The name of the data folder of the installed application. It is here and not in the package, so that no
        /// data of an action makes the library remove another folder.
        /// </summary>
        public const string Name = "Bravo";

        /// <summary>
        /// Removes the data folder of the application with everything in it, and nothing else of the folder that it
        /// is in. A folder that is not there is not an error.
        /// </summary>
        /// <param name="localApplicationData">The full path of the folder of local application data.</param>
        public static void Remove(string localApplicationData)
        {
            // A relative path would name a folder of the current one.
            if (!FullPath.IsFullyQualified(localApplicationData))
            {
                throw new ArgumentException($"'{localApplicationData}' is not a full path.", nameof(localApplicationData));
            }

            var folder = Path.Combine(localApplicationData, Name);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
