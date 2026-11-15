namespace Bravo.Setup.CommandLine;

/// <summary>
/// Removes the folder where the application keeps the data of a user.
/// </summary>
/// <param name="LocalApplicationData">
/// The full path of the folder of the local application data of the user: the data folder is in it.
/// </param>
internal sealed record RemoveUserDataCommand(string LocalApplicationData) : SetupCommand;
