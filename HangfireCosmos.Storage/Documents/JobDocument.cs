using Hangfire;
using Hangfire.States;
using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a job document in the Jobs container.
/// </summary>
public class JobDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the JobDocument class.
    /// </summary>
    public JobDocument()
    {
        DocumentType = "job";
    }

    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    [JsonProperty("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name for the job.
    /// </summary>
    [JsonProperty("queueName")]
    public string QueueName { get; set; } = "default";

    /// <summary>
    /// Gets or sets the current state of the job.
    /// </summary>
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state data for the current state.
    /// </summary>
    [JsonProperty("stateData")]
    public Dictionary<string, string> StateData { get; set; } = new();

    /// <summary>
    /// Gets or sets the complete state history for the job.
    /// </summary>
    [JsonProperty("stateHistory")]
    public List<StateHistoryEntry> StateHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the job invocation data.
    /// </summary>
    [JsonProperty("invocationData")]
    public InvocationData InvocationData { get; set; } = new();

    /// <summary>
    /// Gets or sets the job parameters.
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the job creation timestamp.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a state history entry for a job.
/// </summary>
public class StateHistoryEntry
{
    /// <summary>
    /// Gets or sets the state name.
    /// </summary>
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the state change.
    /// </summary>
    [JsonProperty("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets when the state was created.
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the state data.
    /// </summary>
    [JsonProperty("data")]
    public Dictionary<string, string> Data { get; set; } = new();
}

/// <summary>
/// Represents job invocation data.
/// </summary>
public class InvocationData
{
    /// <summary>
    /// Gets or sets the type name of the class containing the job method.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name to invoke.
    /// </summary>
    [JsonProperty("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameter types.
    /// </summary>
    [JsonProperty("parameterTypes")]
    public string[] ParameterTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the serialized arguments for the method.
    /// </summary>
    [JsonProperty("arguments")]
    public string[] Arguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the generic arguments if the method is generic.
    /// </summary>
    [JsonProperty("genericArguments")]
    public string[]? GenericArguments { get; set; }
}