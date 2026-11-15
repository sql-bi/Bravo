namespace Bravo.Setup.UserData;

/// <summary>
/// The folder where the application keeps the data of a user: settings, the profile of the webview, logs.
/// Its name is fixed, and the caller says which folder of local application data it is in.
/// </summary>
internal interface IUserDataFolder
{
    /// <summary>
    /// Removes the data folder of the application with everything in it, and nothing else of the folder
    /// that it is in. A folder that is not there is not an error.
    /// </summary>
    void Remove(string localApplicationData);
}
