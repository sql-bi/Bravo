using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Bravo.Setup.CommandLine;

/// <summary>
/// The options that follow the name of a command, and the first thing that is wrong with them. A command
/// reads the options it has; the ones that nobody read are not options of that command.
/// </summary>
internal sealed class SetupOptions
{
    private const string NamePrefix = "--";

    private readonly Dictionary<string, string> _values;
    private readonly HashSet<string> _read = new(StringComparer.Ordinal);

    private SetupOptions(Dictionary<string, string> values)
    {
        _values = values;
    }

    /// <summary>
    /// The first error that reading the options met, or <see langword="null"/>.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Reads the options from a command line, whose first argument is the name of the command.
    /// </summary>
    public static bool TryRead(
        IReadOnlyList<string> arguments,
        [NotNullWhen(true)] out SetupOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        options = null;
        for (var index = 1; index < arguments.Count; index += 2)
        {
            var name = arguments[index];
            if (!name.StartsWith(NamePrefix, StringComparison.Ordinal))
            {
                error = $"'{name}' is not the name of an option.";
                return false;
            }

            if (index + 1 == arguments.Count)
            {
                error = $"The option {name} has no value.";
                return false;
            }

            if (!values.TryAdd(name, arguments[index + 1]))
            {
                error = $"The option {name} is given twice.";
                return false;
            }
        }

        options = new SetupOptions(values);
        error = null;
        return true;
    }

    /// <summary>
    /// The value of an option, which may be empty, or <see langword="null"/> when the option is missing.
    /// </summary>
    public string? Read(string name)
    {
        _read.Add(name);
        if (_values.TryGetValue(name, out var value))
        {
            return value;
        }

        Reject($"The option {name} is missing.");
        return null;
    }

    /// <summary>
    /// The value of an option that is the full path of a file or of a folder.
    /// </summary>
    public string? ReadFullPath(string name)
    {
        var value = Read(name);
        if (value is null)
        {
            return null;
        }

        // A relative path would depend on the folder that the package starts the program in.
        if (!Path.IsPathFullyQualified(value))
        {
            Reject($"The option {name} is not a full path: '{value}'.");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Says which option nobody read, or <see langword="null"/> when every option was read.
    /// </summary>
    public string? DescribeUnread()
    {
        var unread = _values.Keys.FirstOrDefault(name => !_read.Contains(name));
        return unread is null ? null : $"{unread} is not an option of the command.";
    }

    // Only the first error is kept: the ones after it are often its consequences.
    private void Reject(string error)
    {
        Error ??= error;
    }
}
