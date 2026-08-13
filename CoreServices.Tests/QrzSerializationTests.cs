using System.Text.Json;
using CoreServices.Model.Qrz;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies that QRZ authentication material cannot reach public JSON responses.
/// </summary>
public sealed class QrzSerializationTests
{
    /// <summary>
    /// Omits the provider session key when serializing a QRZ response.
    /// </summary>
    [Fact]
    public void Serialize_WhenSessionContainsKey_OmitsTheKey()
    {
        // Arrange
        var database = new QRZDatabase
        {
            Session = [new QRZDatabaseSession { Key = "secret-session-key" }]
        };

        // Act
        var json = JsonSerializer.Serialize(database);

        // Assert
        Assert.DoesNotContain("secret-session-key", json, StringComparison.Ordinal);
        Assert.DoesNotContain("key", json, StringComparison.OrdinalIgnoreCase);
    }
}
