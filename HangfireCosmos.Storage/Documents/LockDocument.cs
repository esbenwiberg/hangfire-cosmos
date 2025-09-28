using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a distributed lock document in the Locks container.
/// </summary>
public class LockDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the LockDocument class.
    /// </summary>
    public LockDocument()
    {
        DocumentType = "lock";
        PartitionKey = "locks";
    }

    /// <summary>
    /// Gets or sets the resource being locked.
    /// </summary>
    [JsonProperty("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner of the lock (typically server ID).
    /// </summary>
    [JsonProperty("owner")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the lock was acquired.
    /// </summary>
    [JsonProperty("acquiredAt")]
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the lock timeout value.
    /// </summary>
    [JsonProperty("timeout")]
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets additional lock metadata.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}