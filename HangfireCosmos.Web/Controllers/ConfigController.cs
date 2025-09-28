using Microsoft.AspNetCore.Mvc;
using HangfireCosmos.Storage;
using Hangfire;
using System.Collections;

namespace HangfireCosmos.Web.Controllers;

/// <summary>
/// Controller for configuration management and status display
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ConfigController(
        IConfiguration configuration, 
        ILogger<ConfigController> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get current application configuration
    /// </summary>
    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var config = new
        {
            Environment = new
            {
                Name = _environment.EnvironmentName,
                IsDevelopment = _environment.IsDevelopment(),
                IsProduction = _environment.IsProduction(),
                IsStaging = _environment.IsStaging(),
                ContentRootPath = _environment.ContentRootPath,
                WebRootPath = _environment.WebRootPath
            },
            Hangfire = new
            {
                CosmosOptions = new
                {
                    DatabaseName = _configuration["HangfireCosmosOptions:DatabaseName"],
                    DefaultThroughput = _configuration["HangfireCosmosOptions:DefaultThroughput"],
                    AutoCreateDatabase = _configuration["HangfireCosmosOptions:AutoCreateDatabase"],
                    AutoCreateContainers = _configuration["HangfireCosmosOptions:AutoCreateContainers"],
                    DefaultJobExpiration = _configuration["HangfireCosmosOptions:DefaultJobExpiration"],
                    ServerTimeout = _configuration["HangfireCosmosOptions:ServerTimeout"],
                    LockTimeout = _configuration["HangfireCosmosOptions:LockTimeout"],
                    MaxRetryAttempts = _configuration["HangfireCosmosOptions:MaxRetryAttempts"],
                    RetryDelay = _configuration["HangfireCosmosOptions:RetryDelay"]
                },
                Dashboard = new
                {
                    Title = _configuration["Dashboard:Title"],
                    StatsPollingInterval = _configuration["Dashboard:StatsPollingInterval"],
                    AllowAnonymous = _configuration["Dashboard:Authorization:AllowAnonymous"]
                }
            },
            JobTesting = new
            {
                EnableTestJobs = _configuration["JobTesting:EnableTestJobs"],
                MaxConcurrentJobs = _configuration["JobTesting:MaxConcurrentJobs"],
                DefaultJobTimeout = _configuration["JobTesting:DefaultJobTimeout"],
                EnableFailureSimulation = _configuration["JobTesting:EnableFailureSimulation"],
                EnableLongRunningJobs = _configuration["JobTesting:EnableLongRunningJobs"]
            },
            HealthChecks = new
            {
                Enabled = _configuration["HealthChecks:Enabled"],
                DetailedErrors = _configuration["HealthChecks:DetailedErrors"]
            },
            Logging = new
            {
                LogLevel = _configuration.GetSection("Logging:LogLevel").GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value),
            }
        };

        return Ok(config);
    }

    /// <summary>
    /// Get connection strings (masked for security)
    /// </summary>
    [HttpGet("connections")]
    public IActionResult GetConnectionStrings()
    {
        var connections = new Dictionary<string, object>();
        
        var connectionStringsSection = _configuration.GetSection("ConnectionStrings");
        foreach (var connection in connectionStringsSection.GetChildren())
        {
            var value = connection.Value;
            if (!string.IsNullOrEmpty(value))
            {
                // Mask sensitive information
                if (value.Contains("AccountKey="))
                {
                    var parts = value.Split(';');
                    var maskedParts = parts.Select(part =>
                    {
                        if (part.StartsWith("AccountKey="))
                        {
                            return "AccountKey=***MASKED***";
                        }
                        return part;
                    });
                    value = string.Join(';', maskedParts);
                }
                
                connections[connection.Key] = new
                {
                    Value = value,
                    IsMasked = true
                };
            }
        }

        return Ok(connections);
    }

    /// <summary>
    /// Get Hangfire storage information
    /// </summary>
    [HttpGet("storage")]
    public IActionResult GetStorageInfo()
    {
        try
        {
            var storage = JobStorage.Current;
            var storageInfo = new
            {
                StorageType = storage.GetType().Name,
                StorageAssembly = storage.GetType().Assembly.GetName().Name,
                StorageVersion = storage.GetType().Assembly.GetName().Version?.ToString(),
                Connection = new
                {
                    CanConnect = true,
                    LastChecked = DateTime.UtcNow
                }
            };

            return Ok(storageInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage information");
            return StatusCode(500, new 
            { 
                Error = "Failed to retrieve storage information", 
                Message = ex.Message 
            });
        }
    }

    /// <summary>
    /// Get application information
    /// </summary>
    [HttpGet("application")]
    public IActionResult GetApplicationInfo()
    {
        var appInfo = new
        {
            Name = "Hangfire Cosmos Storage Provider - Web Application",
            Version = GetType().Assembly.GetName().Version?.ToString(),
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            OSArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime,
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            CommandLineArgs = Environment.GetCommandLineArgs()
        };

        return Ok(appInfo);
    }

    /// <summary>
    /// Validate current configuration
    /// </summary>
    [HttpGet("validate")]
    public IActionResult ValidateConfiguration()
    {
        var validationResults = new List<object>();

        // Validate Cosmos DB connection string
        var cosmosConnectionString = _configuration.GetConnectionString("CosmosDb");
        validationResults.Add(new
        {
            Component = "CosmosDB Connection String",
            IsValid = !string.IsNullOrEmpty(cosmosConnectionString),
            Message = string.IsNullOrEmpty(cosmosConnectionString) 
                ? "Connection string is missing" 
                : "Connection string is configured"
        });

        // Validate Hangfire Cosmos options
        var databaseName = _configuration["HangfireCosmosOptions:DatabaseName"];
        validationResults.Add(new
        {
            Component = "Hangfire Database Name",
            IsValid = !string.IsNullOrEmpty(databaseName),
            Message = string.IsNullOrEmpty(databaseName) 
                ? "Database name is not configured" 
                : $"Database name: {databaseName}"
        });

        // Validate throughput setting
        var throughputStr = _configuration["HangfireCosmosOptions:DefaultThroughput"];
        var throughputValid = int.TryParse(throughputStr, out var throughput) && throughput >= 400;
        validationResults.Add(new
        {
            Component = "Default Throughput",
            IsValid = throughputValid,
            Message = throughputValid 
                ? $"Throughput: {throughput} RU/s" 
                : "Invalid throughput setting (minimum 400 RU/s required)"
        });

        // Validate job testing configuration
        var jobTestingEnabled = _configuration.GetValue<bool>("JobTesting:EnableTestJobs");
        validationResults.Add(new
        {
            Component = "Job Testing",
            IsValid = true,
            Message = jobTestingEnabled ? "Job testing is enabled" : "Job testing is disabled"
        });

        // Validate health checks
        var healthChecksEnabled = _configuration.GetValue<bool>("HealthChecks:Enabled");
        validationResults.Add(new
        {
            Component = "Health Checks",
            IsValid = true,
            Message = healthChecksEnabled ? "Health checks are enabled" : "Health checks are disabled"
        });

        var overallValid = validationResults.All(r => (bool)r.GetType().GetProperty("IsValid")!.GetValue(r)!);

        return Ok(new
        {
            IsValid = overallValid,
            ValidationResults = validationResults,
            ValidatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get environment variables (filtered for security)
    /// </summary>
    [HttpGet("environment")]
    public IActionResult GetEnvironmentVariables()
    {
        var envVars = new Dictionary<string, string>();
        
        // Only include safe environment variables
        var safeVarPrefixes = new[] 
        { 
            "ASPNETCORE_", 
            "DOTNET_", 
            "PATH", 
            "TEMP", 
            "TMP", 
            "COMPUTERNAME", 
            "PROCESSOR_", 
            "NUMBER_OF_PROCESSORS",
            "OS",
            "PATHEXT"
        };

        foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            var key = envVar.Key?.ToString();
            var value = envVar.Value?.ToString();
            
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                if (safeVarPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    envVars[key] = value;
                }
            }
        }

        return Ok(new
        {
            EnvironmentVariables = envVars,
            Count = envVars.Count,
            Note = "Only safe environment variables are displayed"
        });
    }

    /// <summary>
    /// Get feature flags and toggles
    /// </summary>
    [HttpGet("features")]
    public IActionResult GetFeatureFlags()
    {
        var features = new
        {
            JobTesting = new
            {
                Enabled = _configuration.GetValue<bool>("JobTesting:EnableTestJobs"),
                FailureSimulation = _configuration.GetValue<bool>("JobTesting:EnableFailureSimulation"),
                LongRunningJobs = _configuration.GetValue<bool>("JobTesting:EnableLongRunningJobs")
            },
            HealthChecks = new
            {
                Enabled = _configuration.GetValue<bool>("HealthChecks:Enabled"),
                DetailedErrors = _configuration.GetValue<bool>("HealthChecks:DetailedErrors")
            },
            Dashboard = new
            {
                AllowAnonymous = _configuration.GetValue<bool>("Dashboard:Authorization:AllowAnonymous")
            },
            Cosmos = new
            {
                AutoCreateDatabase = _configuration.GetValue<bool>("HangfireCosmosOptions:AutoCreateDatabase"),
                AutoCreateContainers = _configuration.GetValue<bool>("HangfireCosmosOptions:AutoCreateContainers")
            }
        };

        return Ok(features);
    }
}