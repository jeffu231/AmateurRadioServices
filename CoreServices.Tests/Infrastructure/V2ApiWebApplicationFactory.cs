using CoreServices.Integrations.Aprs;
using CoreServices.Integrations.Qrz;
using CoreServices.Model.Aprs;
using CoreServices.Model.Qrz;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Starts the API with deterministic v2 provider responses.
/// </summary>
public sealed class V2ApiWebApplicationFactory : WebApplicationFactory<ApiEntryPoint>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAprsClient>();
            services.RemoveAll<IQrzClient>();
            services.AddSingleton<IAprsClient>(new StubAprsClient(new AprsLocRecord
            {
                Found = 1,
                Entries = [new AprsEntry
                {
                    Name = "N9NOC/P",
                    SrcCall = "N9NOC/P",
                    Lat = 41.8781,
                    Lng = -87.6298,
                    Comment = "test location"
                }]
            }));
            services.AddSingleton<IQrzClient>(new StubQrzClient(new QRZDatabase
            {
                Callsign = [new QRZDatabaseCallsign { call = "N9NOC", grid = "EN61" }]
            }));
        });
    }
}
