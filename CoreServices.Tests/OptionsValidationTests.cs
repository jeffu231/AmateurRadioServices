using System.ComponentModel.DataAnnotations;
using CoreServices.Integrations.Qrz;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreServices.Tests;

/// <summary>
/// Verifies validation rules for required provider configuration.
/// </summary>
public sealed class OptionsValidationTests
{
    /// <summary>
    /// Stops application startup when required provider configuration is absent.
    /// </summary>
    [Fact]
    public void CreateClient_WhenProviderCredentialsAreMissing_FailsDuringStartup()
    {
        // Arrange
        using var factory = new WebApplicationFactory<ApiEntryPoint>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("MissingConfiguration"));

        // Act
        var exception = Record.Exception(factory.CreateClient);

        // Assert
        var aggregateException = Assert.IsType<AggregateException>(exception);
        Assert.Contains(aggregateException.InnerExceptions, exception => exception is OptionsValidationException);
    }

    /// <summary>
    /// Rejects QRZ configuration that has no account credentials.
    /// </summary>
    [Fact]
    public void Validate_WhenCredentialsAreMissing_ReturnsValidationFailures()
    {
        // Arrange
        var results = new List<ValidationResult>();
        var options = new QrzOptions();

        // Act
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(QrzOptions.Username)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(QrzOptions.Password)));
    }
}
