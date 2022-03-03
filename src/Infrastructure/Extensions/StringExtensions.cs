namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;

    internal static class StringExtensions
    {
        private static Regex? _invalidFileNameCharsRegex;

        public static bool IsPBIDesktopMainWindowTitle(this string windowTitle)
        {
            return windowTitle.EndsWith(AppEnvironment.PBIDesktopMainWindowTitleSuffix);
        }

        /// <summary>
        /// Convert the old Microsoft datetime offset "/Date(1617810719887)/" to <see cref="DateTimeOffset"/>
        /// </summary>
        /// <param name="microsoftDateTimeOffset">Old Microsoft datetime offset string "/Date(1617810719887)/"</param>
        public static DateTimeOffset? ToDateTimeOffset(this string microsoftDateTimeOffset)
        {
            var regex = new Regex("^\\/Date\\(([0-9]+)\\)\\/$");

            var match = regex.Match(microsoftDateTimeOffset);
            if (match.Success)
            {
                var seconds = long.Parse(match.Groups[1].Value);
                return DateTimeOffset.FromUnixTimeMilliseconds(seconds);
            }

            return null;
        }

        public static string? NullIfEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static string? NullIfWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static string FormatInvariant(this string format, object? arg0)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arg0);
        }

        public static string ToFileDialogFilterString(this string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                filter = " |*.*";

            var stringBuilder = new StringBuilder(filter);
            stringBuilder.Replace('|', '\0');
            stringBuilder.Append('\0');
            stringBuilder.Append('\0');
            return stringBuilder.ToString();
        }

        public static string ReplaceInvalidFileNameChars(this string path, string replacement = "_")
        {
            if (_invalidFileNameCharsRegex is null)
            {
                // Not necessary to include GetInvalidPathChars(), the illegal file name char list contains the illegal path char list
                var pattern = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
                _invalidFileNameCharsRegex = new($"[{ pattern }]");
            }

            path = _invalidFileNameCharsRegex.Replace(path, replacement);
            return path;
        }

        public static bool EqualsI(this string? current, string? value)
        {
            return current?.Equals(value, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Remove initial CR/LF or SPACE/CR/LF after the last non-empty character of the expression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (string? Expression, DaxLineBreakStyle LineBreakStyle) NormalizeDax(this string? expression)
        {
            // Ignore minor differences in DAX Formatter https://github.com/sql-bi/Bravo/issues/86
            // We ignore differences in measures caused by initial CR/LF or spaces/CR/LF after the last non-empty character of the formula.
            // This way, we do not report as "to be formatted" a measure that differs only for initial and final CR/LF/spaces
            
            var lineBreakStyle = DaxLineBreakStyle.None;
            
            if (expression?.Length > 0)
            {
                // Replace all occurrences of CRLF with LF since this is the default EOL character in SSAS
                expression = expression.Replace("\r\n", "\n");

                if (!expression.StartsWith("\n\n"))
                {
                    // remove a single EOL leading character, if any
                    if (expression[0] == '\n')
                    {
                        expression = expression[1..];
                        lineBreakStyle = DaxLineBreakStyle.InitialLineBreak;
                    }
                }

                //expression = expression.TrimEnd('\n', ' ');

                if (!expression.EndsWith("\n\n"))
                {
                    // remove a single EOL trailing character, if any
                    if (expression[^1] == '\n')
                        expression = expression[..^1];
                }

                if (!expression.EndsWith("  "))
                {
                    // remove a single SPACE trailing character, if any
                    if (expression[^1] == ' ')
                        expression = expression[..^1];
                }
            }

            return (expression, lineBreakStyle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ApplyLineBreakStyle(this string? expression, DaxLineBreakStyle lineBreakStyle)
        {
            if (expression?.Length > 0)
            {
                switch (lineBreakStyle)
                {
                    case DaxLineBreakStyle.None:
                        {
                            if (expression[0] == '\n')
                                expression = expression[1..];
                        }
                        break;
                    case DaxLineBreakStyle.InitialLineBreak:
                        {
                            expression = '\n' + expression;
                        }
                        break;
                    default:
                        throw new BravoUnexpectedException(lineBreakStyle.ToString());
                }
            }

            return expression;
        }
    }
}
