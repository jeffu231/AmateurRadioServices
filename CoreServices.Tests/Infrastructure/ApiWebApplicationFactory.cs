using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Starts the API with non-secret test configuration and no live provider calls.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<ApiEntryPoint>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
