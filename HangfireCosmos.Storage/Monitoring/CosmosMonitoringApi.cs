using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Storage.Monitoring;

/// <summary>
/// Implements IMonitoringApi for Cosmos DB storage monitoring and dashboard functionality.
/// </summary>
public class CosmosMonitoringApi : IMonitoringApi
{
    private readonly ICosmosDocumentRepository _repository;
    private readonly CosmosStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the CosmosMonitoringApi class.
    /// </summary>
    /// <param name="repository">The document repository.</param>
    /// <param name="options">The storage options.</param>
    public CosmosMonitoringApi(ICosmosDocumentRepository repository, CosmosStorageOptions options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public StatisticsDto GetStatistics()
    {
        var stats = new StatisticsDto();

        try
        {
            // Get job counts by state - use simple queries since we can't use aggregation with BaseDocument constraint
            stats.Enqueued = GetJobCountByState("enqueued");
            stats.Processing = GetJobCountByState("processing");
            stats.Succeeded = GetJobCountByState("succeeded");
            stats.Failed = GetJobCountByState("failed");
            stats.Scheduled = GetJobCountByState("scheduled");
            stats.Deleted = GetJobCountByState("deleted");

            // Get server count
            var serverQuery = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'server'");
            
            var servers = _repository.QueryDocumentsAsync<ServerDocument>(
                _options.ServersContainerName, serverQuery, "servers").GetAwaiter().GetResult();
            
            stats.Servers = servers.Count();

            // Get queue count
            var queueQuery = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'queue'");
            
            var queues = _repository.QueryDocumentsAsync<QueueDocument>(
                _options.QueuesContainerName, queueQuery, "queues").GetAwaiter().GetResult();
            
            stats.Queues = queues.Count();
        }
        catch
        {
            // Return empty stats on error
        }

        return stats;
    }

    /// <summary>
    /// Gets the count of jobs in a specific state.
    /// </summary>
    /// <param name="state">The job state.</param>
    /// <returns>The count of jobs in the specified state.</returns>
    private long GetJobCountByState(string state)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = @state")
                .WithParameter("@state", state);
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query).GetAwaiter().GetResult();
            
