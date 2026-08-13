using System.Text.RegularExpressions;

namespace CoreServices.Validation;

/// <summary>
/// Validates four-, six-, and eight-character Maidenhead grid locators.
/// </summary>
internal static partial class MaidenheadGridValidator
{
    /// <summary>
    /// Determines whether a grid locator has a supported Maidenhead format.
    /// </summary>
    /// <param name="grid">The grid locator to validate.</param>
    /// <returns><see langword="true" /> if the locator is valid; otherwise, <see langword="false" />.</returns>
    public static bool IsValid(string grid) => GridPattern().IsMatch(grid);

    [GeneratedRegex("^[A-R]{2}[0-9]{2}(?:[A-X]{2}(?:[0-9]{2})?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex GridPattern();
}
