namespace CoreServices.Validation;

/// <summary>
/// Validates bounded APRS identifier lists accepted by the v1 routes.
/// </summary>
internal static class AprsIdentifierValidator
{
    private const int MaximumIdentifierCount = 25;
    private const int MaximumIdentifierLength = 16;
    private const int MaximumRouteValueLength = 512;

    /// <summary>
    /// Determines whether an identifier list is bounded and contains unique non-empty values.
    /// </summary>
    /// <param name="identifiers">The comma-separated APRS identifiers.</param>
    /// <returns><see langword="true" /> if the list is valid; otherwise, <see langword="false" />.</returns>
    public static bool IsValid(string? identifiers)
    {
        if (string.IsNullOrWhiteSpace(identifiers) || identifiers.Length > MaximumRouteValueLength)
        {
            return false;
        }

        var values = identifiers.Split(',', StringSplitOptions.None | StringSplitOptions.TrimEntries);
        if (values.Length > MaximumIdentifierCount)
        {
            return false;
        }

        var uniqueValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return values.All(value => value.Length is > 0 and <= MaximumIdentifierLength && uniqueValues.Add(value));
    }
}
