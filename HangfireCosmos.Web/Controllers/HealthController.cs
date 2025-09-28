using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hangfire;
using HangfireCosmos.Storage;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;

namespace HangfireCosmos.Web.Controllers;

/// <summary>
/// Controller for health checks and system monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;
    private readonly IConfiguration _configuration;

    public HealthController(
        HealthCheckService healthCheckService, 
        ILogger<HealthController> logger,
        IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get overall health status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();
        
        var response = new
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
            Checks = healthReport.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration.TotalMilliseconds,
                Description = entry.Value.Description,
                Data = entry.Value.Data,
                Exception = entry.Value.Exception?.Message
            })
        };

        var statusCode = healthReport.Status switch
        {
            HealthStatus.Healthy => 200,
            HealthStatus.Degraded => 200,
            HealthStatus.Unhealthy => 503,
            _ => 500
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Get detailed system information
    /// </summary>
    [HttpGet("system")]
    public IActionResult GetSystemInfo()
    {
        var systemInfo = new
        {
            Environment = new
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                CLRVersion = Environment.Version.ToString(),
                CurrentDirectory = Environment.CurrentDirectory
            },
            Memory = new
            {
                TotalMemory = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            },
            Application = new
            {
                StartTime = Process.GetCurrentProcess().StartTime,
                Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                ThreadCount = Process.GetCurrentProcess().Threads.Count,
                HandleCount = Process.GetCurrentProcess().HandleCount
            },
            Configuration = new
            {
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"],
                CosmosDatabase = _configuration["HangfireCosmosOptions:DatabaseName"],
                DefaultThroughput = _configuration["HangfireCosmosOptions:DefaultThroughput"],
                AutoCreateDatabase = _configuration["HangfireCosmosOptions:AutoCreateDatabase"],
                JobTestingEnabled = _configuration["JobTesting:EnableTestJobs"]
            }
        };

        return Ok(systemInfo);
    }

    /// <summary>
    /// Get Hangfire statistics
    /// </summary>
    [HttpGet("hangfire")]
    public IActionResult GetHangfireStats()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            
            var stats = new
            {
                Overview = new
                {
                    EnqueuedCount = monitoringApi.EnqueuedCount("default"),
                    FailedCount = monitoringApi.FailedCount(),
                    ProcessingCount = monitoringApi.ProcessingCount(),
                    ScheduledCount = monitoringApi.ScheduledCount(),
                    SucceededCount = monitoringApi.SucceededJobs(0, 1).Count,
                    DeletedCount = monitoringApi.DeletedJobs(0, 1).Count,
                    RecurringJobCount = monitoringApi.Servers().SelectMany(s => s.Queues).Count()
                },
                Servers = monitoringApi.Servers().Select(server => new
                {
                    Name = server.Name,
                    Heartbeat = server.Heartbeat,
                    Queues = server.Queues,
                    StartedAt = server.StartedAt,
                    WorkersCount = server.WorkersCount
                }),
                Queues = monitoringApi.Queues().Select(queue => new
                {
                    Name = queue.Name,
                    Length = queue.Length,
                    Fetched = queue.Fetched
                }),
                RecentJobs = new
                {
                    Processing = monitoringApi.ProcessingJobs(0, 10),
                    Scheduled = monitoringApi.ScheduledJobs(0, 10),
                    Succeeded = monitoringApi.SucceededJobs(0, 10),
                    Failed = monitoringApi.FailedJobs(0, 10),
                    Deleted = monitoringApi.DeletedJobs(0, 10)
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Hangfire statistics");
            return StatusCode(500, new { Error = "Failed to retrieve Hangfire statistics", Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Cosmos DB connection status
    /// </summary>
    [HttpGet("cosmos")]
    public async Task<IActionResult> GetCosmosStatus()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("CosmosDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                return BadRequest(new { Error = "Cosmos DB connection string not configured" });
            }

            var cosmosClient = new CosmosClient(connectionString);
            var databaseName = _configuration["HangfireCosmosOptions:DatabaseName"] ?? "hangfire";
            
            // Test connection by trying to read database properties
            var database = cosmosClient.GetDatabase(databaseName);
            var response = await database.ReadAsync();
            
            var status = new
            {
                Status = "Connected",
                DatabaseName = databaseName,
                AccountEndpoint = cosmosClient.Endpoint.ToString(),
                // Throughput information not directly available from DatabaseProperties
                LastModified = response.Resource?.LastModified,
                ETag = response.ETag,
                RequestCharge = response.RequestCharge,
                ActivityId = response.ActivityId
            };

            return Ok(status);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB connection error");
            return StatusCode(503, new 
            { 
                Status = "Error", 
                Error = ex.Message, 
                StatusCode = ex.StatusCode,
                ActivityId = ex.ActivityId,
                RequestCharge = ex.RequestCharge
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking Cosmos DB status");
            return StatusCode(500, new { Status = "Error", Error = ex.Message });
        }
    }

    /// <summary>
    /// Test Cosmos DB operations
    /// </summary>
    [HttpPost("cosmos/test")]
    public async Task<IActionResult> TestCosmosOperations()
    {
        try
        {
            var testResults = new List<object>();
            var connectionString = _configuration.GetConnectionString("CosmosDb");
            var databaseName = _configuration["HangfireCosmosOptions:DatabaseName"] ?? "hangfire";
            
            var cosmosClient = new CosmosClient(connectionString);
            var database = cosmosClient.GetDatabase(databaseName);
            
            // Test 1: List containers
            var containerIterator = database.GetContainerQueryIterator<ContainerProperties>();
            var containers = new List<string>();
            while (containerIterator.HasMoreResults)
            {
                var response = await containerIterator.ReadNextAsync();
                containers.AddRange(response.Select(c => c.Id));
            }
            
            testResults.Add(new
            {
                Test = "List Containers",
                Status = "Success",
                Result = containers,
                Count = containers.Count
            });

            // Test 2: Test a simple query on jobs container (if it exists)
            if (containers.Contains("jobs"))
            {
                var jobsContainer = database.GetContainer("jobs");
                var query = "SELECT TOP 5 * FROM c";
                var queryIterator = jobsContainer.GetItemQueryIterator<dynamic>(query);
                
                var jobCount = 0;
                while (queryIterator.HasMoreResults && jobCount < 5)
                {
                    var response = await queryIterator.ReadNextAsync();
                    jobCount += response.Count();
                }
                
                testResults.Add(new
                {
                    Test = "Query Jobs Container",
                    Status = "Success",
                    Result = $"Retrieved {jobCount} job documents"
                });
            }

            return Ok(new
            {
                Status = "Success",
                DatabaseName = databaseName,
                TestResults = testResults,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Cosmos DB operations");
            return StatusCode(500, new 
            { 
                Status = "Error", 
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get application metrics
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        
        var metrics = new
        {
            Timestamp = DateTime.UtcNow,
            Process = new
            {
                Id = process.Id,
                Name = process.ProcessName,
                StartTime = process.StartTime,
                TotalProcessorTime = process.TotalProcessorTime,
                UserProcessorTime = process.UserProcessorTime,
                PrivilegedProcessorTime = process.PrivilegedProcessorTime,
                WorkingSet64 = process.WorkingSet64,
                VirtualMemorySize64 = process.VirtualMemorySize64,
                PrivateMemorySize64 = process.PrivateMemorySize64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            },
            GarbageCollection = new
            {
                TotalMemory = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false)
            },
            Threading = new
            {
                ThreadPoolThreads = ThreadPool.ThreadCount,
                CompletedWorkItems = ThreadPool.CompletedWorkItemCount,
                PendingWorkItems = ThreadPool.PendingWorkItemCount
            }
        };

        return Ok(metrics);
    }
}