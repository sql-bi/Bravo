using System;
using System.Drawing;

namespace Bravo.Desktop;

/// <summary>
/// The icon of the application, with all its sizes, from the resources of the assembly.
/// </summary>
internal static class ProductIcon
{
    private const string ResourceName = "Bravo.ico";

    public static Icon Load()
    {
        using var stream = typeof(ProductIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The assembly has no resource named {ResourceName}.");
        return new Icon(stream);
    }
}
