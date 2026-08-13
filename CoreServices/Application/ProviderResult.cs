namespace CoreServices.Application;

/// <summary>
/// Represents either a successful provider value or a non-sensitive failure category.
/// </summary>
/// <typeparam name="TValue">The successful provider value.</typeparam>
public sealed record ProviderResult<TValue>
{
    /// <summary>
    /// Gets the successful value, or <see langword="null"/> when the operation failed.
    /// </summary>
    public TValue? Value { get; init; }

    /// <summary>
    /// Gets the failure category, or <see langword="null"/> when the operation succeeded.
    /// </summary>
    public ProviderFailureKind? FailureKind { get; init; }

    /// <summary>
    /// Gets a value that indicates whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => FailureKind is null;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The provider value.</param>
    /// <returns>A successful result.</returns>
    public static ProviderResult<TValue> Success(TValue value) => new() { Value = value };

    /// <summary>
    /// Creates a failed result without exception details.
    /// </summary>
    /// <param name="failureKind">The non-sensitive failure category.</param>
    /// <returns>A failed result.</returns>
    public static ProviderResult<TValue> Failure(ProviderFailureKind failureKind) => new() { FailureKind = failureKind };
}
