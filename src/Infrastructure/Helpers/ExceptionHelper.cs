namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;
    using System.Windows.Forms;

    internal static class ExceptionHelper
    {
        public static void WriteToEventLog(Exception exception, EventLogEntryType type, bool throwOnError = true)
        {
            var message = exception.ToString();
            WriteToEventLog(message, type, throwOnError);
        }

        public static void WriteToEventLog(string message, EventLogEntryType type, bool throwOnError = true)
        {
            try
            {
                using var eventLog = new EventLog(logName: "Application", machineName: ".", source: "Application");
                eventLog.WriteEntry(message, type);
            }
            catch
            {
                if (throwOnError)
                    throw;
            }
        }

        public static bool IsOrHasInner<T>(this Exception exception) where T : Exception
        {
            var foundException = Find<T>(exception);
            return foundException != null;
        }

        public static T? Find<T>(this Exception exception) where T : Exception
        {
            if (exception is T foundException)
                return foundException;

            var innerException = exception.InnerException;

            while (innerException is not null)
            {
                if (innerException is T foundInnerException)
                    return foundInnerException;

                innerException = innerException.InnerException;
            }

            return null;
        }

        public static bool IsSafeException(Exception ex)
        {
            if (ex is not StackOverflowException && ex is not OutOfMemoryException && ex is not ThreadAbortException && ex is not AccessViolationException && ex is not SEHException)
            {
                return !typeof(SecurityException).IsAssignableFrom(ex.GetType());
            }

            return false;
        }

        public static void ShowDialog(Exception exception)
        {
            var page = new TaskDialogPage()
            {
                Caption = AppEnvironment.ApplicationMainWindowTitle,
                Heading = @$"Unhandled exception has occurred. The application will be shut down and the error details will be logged in the Windows Event Log.

[{ exception.GetType().Name }] { exception.Message }",
                Icon = TaskDialogIcon.Error,
                AllowCancel = false,
                Buttons = 
                {
                    new TaskDialogCommandLinkButton("&Copy details", "Copy error details to clipboard and close")
                    { 
                        Tag = 10
                    },
                    new TaskDialogCommandLinkButton("&Close", "Terminate the application")
                    { 
                        Tag = 20 
                    },
                },
                Expander = new TaskDialogExpander()
                {
                    Expanded = false,
                    Text = $"{ exception }",
                    Position = TaskDialogExpanderPosition.AfterFootnote,
                }
            };

            var dialogButton = TaskDialog.ShowDialog(page, TaskDialogStartupLocation.CenterScreen);

            switch (dialogButton.Tag)
            {
                case 10:
                    Clipboard.SetText(page.Expander.Text, TextDataFormat.Text);
                    break;
                case 20:
                    break;
                default:
                    throw new BravoUnexpectedInvalidOperationException($"Unhandled { nameof(TaskDialogButton) } result ({ dialogButton.Tag })");
            }
        }
    }
}
