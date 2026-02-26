using ChaosWebApp.Middleware;
using ChaosWebApp.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration: Azure App Configuration or environment variables ───────────
var appConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");
if (!string.IsNullOrWhiteSpace(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(new Uri(appConfigEndpoint), new Azure.Identity.DefaultAzureCredential()));
}
else
{
    // Fallback: when Azure App Configuration is not configured, ensure environment
    // variables (prefixed with "CHAOSAPP_") can override any configuration setting.
    // Example: CHAOSAPP_ApplicationInsights__ConnectionString overrides
    //          ApplicationInsights:ConnectionString in appsettings.json.
    builder.Configuration.AddEnvironmentVariables(prefix: "CHAOSAPP_");
}

// ── Application Insights ─────────────────────────────────────────────────────
var appInsightsCs = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
                    ?? builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsCs))
{
    builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = appInsightsCs);
}

// ── MVC / Razor Pages ─────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// ── OpenAPI / Swagger ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ChaosWebApp API",
        Version     = "v1",
        Description = "RESTful product catalogue API with chaos injection middleware.",
        Contact     = new OpenApiContact { Name = "ChaosApp", Url = new Uri("https://github.com/frkim/ChaosWebApp") }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IChaosService,   ChaosService>();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Swagger UI available in all environments for demo purposes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChaosWebApp API v1");
    c.RoutePrefix          = "swagger";
    c.DocumentTitle        = "ChaosApp — API Explorer";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

app.UseStaticFiles();
app.UseRouting();

// Chaos injection — after routing, before controllers/pages
app.UseMiddleware<ChaosMiddleware>();

app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();
app.MapRazorPages();

app.Run();
