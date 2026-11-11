using System;
using System.IO;
using Sqlbi.Bravo.Infrastructure.Helpers;

namespace Sqlbi.Bravo.Infrastructure;

/// <summary>
/// The folder of the application data, computed without <see cref="AppEnvironment"/>: the crash path needs it when the
/// initialization of AppEnvironment is the crash being reported.
/// </summary>
internal static class ApplicationDataFolder
{
    public static string GetPath()
    {
        var root = DesktopBridgeHelper.IsRunningAsMsixPackage() ? Environment.SpecialFolder.UserProfile : Environment.SpecialFolder.LocalApplicationData;

        return Path.Combine(Environment.GetFolderPath(root, Environment.SpecialFolderOption.DoNotVerify), AppEnvironment.ApplicationName);
    }
}
