namespace Bravo.Setup.UserData;

/// <summary>
/// The folder where the application keeps the data of a user: settings, the profile of the webview, logs.
/// The caller says where it is.
/// </summary>
internal interface IUserDataFolder
{
    /// <summary>
    /// Removes the folder with everything in it. A folder that is not there is not an error.
    /// </summary>
    void Remove(string folder);
}
