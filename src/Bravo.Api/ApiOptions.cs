using System.Collections.Generic;

namespace Bravo.Api;

/// <summary>
/// What the application that hosts the API decides about it.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>
    /// The origins whose pages may call the API from a browser, each written as scheme, host and port. A
    /// browser refuses the answer to a page of any other origin, and to every page while this list is empty.
    /// </summary>
    public IList<string> AllowedOrigins { get; } = [];
}
