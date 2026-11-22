namespace Bravo.Platform.Windows.Tests.Network;

/// <summary>
/// A request as a proxy of a test received it. Of the credentials it keeps the scheme alone.
/// </summary>
/// <param name="Method">The method, <c>CONNECT</c> for a tunnel.</param>
/// <param name="Target">The address in full for a request through a proxy, the path for one to a server.</param>
/// <param name="AuthorizationScheme">The scheme of the credentials for the proxy, when the request has them.</param>
/// <param name="TraceParent">The <c>traceparent</c> header, which tells the trace of the work that sent it.</param>
internal sealed record TestProxyRequest(string Method, string Target, string? AuthorizationScheme, string? TraceParent = null);
