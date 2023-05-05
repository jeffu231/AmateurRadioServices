using System.Net.Http.Headers;
using CoreServices;
using CoreServices.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers(o =>
{
    o.RespectBrowserAcceptHeader = true;
    o.ReturnHttpNotAcceptable = true;
}).AddNewtonsoftJson().AddXmlSerializerFormatters();



builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"));
});

// Add ApiExplorer to discover versions
builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.AddHttpClient<QrzDataService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(10));

var app = builder.Build();

var swaggerBasePath = "api/ars";

app.UseSwagger(options =>
{
    Console.Out.WriteLine(options.RouteTemplate);
    options.RouteTemplate = swaggerBasePath + "/swagger/{documentName}/swagger.{json|yaml}";
    Console.Out.WriteLine(options.RouteTemplate);
});
app.UseSwaggerUI(options =>
{
    Console.Out.WriteLine($"Prefix {options.RoutePrefix}");
    options.RoutePrefix = $"{swaggerBasePath}/swagger";
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
        options.SwaggerEndpoint($"{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant());
});

app.UseAuthorization();

app.MapControllers();

app.Run();