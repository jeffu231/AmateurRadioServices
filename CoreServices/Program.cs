using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CoreServices.Services;

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
        
        ConfigureApiVersioning(builder);
        
        ConfigureSwagger(builder);
        
        builder.Services.AddHttpClient<QrzDataService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));

        builder.Services.AddHttpClient<AprsService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));
        
        builder.Services.AddProblemDetails();

        var app = builder.Build(); 
        
        EnableSwagger(app);
        
        app.UseAuthorization();
        
        app.UseExceptionHandler();

        app.MapControllers();

        app.Run();
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