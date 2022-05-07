namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Microsoft.Toolkit.Uwp.Notifications;
    using Sqlbi.Bravo.Models;
    using System;

    internal static class NotificationHelper
    {
        private const string ArgumentKeyUrl = "url";
        private const string ArgumentKeyAction = "action";
        private const string ArgumentValueDownload = "download";
        private const string ArgumentValueViewDetails = "viewDetails";

        public static void RegisterNotificationHandler()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                ToastNotificationManagerCompat.OnActivated += OnNotificationActivated;
            }
        }

        public static void NotifyUpdateAvailable(BravoUpdate bravoUpdate)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                return;

            var builder = new ToastContentBuilder();

            builder.AddText($"A new version of { AppEnvironment.ApplicationName } is available", AdaptiveTextStyle.Default);
            builder.AddButton(new ToastButton().SetContent("Download").AddArgument(ArgumentKeyAction, ArgumentValueDownload).AddArgument(ArgumentKeyUrl, bravoUpdate.DownloadUrl));
            builder.AddButton(new ToastButton().SetContent("View details").AddArgument(ArgumentKeyAction, ArgumentValueViewDetails).AddArgument(ArgumentKeyUrl, bravoUpdate.ChangelogUrl));
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
                                Text = bravoUpdate.CurrentVersion,
                                HintStyle = AdaptiveTextStyle.Base,
                                HintAlign = AdaptiveTextAlign.Left
                            },
                            new AdaptiveText()
                            {
                                Text = bravoUpdate.InstalledVersion,
                                HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                HintAlign = AdaptiveTextAlign.Left
                            }
                        }
                    }
                }
            });

            builder.Show((customize) =>
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                {
                    customize.ExpirationTime = DateTime.Now.AddMinutes(10);
                }
            });
        }

        public static void ClearNotifications()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                ToastNotificationManagerCompat.History.Clear();
            }
        }

        private static void OnNotificationActivated(ToastNotificationActivatedEventArgsCompat eventArgs)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                return;
            
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
                                _ = ProcessHelper.OpenBrowser(targetUri);
                            }
                        }
                        break;
                }
            }
        }
    }
}
