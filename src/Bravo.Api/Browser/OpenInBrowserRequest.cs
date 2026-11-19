using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bravo.Core.Browser;

namespace Bravo.Api.Browser;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// The page to open in the default browser of the user.
/// </summary>
public sealed record OpenInBrowserRequest : IValidatableObject
{
    /// <summary>
    /// The address of the page: absolute, with the scheme <c>https</c> or <c>http</c>, on a site that the
    /// application trusts.
    /// </summary>
    public required Uri Address { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TrustedSites.Contains(Address))
        {
            var message = "The address is not the one of a page on a site that the application trusts.";
            yield return new ValidationResult(message, [nameof(Address)]);
        }
    }
}
