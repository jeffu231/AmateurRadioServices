using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CoreServices.Infrastructure;
using CoreServices.Integrations.Aprs;
using CoreServices.Integrations.Qrz;
using CoreServices.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreServices;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services.AddControllers(o =>
            {
                o.RespectBrowserAcceptHeader = true;
                o.ReturnHttpNotAcceptable = true;
            })
            .AddJsonOptions(opts =>
            {
                // If you want camelCase JSON (similar to your previous Newtonsoft camel-case usage)
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddXmlSerializerFormatters();
        
        ConfigureOptions(builder);
        ConfigureApiVersioning(builder);
        
        ConfigureSwagger(builder);
        
        builder.Services.AddHttpClient<QrzDataService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));

        builder.Services.AddHttpClient<AprsService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));
        
        builder.Services.AddProblemDetails();
        var rateLimitingOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();
        if (rateLimitingOptions.Enabled)
        {
            ConfigureRateLimiting(builder, rateLimitingOptions);
        }

        var app = builder.Build();
        
        EnableSwagger(app);
        
        app.UseExceptionHandler();

        if (rateLimitingOptions.Enabled)
        {
            app.UseRateLimiter();
        }

        app.UseAuthorization();

        var controllers = app.MapControllers();
        if (rateLimitingOptions.Enabled)
        {
            controllers.RequireRateLimiting("public-api");
        }

        app.Run();
    }

    private static void ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AprsOptions>()
            .BindConfiguration("Aprs")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.PostConfigure<AprsOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = builder.Configuration["AprsApiKey"] ?? string.Empty;
            }
        });

        builder.Services.AddOptions<QrzOptions>()
            .BindConfiguration("Qrz")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.PostConfigure<QrzOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Username))
            {
                options.Username = builder.Configuration["QrzUsername"] ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                options.Password = builder.Configuration["QrzPassword"] ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.AgentIdentifier))
            {
                options.AgentIdentifier = builder.Configuration["AgentIdentifier"] ?? string.Empty;
            }
        });

        builder.Services.AddOptions<RateLimitingOptions>()
            .BindConfiguration("RateLimiting")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void ConfigureRateLimiting(WebApplicationBuilder builder, RateLimitingOptions configuredOptions)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = configuredOptions.WindowSeconds.ToString();
                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many requests.",
                        type: "https://httpstatuses.com/429")
                    .ExecuteAsync(context.HttpContext);
            };
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetConcurrencyLimiter(
                    "all-requests",
                    _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = configuredOptions.GlobalConcurrencyPermitLimit,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
            options.AddPolicy("public-api", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuredOptions.PublicPermitLimit,
                        Window = TimeSpan.FromSeconds(configuredOptions.WindowSeconds),
                        QueueLimit = configuredOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
    }
    
    private static void ConfigureApiVersioning(WebApplicationBuilder builder)
    {
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"));
        });
            
        // Add ApiExplorer to discover versions
        builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                // Configure options for the API explorer
                options.GroupNameFormat = "'v'VVV"; // Formats the group name for Swagger, e.g., "v1" or "v1.1"
                options.SubstituteApiVersionInUrl = true; // Automatically replaces {version} in routes
            });
    }
    
    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
            
        builder.Services.AddSwaggerGen();

        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
    }

    private static void EnableSwagger(WebApplication app)
    {
        var swaggerBasePath = "api/ars";

        app.UseSwagger(options =>
        {
            options.RouteTemplate = swaggerBasePath + "/swagger/{documentName}/swagger.{json|yaml}";
        });
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = $"{swaggerBasePath}/swagger";
            var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
                options.SwaggerEndpoint($"{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
        });
    }
}
