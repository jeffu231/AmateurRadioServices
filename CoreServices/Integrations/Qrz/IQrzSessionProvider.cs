namespace CoreServices.Integrations.Qrz;

/// <summary>
/// Coordinates one QRZ session per application process.
/// </summary>
public interface IQrzSessionProvider
{
    /// <summary>
    /// Gets a valid QRZ session, refreshing it when necessary.
    /// </summary>
    /// <param name="cancellationToken">The token that can cancel the operation.</param>
    /// <returns>The active session, or <see langword="null"/> when authentication fails.</returns>
    Task<QrzSession?> GetSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates the current session after QRZ rejects it.
    /// </summary>
    void InvalidateSession();
}
