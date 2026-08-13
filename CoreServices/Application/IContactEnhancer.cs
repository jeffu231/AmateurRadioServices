using CoreServices.Model;

namespace CoreServices.Application;

/// <summary>
/// Defines contact-enhancement operations independent of HTTP controllers.
/// </summary>
public interface IContactEnhancer
{
    /// <summary>
    /// Enhances a contact using available callsign information.
    /// </summary>
    /// <param name="contact">The contact to enhance.</param>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>The enhanced contact result.</returns>
    Task<ProviderResult<ContactInfo>> EnhanceAsync(ContactInfo contact, CancellationToken cancellationToken);
}
