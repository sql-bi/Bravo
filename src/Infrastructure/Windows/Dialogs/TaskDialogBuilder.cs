using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Windows.Dialogs;

/// <summary>
/// Provides a fluent builder for creating and showing a <see cref="TaskDialogPage"/> dialog.
/// </summary>
public sealed class TaskDialogBuilder
{
    private static readonly Lazy<TaskDialogIcon?> s_currentProcessIcon = new(LoadCurrentProcessIcon);

    private readonly TaskDialogPage _page;
    private IntPtr _ownerHandle = IntPtr.Zero;
    private TaskDialogStartupLocation _startupLocation = TaskDialogStartupLocation.CenterOwner;

    /// <summary>
    /// Creates a new <see cref="TaskDialogBuilder"/> instance.
    /// </summary>
    public static TaskDialogBuilder Create() => new();

    private TaskDialogBuilder()
    {
        _page = new TaskDialogPage
        {
            AllowCancel = false,
            AllowMinimize = false
        };
    }

    public TaskDialogBuilder WithCaption(string caption)
    {
        _page.Caption = caption;
        return this;
    }

    public TaskDialogBuilder WithHeading(string heading)
    {
        _page.Heading = heading;
        return this;
    }

    public TaskDialogBuilder WithText(string text)
    {
        _page.Text = text;
        return this;
    }

    /// <summary>
    /// Sets the dialog icon to the icon of the current process, if available.
    /// </summary>
    public TaskDialogBuilder WithCurrentProcessIcon()
    {
        if (s_currentProcessIcon.Value is { } icon)
            _page.Icon = icon;

        return this;
    }

    public TaskDialogBuilder WithIcon(TaskDialogIcon icon)
    {
        _page.Icon = icon;
        return this;
    }

    public TaskDialogBuilder WithFootnote(string text, TaskDialogIcon? icon = null)
    {
        _page.Footnote = new TaskDialogFootnote(text)
        {
            Icon = icon,
        };
        return this;
    }

    public TaskDialogBuilder WithExpander(
        string text,
        bool expanded = false,
        TaskDialogExpanderPosition position = TaskDialogExpanderPosition.AfterText,
        string? expandedButtonText = null,
        string? collapsedButtonText = null)
    {
        _page.Expander = new TaskDialogExpander(text)
        {
            Expanded = expanded,
            Position = position,
            ExpandedButtonText = expandedButtonText,
            CollapsedButtonText = collapsedButtonText,
        };
        return this;
    }

    /// <summary>
    /// Enables clickable links in the dialog text and footnote, and invokes the specified callback when a link is clicked.
    /// </summary>
    public TaskDialogBuilder WithEnableLinks(Action<string> linkClicked)
    {
        _page.EnableLinks = true;
        _page.LinkClicked += (_, e) => linkClicked(e.LinkHref);
        return this;
    }

    /// <summary>
    /// Sets the dialog to automatically size to its content. If false, the dialog will have a fixed size.
    /// </summary>
    public TaskDialogBuilder WithSizeToContent(bool sizeToContent = true)
    {
        _page.SizeToContent = sizeToContent;
        return this;
    }

    /// <summary>
    /// Sets the default button that is focused when the dialog is shown.
    /// </summary>
    public TaskDialogBuilder WithDefaultButton(TaskDialogButton button)
    {
        _page.DefaultButton = button;
        return this;
    }

    /// <summary>
    /// Sets the window that owns the dialog to the main window of the current process.
    /// </summary>
    public TaskDialogBuilder WithCurrentProcessMainWindowOwner()
    {
        using var process = Process.GetCurrentProcess();

        return WithOwner(process.MainWindowHandle);
    }

    /// <summary>
    /// Sets the window that owns the dialog. Without an owner the dialog is top-level.
    /// </summary>
    public TaskDialogBuilder WithOwner(IntPtr ownerHandle)
    {
        _ownerHandle = ownerHandle;
        return this;
    }

    public TaskDialogBuilder WithStartupLocation(TaskDialogStartupLocation startupLocation)
    {
        _startupLocation = startupLocation;
        return this;
    }

    public TaskDialogBuilder WithAllowCancel(bool allowCancel = true)
    {
        _page.AllowCancel = allowCancel;
        return this;
    }

    public TaskDialogBuilder AddButtons(params TaskDialogButton[] buttons)
    {
        foreach (var button in buttons)
            _page.Buttons.Add(button);

        return this;
    }

    public TaskDialogPage Build()
    {
        return _page;
    }

    /// <summary>
    /// Shows the dialog modally and returns the button the user chose.
    /// </summary>
    public TaskDialogButton Show()
    {
        if (_ownerHandle != IntPtr.Zero)
        {
            return TaskDialog.ShowDialog(_ownerHandle, _page, _startupLocation);
        }

        return TaskDialog.ShowDialog(_page, _startupLocation);
    }

    private static TaskDialogIcon? LoadCurrentProcessIcon()
    {
        if (Environment.ProcessPath is { } path && Icon.ExtractAssociatedIcon(path) is { } icon)
            return new TaskDialogIcon(icon);

        return null;
    }
}
