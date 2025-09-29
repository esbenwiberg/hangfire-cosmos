using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using HangfireCosmos.Storage;
using HangfireCosmos.Storage.Extensions;
using HangfireCosmos.Web.Services;
using HangfireCosmos.Web.Middleware;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

try
{
    // Early startup logging before DI container is available
    Console.WriteLine("Starting Hangfire Cosmos Storage Provider Web Application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Microsoft Logging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
        });

    // Add API Explorer services for Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Hangfire Cosmos Storage Provider API",
            Version = "v1",
            Description = "Comprehensive API for testing and monitoring Hangfire with Cosmos DB storage provider",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Hangfire Cosmos Storage Provider",
                Url = new Uri("https://github.com/your-repo/hangfire-cosmos")
            }
        });

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Configure Cosmos DB connection string
    var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb");
    if (string.IsNullOrEmpty(cosmosConnectionString))
    {
        // Early logging before DI container is available
        Console.WriteLine("Cosmos DB connection string not found, using default emulator connection");
        cosmosConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    }

    // Configure Hangfire with Cosmos DB storage (with fallback to in-memory)
    var cosmosOptions = builder.Configuration.GetSection("HangfireCosmosOptions");
    
    builder.Services.AddHangfire(configuration =>
    {
        configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        try
        {
            // Test Cosmos DB connectivity before configuring
            using var testClient = new CosmosClient(cosmosConnectionString);
            var testTask = testClient.ReadAccountAsync();
            testTask.Wait(TimeSpan.FromSeconds(5));
            
            // Early logging before DI container is available
            Console.WriteLine("Cosmos DB connection successful, using Cosmos storage");
            configuration.UseCosmosStorage(cosmosConnectionString, options =>
            {
                options.DatabaseName = cosmosOptions["DatabaseName"] ?? "hangfire";
                options.DefaultThroughput = int.Parse(cosmosOptions["DefaultThroughput"] ?? "400");
                options.AutoCreateDatabase = bool.Parse(cosmosOptions["AutoCreateDatabase"] ?? "true");
                options.AutoCreateContainers = bool.Parse(cosmosOptions["AutoCreateContainers"] ?? "true");
                options.DefaultJobExpiration = TimeSpan.Parse(cosmosOptions["DefaultJobExpiration"] ?? "7.00:00:00");
                options.ServerTimeout = TimeSpan.Parse(cosmosOptions["ServerTimeout"] ?? "00:05:00");
                options.LockTimeout = TimeSpan.Parse(cosmosOptions["LockTimeout"] ?? "00:01:00");
                options.MaxRetryAttempts = int.Parse(cosmosOptions["MaxRetryAttempts"] ?? "3");
                options.RetryDelay = TimeSpan.Parse(cosmosOptions["RetryDelay"] ?? "00:00:00.500");
                options.CollectionStrategy = Enum.TryParse<CollectionStrategy>(cosmosOptions["CollectionStrategy"], out var strategy) ? strategy : CollectionStrategy.Dedicated;
            });
        }
        catch (Exception ex)
        {
            // Early logging before DI container is available
            Console.WriteLine($"Cosmos DB not available, falling back to in-memory storage: {ex.Message}");
            configuration.UseMemoryStorage();
        }
    });

    // Configure Hangfire Server
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = Environment.ProcessorCount * 2;
        options.Queues = new[] { "default", "critical", "background" };
        options.ServerName = $"{Environment.MachineName}-{Environment.ProcessId}";
    });

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
        .AddAsyncCheck("cosmosdb", async () =>
        {
            try
            {
                var cosmosClient = new CosmosClient(cosmosConnectionString);
                var database = cosmosClient.GetDatabase(cosmosOptions["DatabaseName"] ?? "hangfire");
                await database.ReadAsync();
                return HealthCheckResult.Healthy("Cosmos DB is accessible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Cosmos DB is not accessible", ex);
            }
        }, tags: new[] { "db", "cosmos" });

    // Register application services
    builder.Services.AddScoped<IJobTestingService, JobTestingService>();

    // Add memory cache
    builder.Services.AddMemoryCache();

    // Add HTTP client
    builder.Services.AddHttpClient();

    // Add HTTP logging services
    builder.Services.AddHttpLogging();

    var app = builder.Build();

    // Get logger instance after app is built for proper DI injection
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // Log URL configuration for SSL debugging
    logger.LogInformation("Application URLs configured: {Urls}", string.Join(", ", builder.WebHost.GetSetting("urls")?.Split(';') ?? new[] { "Not specified" }));
    logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("HTTPS Certificate validation disabled for development: {IsDevelopment}", app.Environment.IsDevelopment());

    // Configure the HTTP request pipeline
    app.UseHttpLogging();

    // Global exception handling (should be early in pipeline)
    app.UseGlobalExceptionHandling();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hangfire Cosmos Storage Provider API v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
        });
    }

    app.UseCors();
    app.UseRouting();

    // Add Hangfire Dashboard
    var dashboardOptions = new DashboardOptions
    {
        DashboardTitle = builder.Configuration["Dashboard:Title"] ?? "Hangfire Cosmos Storage Provider",
        StatsPollingInterval = int.Parse(builder.Configuration["Dashboard:StatsPollingInterval"] ?? "2000"),
        Authorization = new[] { new AllowAllAuthorizationFilter() }
    };

    app.UseHangfireDashboard("/hangfire", dashboardOptions);

    // Map controllers
    app.MapControllers();

    // Map health checks
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    exception = x.Value.Exception?.Message,
                    duration = x.Value.Duration.ToString()
                }),
                duration = report.TotalDuration.ToString()
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });

    // Root endpoint
    app.MapGet("/", () => new
    {
        Application = "Hangfire Cosmos Storage Provider - Web Application",
        Version = typeof(Program).Assembly.GetName().Version?.ToString(),
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow,
        Endpoints = new
        {
            Dashboard = "/hangfire",
            Health = "/health",
            API = "/api",
            Swagger = app.Environment.IsDevelopment() ? "/swagger" : null
        }
    });

    // Quick job enqueue endpoint for testing
    app.MapPost("/enqueue-test-job", (IJobTestingService jobService) =>
    {
        var jobId = BackgroundJob.Enqueue(() => jobService.SimpleJobAsync("Quick test job from root endpoint"));
        return new { JobId = jobId, Message = "Test job enqueued successfully" };
    });

    // Setup recurring jobs
    SetupRecurringJobs();

    Console.WriteLine("Application configured successfully, starting web host");
    app.Run();
}
catch (Exception ex)
{
    // For critical startup errors, we need to log to console since DI may not be available
    Console.WriteLine($"Application terminated unexpectedly: {ex}");
    
    // Try to get logger if DI container was built
    try
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogCritical(ex, "Application terminated unexpectedly");
    }
    catch
    {
        // If we can't create a logger, the console output above is our fallback
    }
}

static void SetupRecurringJobs()
{
    // Setup some default recurring jobs for monitoring and maintenance
    RecurringJob.AddOrUpdate(
        "system-health-check",
        () => RecurringJobHelpers.LogSystemHealthCheck(),
        Cron.Hourly);

    RecurringJob.AddOrUpdate(
        "cleanup-old-jobs",
        () => RecurringJobHelpers.LogCleanupOldJobs(),
        Cron.Daily);
}

/// <summary>
/// Helper class for recurring job methods to avoid local function issues with Hangfire expression trees
/// </summary>
public static class RecurringJobHelpers
{
    public static void LogSystemHealthCheck()
    {
        // Use a simple logger factory for recurring jobs
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("RecurringJobs");
        logger.LogInformation("System health check at {Timestamp}", DateTime.UtcNow);
    }

    public static void LogCleanupOldJobs()
    {
        // Use a simple logger factory for recurring jobs
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("RecurringJobs");
        logger.LogInformation("Cleanup old jobs at {Timestamp}", DateTime.UtcNow);
    }
}

/// <summary>
/// Authorization filter that allows all users to access Hangfire Dashboard.
/// WARNING: This is for development/testing only. In production, implement proper authorization.
/// </summary>
public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // Allow all users for development/testing
    }
}
