namespace Bravo.Setup.CommandLine;

/// <summary>
/// Removes the folder where the application keeps the data of a user.
/// </summary>
/// <param name="Folder">The full path of the folder to remove, with everything in it.</param>
internal sealed record RemoveUserDataCommand(string Folder) : SetupCommand;
