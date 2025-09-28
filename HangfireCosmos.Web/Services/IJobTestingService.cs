using Hangfire;

namespace HangfireCosmos.Web.Services;

/// <summary>
/// Interface for job testing service that provides various job scenarios for testing the Cosmos storage provider
/// </summary>
public interface IJobTestingService
{
    /// <summary>
    /// Simple job that completes quickly
    /// </summary>
    Task SimpleJobAsync(string message);

    /// <summary>
    /// Job that takes a specified amount of time to complete
    /// </summary>
    Task LongRunningJobAsync(int durationInSeconds, string jobName);

    /// <summary>
    /// Job that intentionally fails for testing error handling
    /// </summary>
    Task FailingJobAsync(string errorMessage);

    /// <summary>
    /// Job that processes data and returns results
    /// </summary>
    Task<string> DataProcessingJobAsync(int itemCount);

    /// <summary>
    /// Job that demonstrates progress reporting
    /// </summary>
    Task ProgressReportingJobAsync(int totalSteps, IJobCancellationToken cancellationToken);

    /// <summary>
    /// Job that performs I/O operations
    /// </summary>
    Task IoIntensiveJobAsync(string fileName, int operationCount);

    /// <summary>
    /// Job that demonstrates memory usage patterns
    /// </summary>
    Task MemoryIntensiveJobAsync(int memorySizeMB);

    /// <summary>
    /// Recurring job that runs periodically
    /// </summary>
    Task RecurringMaintenanceJobAsync();

    /// <summary>
    /// Job that creates continuation jobs
    /// </summary>
    Task ParentJobAsync(string childJobData);

    /// <summary>
    /// Child job that runs after parent completes
    /// </summary>
    Task ChildJobAsync(string data, string parentJobId);

    /// <summary>
    /// Batch job that processes multiple items
    /// </summary>
    Task BatchItemProcessingJobAsync(int batchId, int itemId, string itemData);
}