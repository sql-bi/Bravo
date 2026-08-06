using System;

namespace Sqlbi.Bravo.Host;

/// <summary>
/// Carries what <see cref="BravoApplicationInitializer"/> produced, as typed entries.
/// </summary>
/// <remarks>
/// Not a container and not a facade: it resolves nothing, forwards nothing, and never leaves the
/// composition root. <see cref="BravoApplicationBuilder"/> publishes each entry to the service
/// collection as an instance; ownership of the disposable entries stays here.
/// </remarks>
internal sealed class BravoApplicationInitializationContext(
    IBravoApplicationInstance instance) : IDisposable
{
    /// <summary>
    /// Gets the single-instance binding; <see cref="IBravoApplicationInstance.IsPrimary"/> decides
    /// whether this process runs the application or redirects its activation.
    /// </summary>
    public IBravoApplicationInstance Instance { get; } = instance;

    /// <summary>
    /// Disposes the entries the initialization created and owns, in reverse creation order. Entries
    /// that are still process-wide statics (telemetry, user settings, web proxy) own themselves and
    /// are not disposed here until the initialization creates them.
    /// </summary>
    public void Dispose() => Instance.Dispose();
}
