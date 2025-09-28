using FluentAssertions;
using HangfireCosmos.Storage.Documents;
using Newtonsoft.Json;
using Xunit;

namespace HangfireCosmos.Tests.Documents;

/// <summary>
/// Unit tests for the ServerDocument class and related classes.
/// </summary>
public class ServerDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var serverDocument = new ServerDocument();

        // Assert
        serverDocument.DocumentType.Should().Be("server");
        serverDocument.PartitionKey.Should().Be("servers");
        serverDocument.ServerId.Should().Be(string.Empty);
        serverDocument.Data.Should().NotBeNull();
        serverDocument.LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        serverDocument.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ServerId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverDocument = new ServerDocument();
        const string expectedServerId = "server-123";

        // Act
        serverDocument.ServerId = expectedServerId;

        // Assert
        serverDocument.ServerId.Should().Be(expectedServerId);
    }

    [Fact]
    public void Data_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverDocument = new ServerDocument();
        var expectedData = new ServerData
        {
            WorkerCount = 5,
            Queues = new[] { "default", "critical" },
            Name = "WebServer01"
        };

        // Act
        serverDocument.Data = expectedData;

        // Assert
        serverDocument.Data.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public void LastHeartbeat_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverDocument = new ServerDocument();
        var expectedLastHeartbeat = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        serverDocument.LastHeartbeat = expectedLastHeartbeat;

        // Assert
        serverDocument.LastHeartbeat.Should().Be(expectedLastHeartbeat);
    }

    [Fact]
    public void StartedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverDocument = new ServerDocument();
        var expectedStartedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        serverDocument.StartedAt = expectedStartedAt;

        // Assert
        serverDocument.StartedAt.Should().Be(expectedStartedAt);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var serverDocument = new ServerDocument
        {
            Id = "server-doc-1",
            ServerId = "server-123",
            Data = new ServerData
            {
                WorkerCount = 5,
                Queues = new[] { "default", "critical" },
                Name = "WebServer01",
                Properties = new Dictionary<string, string> { { "Version", "1.0" } }
            },
            LastHeartbeat = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            StartedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonConvert.SerializeObject(serverDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<ServerDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(serverDocument);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var serverDocument = new ServerDocument
        {
            ServerId = "server-123"
        };

        // Act
        var json = JsonConvert.SerializeObject(serverDocument);

        // Assert
        json.Should().Contain("\"serverId\":");
        json.Should().Contain("\"data\":");
        json.Should().Contain("\"lastHeartbeat\":");
        json.Should().Contain("\"startedAt\":");
    }

    [Fact]
    public void PartitionKey_ShouldBeSetToServersInConstructor()
    {
        // Act
        var serverDocument = new ServerDocument();

        // Assert
        serverDocument.PartitionKey.Should().Be("servers");
    }

    [Theory]
    [InlineData("")]
    [InlineData("server-1")]
    [InlineData("web-server-01")]
    [InlineData("background-processor-123")]
    public void ServerId_WithVariousValues_ShouldSetCorrectly(string serverId)
    {
        // Arrange
        var serverDocument = new ServerDocument();

        // Act
        serverDocument.ServerId = serverId;

        // Assert
        serverDocument.ServerId.Should().Be(serverId);
    }

    [Fact]
    public void JsonDeserialization_WithMissingOptionalProperties_ShouldUseDefaults()
    {
        // Arrange
        const string json = """
        {
            "id": "server-doc-1",
            "serverId": "server-123",
            "documentType": "server",
            "partitionKey": "servers"
        }
        """;

        // Act
        var document = JsonConvert.DeserializeObject<ServerDocument>(json);

        // Assert
        document.Should().NotBeNull();
        document!.Id.Should().Be("server-doc-1");
        document.ServerId.Should().Be("server-123");
        document.DocumentType.Should().Be("server");
        document.PartitionKey.Should().Be("servers");
        document.Data.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for the ServerData class.
/// </summary>
public class ServerDataTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var serverData = new ServerData();

        // Assert
        serverData.WorkerCount.Should().Be(0);
        serverData.Queues.Should().NotBeNull().And.BeEmpty();
        serverData.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        serverData.Name.Should().BeNull();
        serverData.Properties.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void WorkerCount_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        const int expectedWorkerCount = 10;

        // Act
        serverData.WorkerCount = expectedWorkerCount;

        // Assert
        serverData.WorkerCount.Should().Be(expectedWorkerCount);
    }

    [Fact]
    public void Queues_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var expectedQueues = new[] { "default", "critical", "low-priority" };

        // Act
        serverData.Queues = expectedQueues;

        // Assert
        serverData.Queues.Should().BeEquivalentTo(expectedQueues);
    }

    [Fact]
    public void StartedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var expectedStartedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        serverData.StartedAt = expectedStartedAt;

        // Assert
        serverData.StartedAt.Should().Be(expectedStartedAt);
    }

    [Fact]
    public void Name_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        const string expectedName = "WebServer01";

        // Act
        serverData.Name = expectedName;

        // Assert
        serverData.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var expectedProperties = new Dictionary<string, string>
        {
            { "Version", "1.0.0" },
            { "Environment", "Production" },
            { "Region", "US-East" }
        };

        // Act
        serverData.Properties = expectedProperties;

        // Assert
        serverData.Properties.Should().BeEquivalentTo(expectedProperties);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var serverData = new ServerData
        {
            WorkerCount = 5,
            Queues = new[] { "default", "critical" },
            StartedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Name = "WebServer01",
            Properties = new Dictionary<string, string> { { "Version", "1.0" } }
        };

        // Act
        var json = JsonConvert.SerializeObject(serverData);
        var deserializedData = JsonConvert.DeserializeObject<ServerData>(json);

        // Assert
        deserializedData.Should().NotBeNull();
        deserializedData!.Should().BeEquivalentTo(serverData);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var serverData = new ServerData
        {
            WorkerCount = 5,
            Name = "WebServer01"
        };

        // Act
        var json = JsonConvert.SerializeObject(serverData);

        // Assert
        json.Should().Contain("\"workerCount\":");
        json.Should().Contain("\"queues\":");
        json.Should().Contain("\"startedAt\":");
        json.Should().Contain("\"name\":");
        json.Should().Contain("\"properties\":");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    [InlineData(100)]
    public void WorkerCount_WithVariousValues_ShouldSetCorrectly(int workerCount)
    {
        // Arrange
        var serverData = new ServerData();

        // Act
        serverData.WorkerCount = workerCount;

        // Assert
        serverData.WorkerCount.Should().Be(workerCount);
    }

    [Fact]
    public void Queues_WithEmptyArray_ShouldSetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var queues = Array.Empty<string>();

        // Act
        serverData.Queues = queues;

        // Assert
        serverData.Queues.Should().BeEquivalentTo(queues);
    }

    [Fact]
    public void Queues_WithSingleQueue_ShouldSetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var queues = new[] { "default" };

        // Act
        serverData.Queues = queues;

        // Assert
        serverData.Queues.Should().BeEquivalentTo(queues);
    }

    [Fact]
    public void Queues_WithMultipleQueues_ShouldSetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var queues = new[] { "default", "critical", "low-priority", "background" };

        // Act
        serverData.Queues = queues;

        // Assert
        serverData.Queues.Should().BeEquivalentTo(queues);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("WebServer01")]
    [InlineData("Background-Processor-123")]
    public void Name_WithVariousValues_ShouldSetCorrectly(string? name)
    {
        // Arrange
        var serverData = new ServerData();

        // Act
        serverData.Name = name;

        // Assert
        serverData.Name.Should().Be(name);
    }

    [Fact]
    public void Properties_WithEmptyDictionary_ShouldSetCorrectly()
    {
        // Arrange
        var serverData = new ServerData();
        var properties = new Dictionary<string, string>();

        // Act
        serverData.Properties = properties;

        // Assert
        serverData.Properties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void JsonDeserialization_WithNullName_ShouldDeserializeCorrectly()
    {
        // Arrange
        const string json = """
        {
            "workerCount": 5,
            "queues": ["default"],
            "startedAt": "2023-01-01T00:00:00Z",
            "name": null,
            "properties": {}
        }
        """;

        // Act
        var serverData = JsonConvert.DeserializeObject<ServerData>(json);

        // Assert
        serverData.Should().NotBeNull();
        serverData!.Name.Should().BeNull();
        serverData.WorkerCount.Should().Be(5);
        serverData.Queues.Should().BeEquivalentTo(new[] { "default" });
    }
}