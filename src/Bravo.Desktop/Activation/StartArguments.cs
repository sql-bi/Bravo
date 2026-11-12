using Bravo.Core.Activation;
using Bravo.Desktop.Development;

namespace Bravo.Desktop.Activation;

/// <summary>
/// What the command line of a start says.
/// </summary>
/// <param name="Request">What the start asks the application for: handed over to the instance that runs.</param>
/// <param name="Development">
/// What the start asks its own host for, in a development build. A start that is handed over leaves it unused.
/// </param>
internal sealed record StartArguments(ActivationRequest Request, DevelopmentOptions Development);
