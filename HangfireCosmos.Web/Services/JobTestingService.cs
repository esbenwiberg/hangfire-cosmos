using Hangfire;
using System.Text;

namespace HangfireCosmos.Web.Services;

/// <summary>
/// Implementation of job testing service that provides various job scenarios for testing the Cosmos storage provider
/// </summary>
public class JobTestingService : IJobTestingService
{
    private readonly ILogger<JobTestingService> _logger;

    public JobTestingService(ILogger<JobTestingService> logger)
    {
        _logger = logger;
    }

    public async Task SimpleJobAsync(string message)
    {
        _logger.LogInformation("Starting simple job with message: {Message}", message);
        
        // Simulate some work
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        _logger.LogInformation("Simple job completed successfully. Message was: {Message}", message);
    }

    public async Task LongRunningJobAsync(int durationInSeconds, string jobName)
    {
        _logger.LogInformation("Starting long-running job '{JobName}' for {Duration} seconds", jobName, durationInSeconds);
        
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationInSeconds);
        
        while (DateTime.UtcNow < endTime)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            var elapsed = DateTime.UtcNow - startTime;
            var remaining = endTime - DateTime.UtcNow;
            
            _logger.LogInformation("Long-running job '{JobName}' progress: {Elapsed:mm\\:ss} elapsed, {Remaining:mm\\:ss} remaining", 
                jobName, elapsed, remaining);
        }
        
        _logger.LogInformation("Long-running job '{JobName}' completed after {Duration} seconds", jobName, durationInSeconds);
    }

    public async Task FailingJobAsync(string errorMessage)
    {
        _logger.LogInformation("Starting failing job with error message: {ErrorMessage}", errorMessage);
        
        // Simulate some work before failing
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        _logger.LogError("Job is about to fail with message: {ErrorMessage}", errorMessage);
        throw new InvalidOperationException($"Intentional job failure: {errorMessage}");
    }

    public async Task<string> DataProcessingJobAsync(int itemCount)
    {
        _logger.LogInformation("Starting data processing job for {ItemCount} items", itemCount);
        
        var results = new StringBuilder();
        var processedCount = 0;
        
        for (int i = 1; i <= itemCount; i++)
        {
            // Simulate processing each item
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            
            var itemResult = $"Item_{i}_Processed_At_{DateTime.UtcNow:HH:mm:ss.fff}";
            results.AppendLine(itemResult);
            processedCount++;
            
            if (i % 10 == 0)
            {
                _logger.LogInformation("Data processing job progress: {Processed}/{Total} items processed", i, itemCount);
            }
        }
        
        var finalResult = $"Processed {processedCount} items successfully.\n{results}";
        _logger.LogInformation("Data processing job completed. Processed {ProcessedCount} items", processedCount);
        
        return finalResult;
    }

    public async Task ProgressReportingJobAsync(int totalSteps, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting progress reporting job with {TotalSteps} steps", totalSteps);
        
        for (int step = 1; step <= totalSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Simulate work for each step
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken.ShutdownToken);
            
            var progressPercentage = (step * 100) / totalSteps;
            _logger.LogInformation("Progress reporting job: Step {Step}/{TotalSteps} completed ({Progress}%)", 
                step, totalSteps, progressPercentage);
        }
        
        _logger.LogInformation("Progress reporting job completed all {TotalSteps} steps", totalSteps);
    }

    public async Task IoIntensiveJobAsync(string fileName, int operationCount)
    {
        _logger.LogInformation("Starting I/O intensive job with file '{FileName}' and {OperationCount} operations", 
            fileName, operationCount);
        
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{fileName}_{Guid.NewGuid()}.tmp");
        
        try
        {
            // Write operations
            for (int i = 1; i <= operationCount; i++)
            {
                var content = $"Operation {i} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n";
                await File.AppendAllTextAsync(tempFilePath, content);
                
                if (i % 100 == 0)
                {
                    _logger.LogInformation("I/O intensive job: {Completed}/{Total} write operations completed", i, operationCount);
                }
            }
            
            // Read operations
            var fileContent = await File.ReadAllTextAsync(tempFilePath);
            var lineCount = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            
            _logger.LogInformation("I/O intensive job completed. File '{FileName}' has {LineCount} lines", fileName, lineCount);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                _logger.LogInformation("Temporary file '{TempFilePath}' cleaned up", tempFilePath);
            }
        }
    }

    public async Task MemoryIntensiveJobAsync(int memorySizeMB)
    {
        _logger.LogInformation("Starting memory intensive job with {MemorySize} MB allocation", memorySizeMB);
        
        var initialMemory = GC.GetTotalMemory(false);
        _logger.LogInformation("Initial memory usage: {InitialMemory:N0} bytes", initialMemory);
        
        // Allocate memory
        var arrays = new List<byte[]>();
        var bytesPerMB = 1024 * 1024;
        
        for (int i = 0; i < memorySizeMB; i++)
        {
            var array = new byte[bytesPerMB];
            // Fill with some data to ensure allocation
            for (int j = 0; j < array.Length; j += 1024)
            {
                array[j] = (byte)(i % 256);
            }
            arrays.Add(array);
            
            await Task.Delay(TimeSpan.FromMilliseconds(10)); // Small delay to allow monitoring
            
            if ((i + 1) % 10 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                _logger.LogInformation("Memory intensive job: Allocated {Allocated} MB, current memory: {CurrentMemory:N0} bytes", 
                    i + 1, currentMemory);
            }
        }
        
        var peakMemory = GC.GetTotalMemory(false);
        _logger.LogInformation("Peak memory usage: {PeakMemory:N0} bytes", peakMemory);
        
        // Hold memory for a bit
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Clear references and force GC
        arrays.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        _logger.LogInformation("Memory intensive job completed. Final memory: {FinalMemory:N0} bytes", finalMemory);
    }

    public async Task RecurringMaintenanceJobAsync()
    {
        _logger.LogInformation("Starting recurring maintenance job at {Timestamp}", DateTime.UtcNow);
        
        // Simulate maintenance tasks
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        var memoryBefore = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);
        
        _logger.LogInformation("Recurring maintenance job completed. Memory before: {MemoryBefore:N0}, after: {MemoryAfter:N0}", 
            memoryBefore, memoryAfter);
    }

    public async Task ParentJobAsync(string childJobData)
    {
        _logger.LogInformation("Starting parent job with child data: {ChildJobData}", childJobData);
        
        // Simulate parent job work
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        // Create continuation job
        var parentJobId = JobStorage.Current.GetConnection().GetJobData("current")?.Job?.Args?.FirstOrDefault()?.ToString() ?? "unknown";
        
        BackgroundJob.ContinueJobWith(
            parentJobId,
            () => ChildJobAsync(childJobData, parentJobId));
        
        _logger.LogInformation("Parent job completed and scheduled child job with data: {ChildJobData}", childJobData);
    }

    public async Task ChildJobAsync(string data, string parentJobId)
    {
        _logger.LogInformation("Starting child job with data: {Data}, parent job ID: {ParentJobId}", data, parentJobId);
        
        // Simulate child job work
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        _logger.LogInformation("Child job completed. Processed data: {Data} from parent: {ParentJobId}", data, parentJobId);
    }

    public async Task BatchItemProcessingJobAsync(int batchId, int itemId, string itemData)
    {
        _logger.LogInformation("Starting batch item processing for batch {BatchId}, item {ItemId}: {ItemData}", 
            batchId, itemId, itemData);
        
        // Simulate item processing
        await Task.Delay(TimeSpan.FromMilliseconds(500 + (itemId % 1000))); // Variable processing time
        
        // Simulate some processing logic
        var processedData = $"PROCESSED_{itemData.ToUpperInvariant()}_{DateTime.UtcNow:HHmmss}";
        
        _logger.LogInformation("Batch item processing completed for batch {BatchId}, item {ItemId}. Result: {ProcessedData}", 
            batchId, itemId, processedData);
    }
}