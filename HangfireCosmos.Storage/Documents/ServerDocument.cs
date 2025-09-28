using Newtonsoft.Json;

namespace HangfireCosmos.Storage.Documents;

/// <summary>
/// Represents a server document in the Servers container.
/// </summary>
public class ServerDocument : BaseDocument
{
    /// <summary>
    /// Initializes a new instance of the ServerDocument class.
    /// </summary>
    public ServerDocument()
    {
        DocumentType = "server";
        PartitionKey = "servers";
    }

    /// <summary>
    /// Gets or sets the server ID.
    /// </summary>
    [JsonProperty("serverId")]
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server data containing configuration and metadata.
    /// </summary>
    [JsonProperty("data")]
    public ServerData Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the last heartbeat timestamp.
    /// </summary>
    [JsonProperty("lastHeartbeat")]
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the server was started.
    /// </summary>
    [JsonProperty("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents server configuration and metadata.
/// </summary>
public class ServerData
{
    /// <summary>
    /// Gets or sets the number of worker threads on this server.
    /// </summary>
    [JsonProperty("workerCount")]
    public int WorkerCount { get; set; }

    /// <summary>
    /// Gets or sets the list of queues this server processes.
    /// </summary>
    [JsonProperty("queues")]
    public string[] Queues { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets when the server was started.
    /// </summary>
    [JsonProperty("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the server name or hostname.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional server properties.
    /// </summary>
    [JsonProperty("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}