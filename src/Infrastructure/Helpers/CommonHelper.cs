namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Configuration;
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Security.Policies;
    using Sqlbi.Bravo.Infrastructure.Windows.Interop;
    using Sqlbi.Bravo.Models;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    internal static class CommonHelper
    {
        public static string? ChangeUriScheme(string? uriString, string scheme, bool ignorePort = false)
        {
            if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            {
                var uriBuilder = new UriBuilder(uri)
                {
                    Scheme = scheme
                };

                if (ignorePort)
                {
                    uriBuilder.Port = -1;
                }
                
                return uriBuilder.Uri.AbsoluteUri;
            }

            return null;
        }

        public static string? ReadRegistryString(RegistryKey registryKey, string keyName, string valueName)
        {
            try
            {
                using var registrySubKey = registryKey.OpenSubKey(keyName, writable: false);

                if (registrySubKey is not null)
                {
                    var value = registrySubKey.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (value is not null)
                    {
                        var valueKind = registrySubKey.GetValueKind(valueName);
                        if (valueKind == RegistryValueKind.String)
                        {
                            var valueString = (string)value;
                            return valueString;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }

            return null;
        }

        public static User32.KeyState GetKeyState(Keys key)
        {
            var state = User32.KeyState.None;
            {
                var retval = User32.GetKeyState((int)key);

                if ((retval & 0x8000) == 0x8000)
                    state |= User32.KeyState.Down;

                if ((retval & 1) == 1)
                    state |= User32.KeyState.Toggled;
            }
            return state;
        }

        public static bool IsKeyDown(Keys key)
        {
            var state = GetKeyState(key);

            return state.HasFlag(User32.KeyState.Down);
        }

        public static bool AreDirectoryPathsEqual(string path1, string path2)
        {
            var normalizedPath1 = NormalizeDirectoryPath(path1);
            var normalizedPath2 = NormalizeDirectoryPath(path2);

            var equals =  normalizedPath1.EqualsI(normalizedPath2);
            return equals;
        }

        public static string NormalizeDirectoryPath(string path)
        {
            var normalizedPath = path;

            normalizedPath = normalizedPath.Trim();
            normalizedPath = Path.TrimEndingDirectorySeparator(normalizedPath);
            normalizedPath = new DirectoryInfo(normalizedPath).FullName;

            return normalizedPath;
        }

        public static string NormalizeUriString(string uriString)
        {
            var uri = new Uri(uriString, UriKind.Absolute);
            return uri.AbsoluteUri;
        }

        public async static Task<BravoUpdate> CheckForUpdateAsync(UpdateChannelType updateChannel, CancellationToken cancellationToken)
        {
            UserPreferences.Current.AssertUpdateCheckEnabledPolicy();
            UserPreferences.Current.AssertUpdateChannelPolicy(updateChannel);

            var channelPath = updateChannel switch
            {
                UpdateChannelType.Stable => "bravo-public",
                UpdateChannelType.Dev => "bravo-internal", 
                _ => throw new BravoUnexpectedInvalidOperationException($"Unhandled { nameof(UpdateChannelType) } value ({ updateChannel })")
            };

            using var httpClient = new HttpClient();
            var requestUri = $"https://bravorelease.blob.core.windows.net/{ channelPath }/currentversion.json?nocache={ DateTimeOffset.Now.ToUnixTimeSeconds() }";
            var json = await httpClient.GetStringAsync(requestUri, cancellationToken).ConfigureAwait(false);

            using var document = JsonDocument.Parse(json);

            var bravoUpdate = new BravoUpdate
            {
                UpdateChannel = updateChannel,
                InstalledVersion = AppEnvironment.ApplicationFileVersion,
                CurrentVersion = document.RootElement.GetProperty("version").GetString(),
                DownloadUrl = document.RootElement.GetProperty("download").GetString(),
                ChangelogUrl = document.RootElement.GetProperty("changelog").GetString(),
            };

            bravoUpdate.IsNewerVersion = GetIsNewerVersion(bravoUpdate);
            bravoUpdate.DownloadUrl = GetDownloadUrl(bravoUpdate);

            return bravoUpdate;

            static bool GetIsNewerVersion(BravoUpdate bravoUpdate)
            {
                var installedVersion = Version.Parse(bravoUpdate.InstalledVersion!);
                var currentVersion = Version.Parse(bravoUpdate.CurrentVersion!);

                return currentVersion > installedVersion;
            }

            static string GetDownloadUrl(BravoUpdate bravoUpdate)
            {
                BravoUnexpectedException.Assert(AppEnvironment.DeploymentMode != AppDeploymentMode.Packaged);

                var downloadUri = new Uri(bravoUpdate.DownloadUrl!, UriKind.Absolute);
                var downloadFileNameWithoutExtension = Path.GetFileNameWithoutExtension(downloadUri.LocalPath);
                var downloadFileExtension = Path.GetExtension(downloadUri.LocalPath);
                var downloadFileName = Path.GetFileName(downloadUri.LocalPath);

                if (AppEnvironment.PublishMode == AppPublishMode.FrameworkDependent)
                {
                    downloadFileNameWithoutExtension += "-frameworkdependent";
                }

                if (AppEnvironment.DeploymentMode == AppDeploymentMode.PerMachine)
                {
                    // keep current value
                }
                else if (AppEnvironment.DeploymentMode == AppDeploymentMode.PerUser)
                {
                    downloadFileNameWithoutExtension += "-userinstaller";
                }
                else if (AppEnvironment.DeploymentMode == AppDeploymentMode.Portable)
                {
                    downloadFileNameWithoutExtension += "-portable";
                    downloadFileExtension = ".zip";
                }

                var newFileName = $"{ downloadFileNameWithoutExtension }{ downloadFileExtension }";
                var newPath = downloadUri.LocalPath.Replace(downloadFileName, newFileName);
                var uriBuilder = new UriBuilder(downloadUri)
                {
                    Path = newPath
                };

                return uriBuilder.Uri.AbsoluteUri;
            }
        }
    }
}
