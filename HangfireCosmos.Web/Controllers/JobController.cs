using Hangfire;
using HangfireCosmos.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HangfireCosmos.Web.Controllers;

/// <summary>
/// Controller for managing and testing various Hangfire job scenarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class JobController : ControllerBase
{
    private readonly IJobTestingService _jobTestingService;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobTestingService jobTestingService, ILogger<JobController> logger)
    {
        _jobTestingService = jobTestingService;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a simple fire-and-forget job
    /// </summary>
    [HttpPost("simple")]
    public IActionResult EnqueueSimpleJob([FromBody] SimpleJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.SimpleJobAsync(request.Message));
        
        _logger.LogInformation("Enqueued simple job with ID: {JobId}, Message: {Message}", jobId, request.Message);
        
        return Ok(new { JobId = jobId, Message = "Simple job enqueued successfully" });
    }

    /// <summary>
    /// Schedule a delayed job
    /// </summary>
    [HttpPost("delayed")]
    public IActionResult ScheduleDelayedJob([FromBody] DelayedJobRequest request)
    {
        var delay = TimeSpan.FromSeconds(request.DelayInSeconds);
        var jobId = BackgroundJob.Schedule(() => _jobTestingService.SimpleJobAsync(request.Message), delay);
        
        _logger.LogInformation("Scheduled delayed job with ID: {JobId}, Delay: {Delay} seconds", jobId, request.DelayInSeconds);
        
        return Ok(new { JobId = jobId, Message = $"Job scheduled to run in {request.DelayInSeconds} seconds" });
    }

    /// <summary>
    /// Create or update a recurring job
    /// </summary>
    [HttpPost("recurring")]
    public IActionResult CreateRecurringJob([FromBody] RecurringJobRequest request)
    {
        RecurringJob.AddOrUpdate(
            request.JobId,
            () => _jobTestingService.RecurringMaintenanceJobAsync(),
            request.CronExpression);
        
        _logger.LogInformation("Created/updated recurring job with ID: {JobId}, Cron: {CronExpression}", 
            request.JobId, request.CronExpression);
        
        return Ok(new { JobId = request.JobId, Message = "Recurring job created/updated successfully" });
    }

    /// <summary>
    /// Enqueue a long-running job
    /// </summary>
    [HttpPost("long-running")]
    public IActionResult EnqueueLongRunningJob([FromBody] LongRunningJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.LongRunningJobAsync(request.DurationInSeconds, request.JobName));
        
        _logger.LogInformation("Enqueued long-running job with ID: {JobId}, Duration: {Duration} seconds", 
            jobId, request.DurationInSeconds);
        
        return Ok(new { JobId = jobId, Message = $"Long-running job enqueued for {request.DurationInSeconds} seconds" });
    }

    /// <summary>
    /// Enqueue a job that will fail
    /// </summary>
    [HttpPost("failing")]
    public IActionResult EnqueueFailingJob([FromBody] FailingJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.FailingJobAsync(request.ErrorMessage));
        
        _logger.LogInformation("Enqueued failing job with ID: {JobId}, Error: {ErrorMessage}", jobId, request.ErrorMessage);
        
        return Ok(new { JobId = jobId, Message = "Failing job enqueued successfully" });
    }

    /// <summary>
    /// Enqueue a data processing job
    /// </summary>
    [HttpPost("data-processing")]
    public IActionResult EnqueueDataProcessingJob([FromBody] DataProcessingJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.DataProcessingJobAsync(request.ItemCount));
        
        _logger.LogInformation("Enqueued data processing job with ID: {JobId}, Items: {ItemCount}", jobId, request.ItemCount);
        
        return Ok(new { JobId = jobId, Message = $"Data processing job enqueued for {request.ItemCount} items" });
    }

    /// <summary>
    /// Enqueue a job with progress reporting
    /// </summary>
    [HttpPost("progress-reporting")]
    public IActionResult EnqueueProgressReportingJob([FromBody] ProgressReportingJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.ProgressReportingJobAsync(request.TotalSteps, JobCancellationToken.Null));
        
        _logger.LogInformation("Enqueued progress reporting job with ID: {JobId}, Steps: {TotalSteps}", jobId, request.TotalSteps);
        
        return Ok(new { JobId = jobId, Message = $"Progress reporting job enqueued with {request.TotalSteps} steps" });
    }

    /// <summary>
    /// Enqueue an I/O intensive job
    /// </summary>
    [HttpPost("io-intensive")]
    public IActionResult EnqueueIoIntensiveJob([FromBody] IoIntensiveJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.IoIntensiveJobAsync(request.FileName, request.OperationCount));
        
        _logger.LogInformation("Enqueued I/O intensive job with ID: {JobId}, File: {FileName}, Operations: {OperationCount}", 
            jobId, request.FileName, request.OperationCount);
        
        return Ok(new { JobId = jobId, Message = $"I/O intensive job enqueued with {request.OperationCount} operations" });
    }

    /// <summary>
    /// Enqueue a memory intensive job
    /// </summary>
    [HttpPost("memory-intensive")]
    public IActionResult EnqueueMemoryIntensiveJob([FromBody] MemoryIntensiveJobRequest request)
    {
        var jobId = BackgroundJob.Enqueue(() => _jobTestingService.MemoryIntensiveJobAsync(request.MemorySizeMB));
        
        _logger.LogInformation("Enqueued memory intensive job with ID: {JobId}, Memory: {MemorySize} MB", 
            jobId, request.MemorySizeMB);
        
        return Ok(new { JobId = jobId, Message = $"Memory intensive job enqueued for {request.MemorySizeMB} MB" });
    }

    /// <summary>
    /// Enqueue a parent job that creates continuation jobs
    /// </summary>
    [HttpPost("continuation")]
    public IActionResult EnqueueContinuationJob([FromBody] ContinuationJobRequest request)
    {
        var parentJobId = BackgroundJob.Enqueue(() => _jobTestingService.ParentJobAsync(request.ChildJobData));
        
        _logger.LogInformation("Enqueued parent job with ID: {JobId}, Child data: {ChildJobData}", 
            parentJobId, request.ChildJobData);
        
        return Ok(new { JobId = parentJobId, Message = "Parent job enqueued, child job will be created on completion" });
    }

    /// <summary>
    /// Enqueue multiple batch jobs
    /// </summary>
    [HttpPost("batch")]
    public IActionResult EnqueueBatchJobs([FromBody] BatchJobRequest request)
    {
        var jobIds = new List<string>();
        var batchId = Random.Shared.Next(1000, 9999);
        
        for (int i = 1; i <= request.ItemCount; i++)
        {
            var itemData = $"{request.DataPrefix}_{i}";
            var jobId = BackgroundJob.Enqueue(() => _jobTestingService.BatchItemProcessingJobAsync(batchId, i, itemData));
            jobIds.Add(jobId);
        }
        
        _logger.LogInformation("Enqueued batch of {JobCount} jobs with batch ID: {BatchId}", jobIds.Count, batchId);
        
        return Ok(new { 
            BatchId = batchId, 
            JobIds = jobIds, 
            Message = $"Batch of {request.ItemCount} jobs enqueued successfully" 
        });
    }

    /// <summary>
    /// Delete a recurring job
    /// </summary>
    [HttpDelete("recurring/{jobId}")]
    public IActionResult DeleteRecurringJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
        
        _logger.LogInformation("Deleted recurring job with ID: {JobId}", jobId);
        
        return Ok(new { Message = $"Recurring job '{jobId}' deleted successfully" });
    }

    /// <summary>
    /// Delete a background job
    /// </summary>
    [HttpDelete("{jobId}")]
    public IActionResult DeleteJob(string jobId)
    {
        var result = BackgroundJob.Delete(jobId);
        
        _logger.LogInformation("Attempted to delete job with ID: {JobId}, Result: {Result}", jobId, result);
        
        return Ok(new { Message = $"Job '{jobId}' deletion attempted", Success = result });
    }

    /// <summary>
    /// Requeue a failed job
    /// </summary>
    [HttpPost("{jobId}/requeue")]
    public IActionResult RequeueJob(string jobId)
    {
        var result = BackgroundJob.Requeue(jobId);
        
        _logger.LogInformation("Attempted to requeue job with ID: {JobId}, Result: {Result}", jobId, result);
        
        return Ok(new { Message = $"Job '{jobId}' requeue attempted", Success = result });
    }
}

// Request DTOs
public record SimpleJobRequest(string Message);
public record DelayedJobRequest(string Message, int DelayInSeconds);
public record RecurringJobRequest(string JobId, string CronExpression);
public record LongRunningJobRequest(string JobName, int DurationInSeconds);
public record FailingJobRequest(string ErrorMessage);
public record DataProcessingJobRequest(int ItemCount);
public record ProgressReportingJobRequest(int TotalSteps);
public record IoIntensiveJobRequest(string FileName, int OperationCount);
public record MemoryIntensiveJobRequest(int MemorySizeMB);
public record ContinuationJobRequest(string ChildJobData);
public record BatchJobRequest(string DataPrefix, int ItemCount);