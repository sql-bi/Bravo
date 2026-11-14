using System;
using System.Drawing;
using Bravo.Core.Settings;

namespace Bravo.Desktop.Shell;

/// <summary>
/// The colors of the main window for a theme of the user: whether its frame is the dark one, and what it
/// shows where the page draws nothing, as before the page has loaded.
/// </summary>
internal sealed record WindowColors
{
    // The background of the user interface in each mode: nothing flashes in another color before the page.
    public static WindowColors Light { get; } = new() { IsDark = false, Background = Color.FromArgb(0xF3, 0xF3, 0xF3) };

    public static WindowColors Dark { get; } = new() { IsDark = true, Background = Color.FromArgb(0x20, 0x20, 0x20) };

    public required bool IsDark { get; init; }

    public required Color Background { get; init; }

    /// <param name="theme">The theme that the user chose.</param>
    /// <param name="isSystemDark">Whether Windows asks applications to be dark, which Auto follows.</param>
    public static WindowColors For(Theme theme, bool isSystemDark)
    {
        return theme switch
        {
            Theme.Light => Light,
            Theme.Dark => Dark,
            Theme.Auto => isSystemDark ? Dark : Light,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "No colors for the theme."),
        };
    }
}
