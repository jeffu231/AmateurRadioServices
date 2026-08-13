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
                Callsign = [new QRZDatabaseCallsign
                {
                    call = "N9NOC",
                    aliases = "N9NOC/P",
                    dxcc = "291",
                    fname = "Alex",
                    name = "Operator",
                    addr1 = "123 Main Street",
                    addr2 = "Suite 4",
                    state = "IL",
                    zip = "60601",
                    country = "United States",
                    lat = "41.8781",
                    lon = "-87.6298",
                    grid = "EN61",
                    county = "Cook",
                    ccode = "US",
                    fips = "17031",
                    land = "United States",
                    efdate = "2020-01-01",
                    expdate = "2030-01-01",
                    @class = "Amateur Extra",
                    codes = "HVIE",
                    qslmgr = "N9NOC",
                    email = "alex@example.test",
                    u_views = "42",
                    bio = "Test operator",
                    biodate = "2026-01-01",
                    moddate = "2026-08-13",
                    MSA = "Chicago",
                    AreaCode = "312",
                    TimeZone = "America/Chicago",
                    GMTOffset = "-6",
                    DST = "Y",
                    eqsl = "1",
                    mqsl = "1",
                    cqzone = "4",
                    ituzone = "8",
                    lotw = "1",
                    geoloc = "41.8781,-87.6298",
                    name_fmt = "Alex Operator"
                }]
            }));
        });
    }
}
