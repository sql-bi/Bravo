using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// Removes from a text the names that tell who the user is and which computer it is: the folder of the profile,
/// the name of the user and the names of the computer. Each becomes the variable of Windows that holds it.
/// </summary>
internal sealed class PersonalNames
{
    private readonly string _userProfile;
    private readonly string _userName;
    private readonly string[] _machineNames;

    /// <param name="userProfile">The folder of the profile of the user.</param>
    /// <param name="userName">The name with which the user signed in.</param>
    /// <param name="machineNames">The names of the computer, for Windows and on the network.</param>
    public PersonalNames(string userProfile, string userName, params string[] machineNames)
    {
        _userProfile = userProfile.TrimEnd('\\');
        _userName = userName;

        // The longer first: the name of Windows can be the first 15 characters of the one on the network.
        _machineNames =
        [
            .. machineNames.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(name => name.Length),
        ];
    }

    /// <summary>
    /// The names of the user who runs the process, on this computer.
    /// </summary>
    public static PersonalNames OfCurrentUser()
    {
        return new PersonalNames(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.UserName,
            Environment.MachineName,
            GetHostName());
    }

    public string RemoveFrom(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // The folder first: it has the name of the user in it.
        text = ReplaceWord(text, _userProfile, "%USERPROFILE%");
        text = ReplaceWord(text, _userName, "%USERNAME%");
        foreach (var machineName in _machineNames)
        {
            text = ReplaceWord(text, machineName, "%COMPUTERNAME%");
        }

        return text;
    }

    // The name of the computer on the network, which can be longer than the one of Windows.
    private static string GetHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch (SocketException)
        {
            return string.Empty;
        }
    }

    // Only as a whole word: a name can also be a part of another word, and a folder the start of another one.
    private static string ReplaceWord(string text, string word, string replacement)
    {
        if (word.Length == 0)
        {
            return text;
        }

        var pattern = $@"(?<!\w){Regex.Escape(word)}(?!\w)";
        return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
