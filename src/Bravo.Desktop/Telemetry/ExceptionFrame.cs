using System.Text.Json.Serialization;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// One frame of the stack of an exception.
/// </summary>
/// <param name="Level">Its place in the whole stack, from 0 where the exception was thrown.</param>
/// <param name="Method">The full name of the type and the name of the method.</param>
/// <param name="Assembly">The full name of the assembly of the method.</param>
/// <param name="FileName">The source file, when the symbols name it, without the personal names in its path.</param>
/// <param name="Line">The line in the source file, or 0.</param>
internal sealed record ExceptionFrame(int Level, string Method, string Assembly, string? FileName, int Line)
{
    /// <summary>
    /// The characters of the frame that count towards the limit of a stack.
    /// </summary>
    [JsonIgnore]
    public int Length => Method.Length + Assembly.Length + (FileName?.Length ?? 0);
}