            return jobs.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'enqueued' AND c.queueName = @queue ORDER BY c.createdAt OFFSET @offset LIMIT @limit")
            .WithParameter("@queue", queue)
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query, $"job:{queue}").GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, EnqueuedJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new EnqueuedJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                State = job.State,
                EnqueuedAt = job.CreatedAt,
                InEnqueuedState = job.State == "enqueued"
            };
            
            result.Add(new KeyValuePair<string, EnqueuedJobDto>(job.JobId, dto));
        }

        return new JobList<EnqueuedJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'processing' AND c.queueName = @queue ORDER BY c.updatedAt OFFSET @offset LIMIT @limit")
            .WithParameter("@queue", queue)
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query, $"job:{queue}").GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, FetchedJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new FetchedJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                State = job.State,
                FetchedAt = job.UpdatedAt
            };
            
            result.Add(new KeyValuePair<string, FetchedJobDto>(job.JobId, dto));
        }

        return new JobList<FetchedJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<ProcessingJobDto> ProcessingJobs(int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'processing' ORDER BY c.updatedAt OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, ProcessingJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new ProcessingJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                InProcessingState = job.State == "processing",
                ServerId = GetServerIdFromStateHistory(job),
                StartedAt = job.UpdatedAt
            };
            
            result.Add(new KeyValuePair<string, ProcessingJobDto>(job.JobId, dto));
        }

        return new JobList<ProcessingJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<ScheduledJobDto> ScheduledJobs(int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'scheduled' ORDER BY c.createdAt OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, ScheduledJobDto>>();
        
        foreach (var job in jobs)
        {
            var scheduledAt = GetScheduledAtFromStateData(job);
            
            var dto = new ScheduledJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                InScheduledState = job.State == "scheduled",
                EnqueueAt = scheduledAt,
                ScheduledAt = scheduledAt
            };
            
            result.Add(new KeyValuePair<string, ScheduledJobDto>(job.JobId, dto));
        }

        return new JobList<ScheduledJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<SucceededJobDto> SucceededJobs(int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'succeeded' ORDER BY c.updatedAt DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, SucceededJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new SucceededJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                InSucceededState = job.State == "succeeded",
                Result = GetResultFromStateData(job),
                TotalDuration = GetDurationFromStateHistory(job),
                SucceededAt = job.UpdatedAt
            };
            
            result.Add(new KeyValuePair<string, SucceededJobDto>(job.JobId, dto));
        }

        return new JobList<SucceededJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<FailedJobDto> FailedJobs(int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'failed' ORDER BY c.updatedAt DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, FailedJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new FailedJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                InFailedState = job.State == "failed",
                Reason = GetFailureReasonFromStateHistory(job),
                ExceptionDetails = GetExceptionDetailsFromStateData(job),
                ExceptionMessage = GetExceptionMessageFromStateData(job),
                ExceptionType = GetExceptionTypeFromStateData(job),
                FailedAt = job.UpdatedAt
            };
            
            result.Add(new KeyValuePair<string, FailedJobDto>(job.JobId, dto));
        }

        return new JobList<FailedJobDto>(result);
    }

    /// <inheritdoc />
    public JobList<DeletedJobDto> DeletedJobs(int from, int perPage)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'deleted' ORDER BY c.updatedAt DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", from)
            .WithParameter("@limit", perPage);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var result = new List<KeyValuePair<string, DeletedJobDto>>();
        
        foreach (var job in jobs)
        {
            var dto = new DeletedJobDto
            {
                Job = DeserializeJob(job.InvocationData),
                InDeletedState = job.State == "deleted",
                DeletedAt = job.UpdatedAt
            };
            
            result.Add(new KeyValuePair<string, DeletedJobDto>(job.JobId, dto));
        }

        return new JobList<DeletedJobDto>(result);
    }

    /// <inheritdoc />
    public IList<ServerDto> GetServers()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'server' ORDER BY c.startedAt");

        var servers = _repository.QueryDocumentsAsync<ServerDocument>(
            _options.ServersContainerName, query, "servers").GetAwaiter().GetResult();

        var result = new List<ServerDto>();
        
        foreach (var server in servers)
        {
            var dto = new ServerDto
            {
                Name = server.ServerId,
                Heartbeat = server.LastHeartbeat,
                Queues = server.Data.Queues,
                StartedAt = server.StartedAt,
                WorkersCount = server.Data.WorkerCount
            };
            
            result.Add(dto);
        }

        return result;
    }

    /// <inheritdoc />
    public IList<QueueWithTopEnqueuedJobsDto> Queues()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'queue' ORDER BY c.queueName");

        var queues = _repository.QueryDocumentsAsync<QueueDocument>(
            _options.QueuesContainerName, query, "queues").GetAwaiter().GetResult();

        var result = new List<QueueWithTopEnqueuedJobsDto>();
        
        foreach (var queue in queues)
        {
            // Get top jobs for this queue
            var jobsQuery = new QueryDefinition(
                "SELECT TOP 5 * FROM c WHERE c.documentType = 'job' AND c.queueName = @queueName AND c.state = 'enqueued' ORDER BY c.createdAt")
                .WithParameter("@queueName", queue.QueueName);

            var topJobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, jobsQuery, $"job:{queue.QueueName}").GetAwaiter().GetResult();

            var dto = new QueueWithTopEnqueuedJobsDto
            {
                Name = queue.QueueName,
                Length = queue.Length,
                Fetched = queue.Fetched,
                FirstJobs = new JobList<EnqueuedJobDto>(topJobs.Select(job => new KeyValuePair<string, EnqueuedJobDto>(
                    job.JobId,
                    new EnqueuedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData),
                        State = job.State,
                        EnqueuedAt = job.CreatedAt,
                        InEnqueuedState = job.State == "enqueued"
                    })).ToList())
            };
            
            result.Add(dto);
        }

        return result;
    }

    /// <inheritdoc />
    public JobDetailsDto JobDetails(string jobId)
    {
        if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException(nameof(jobId));

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.documentType = 'job' AND c.jobId = @jobId")
            .WithParameter("@jobId", jobId);

        var jobs = _repository.QueryDocumentsAsync<JobDocument>(
            _options.JobsContainerName, query).GetAwaiter().GetResult();

        var job = jobs.FirstOrDefault();
        if (job == null) return null!;

        var history = job.StateHistory.Select(h => new StateHistoryDto
        {
            StateName = h.State,
            Reason = h.Reason,
            CreatedAt = h.CreatedAt,
            Data = h.Data
        }).ToList();

        return new JobDetailsDto
        {
            CreatedAt = job.CreatedAt,
            Job = DeserializeJob(job.InvocationData),
            History = history,
            Properties = job.Parameters
        };
    }

    /// <summary>
    /// Deserializes invocation data to a job.
    /// </summary>
    /// <param name="invocationData">The invocation data.</param>
    /// <returns>The deserialized job.</returns>
    private Job DeserializeJob(Documents.InvocationData invocationData)
    {
        try
        {
            var type = Type.GetType(invocationData.Type);
            if (type == null) return null!;

            var parameterTypes = invocationData.ParameterTypes.Select(Type.GetType).ToArray();
            var method = type.GetMethod(invocationData.Method, parameterTypes!);
            if (method == null) return null!;

            var arguments = invocationData.Arguments.Select(arg => 
                Newtonsoft.Json.JsonConvert.DeserializeObject(arg)).ToArray();

            return new Job(method, arguments);
        }
        catch
        {
            return null!;
        }
    }

    /// <summary>
    /// Gets the server ID from state history.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The server ID or null.</returns>
    private string? GetServerIdFromStateHistory(JobDocument job)
    {
        var processingState = job.StateHistory.LastOrDefault(h => h.State == "processing");
        return processingState?.Data.TryGetValue("ServerId", out var serverId) == true ? serverId : null;
    }

    /// <summary>
    /// Gets the scheduled time from state data.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The scheduled time.</returns>
    private DateTime GetScheduledAtFromStateData(JobDocument job)
    {
        if (job.StateData.TryGetValue("EnqueueAt", out var enqueueAtStr) && 
            DateTime.TryParse(enqueueAtStr, out var enqueueAt))
        {
            return enqueueAt;
        }
        return job.CreatedAt;
    }

    /// <summary>
    /// Gets the result from state data.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The result or null.</returns>
    private string? GetResultFromStateData(JobDocument job)
    {
        return job.StateData.TryGetValue("Result", out var result) ? result : null;
    }

    /// <summary>
    /// Gets the duration from state history.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The duration or null.</returns>
    private long? GetDurationFromStateHistory(JobDocument job)
    {
        var processingState = job.StateHistory.FirstOrDefault(h => h.State == "processing");
        var succeededState = job.StateHistory.LastOrDefault(h => h.State == "succeeded");
        
        if (processingState != null && succeededState != null)
        {
            return (long)(succeededState.CreatedAt - processingState.CreatedAt).TotalMilliseconds;
        }
        
        return null;
    }

    /// <summary>
    /// Gets the failure reason from state history.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The failure reason or null.</returns>
    private string? GetFailureReasonFromStateHistory(JobDocument job)
    {
        return job.StateHistory.LastOrDefault(h => h.State == "failed")?.Reason;
    }

    /// <summary>
    /// Gets the exception details from state data.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The exception details or null.</returns>
    private string? GetExceptionDetailsFromStateData(JobDocument job)
    {
        return job.StateData.TryGetValue("ExceptionDetails", out var details) ? details : null;
    }

    /// <summary>
    /// Gets the exception message from state data.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The exception message or null.</returns>
    private string? GetExceptionMessageFromStateData(JobDocument job)
    {
        return job.StateData.TryGetValue("ExceptionMessage", out var message) ? message : null;
    }

    /// <summary>
    /// Gets the exception type from state data.
    /// </summary>
    /// <param name="job">The job document.</param>
    /// <returns>The exception type or null.</returns>
    private string? GetExceptionTypeFromStateData(JobDocument job)
    {
        return job.StateData.TryGetValue("ExceptionType", out var type) ? type : null;
    }

    /// <inheritdoc />
    public long EnqueuedCount(string queue)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'enqueued' AND c.queueName = @queue")
                .WithParameter("@queue", queue);
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query, $"job:{queue}").GetAwaiter().GetResult();
            
            return jobs.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public long FetchedCount(string queue)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'processing' AND c.queueName = @queue")
                .WithParameter("@queue", queue);
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query, $"job:{queue}").GetAwaiter().GetResult();
            
            return jobs.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public long ScheduledCount()
    {
        return GetJobCountByState("scheduled");
    }

    /// <inheritdoc />
    public long ProcessingCount()
    {
        return GetJobCountByState("processing");
    }

    /// <inheritdoc />
    public long SucceededListCount()
    {
        return GetJobCountByState("succeeded");
    }

    /// <inheritdoc />
    public long FailedCount()
    {
        return GetJobCountByState("failed");
    }

    /// <inheritdoc />
    public long DeletedListCount()
    {
        return GetJobCountByState("deleted");
    }

    /// <inheritdoc />
    public IDictionary<DateTime, long> SucceededByDatesCount()
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'succeeded' ORDER BY c.updatedAt DESC");
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query).GetAwaiter().GetResult();
            
            return jobs
                .GroupBy(j => j.UpdatedAt.Date)
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        catch
        {
            return new Dictionary<DateTime, long>();
        }
    }

    /// <inheritdoc />
    public IDictionary<DateTime, long> FailedByDatesCount()
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'failed' ORDER BY c.updatedAt DESC");
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query).GetAwaiter().GetResult();
            
            return jobs
                .GroupBy(j => j.UpdatedAt.Date)
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        catch
        {
            return new Dictionary<DateTime, long>();
        }
    }

    /// <inheritdoc />
    public IDictionary<DateTime, long> HourlySucceededJobs()
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'succeeded' ORDER BY c.updatedAt DESC");
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query).GetAwaiter().GetResult();
            
            return jobs
                .GroupBy(j => new DateTime(j.UpdatedAt.Year, j.UpdatedAt.Month, j.UpdatedAt.Day, j.UpdatedAt.Hour, 0, 0))
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        catch
        {
            return new Dictionary<DateTime, long>();
        }
    }

    /// <inheritdoc />
    public IDictionary<DateTime, long> HourlyFailedJobs()
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'job' AND c.state = 'failed' ORDER BY c.updatedAt DESC");
            
            var jobs = _repository.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName, query).GetAwaiter().GetResult();
            
            return jobs
                .GroupBy(j => new DateTime(j.UpdatedAt.Year, j.UpdatedAt.Month, j.UpdatedAt.Day, j.UpdatedAt.Hour, 0, 0))
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        catch
        {
            return new Dictionary<DateTime, long>();
        }
    }

    /// <inheritdoc />
    public IList<ServerDto> Servers()
    {
        return GetServers();
    }
}