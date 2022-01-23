using AutoUpdaterDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class NotificationHelper
    {
        private const string ArgumentKeyUrl = "url";
        private const string ArgumentKeyAction = "action";
        private const string ArgumentValueDownload = "download";
        private const string ArgumentValueViewDetails = "viewDetails";

        public static void RegisterNotificationHandler() => ToastNotificationManagerCompat.OnActivated += OnNotificationActivated;

        public static void NotifyUpdateAvailable(UpdateInfoEventArgs updateInfo)
        {
            var builder = new ToastContentBuilder();

            builder.AddText($"A new version of { AppConstants.ApplicationName } is available", AdaptiveTextStyle.Default);
            builder.AddButton(new ToastButton().SetContent("Download").AddArgument(ArgumentKeyAction, ArgumentValueDownload).AddArgument(ArgumentKeyUrl, updateInfo.DownloadURL));
            builder.AddButton(new ToastButton().SetContent("View details").AddArgument(ArgumentKeyAction, ArgumentValueViewDetails).AddArgument(ArgumentKeyUrl, updateInfo.ChangelogURL));
            // builder.AddAttributionText("Via update notifier")
            builder.AddVisualChild(new AdaptiveGroup()
            {
                Children =
                {
                    new AdaptiveSubgroup()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "New version available",
                                HintStyle = AdaptiveTextStyle.Base
                            },
                            new AdaptiveText()
                            {
                                Text = "Current version",
                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                            }
                        }
                    },
                    new AdaptiveSubgroup()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = updateInfo.CurrentVersion,
                                HintStyle = AdaptiveTextStyle.Base,
                                HintAlign = AdaptiveTextAlign.Left
                            },
                            new AdaptiveText()
                            {
                                Text = updateInfo.InstalledVersion.ToString(),
                                HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                HintAlign = AdaptiveTextAlign.Left
                            }
                        }
                    }
                }
            });

            builder.Show((customize) =>
            {
                customize.ExpirationTime = DateTime.Now.AddMinutes(10);
            });
        }

        public static void ClearNotifications()
        {
            ToastNotificationManagerCompat.History.Clear();
        }

        private static void OnNotificationActivated(ToastNotificationActivatedEventArgsCompat eventArgs)
        {
            var toastArgs = ToastArguments.Parse(eventArgs.Argument);

            if (toastArgs.TryGetValue(ArgumentKeyAction, out var actionValue))
            {
                switch (actionValue)
                {
                    case ArgumentValueDownload:
                    case ArgumentValueViewDetails:
                        {
                            if (toastArgs.TryGetValue(ArgumentKeyUrl, out var urlValue) && Uri.TryCreate(urlValue, UriKind.Absolute, out var targetUri))
                            {
                                // sanitize/check input before starting the process
                                if (targetUri.IsAbsoluteUri && !targetUri.IsFile && !targetUri.IsUnc && !targetUri.IsLoopback && targetUri.Authority.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                                {
                                    _ = Process.Start(new ProcessStartInfo
                                    {
                                        UseShellExecute = true,
                                        FileName = urlValue
                                    });
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
