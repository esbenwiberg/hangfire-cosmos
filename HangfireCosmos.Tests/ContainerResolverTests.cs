using HangfireCosmos.Storage;
using HangfireCosmos.Storage.Documents;
using Xunit;

namespace HangfireCosmos.Tests;

public class ContainerResolverTests
{
    [Fact]
    public void GetContainerName_DedicatedStrategy_ReturnsCorrectContainerNames()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            JobsContainerName = "jobs",
            ServersContainerName = "servers",
            LocksContainerName = "locks",
            QueuesContainerName = "queues",
            SetsContainerName = "sets",
            HashesContainerName = "hashes",
            ListsContainerName = "lists",
            CountersContainerName = "counters"
        };
        var resolver = new ContainerResolver(options);

        // Act & Assert
        Assert.Equal("jobs", resolver.GetContainerName(typeof(JobDocument)));
        Assert.Equal("servers", resolver.GetContainerName(typeof(ServerDocument)));
        Assert.Equal("locks", resolver.GetContainerName(typeof(LockDocument)));
        Assert.Equal("queues", resolver.GetContainerName(typeof(QueueDocument)));
        Assert.Equal("sets", resolver.GetContainerName(typeof(SetDocument)));
        Assert.Equal("hashes", resolver.GetContainerName(typeof(HashDocument)));
        Assert.Equal("lists", resolver.GetContainerName(typeof(ListDocument)));
        Assert.Equal("counters", resolver.GetContainerName(typeof(CounterDocument)));
    }

    [Fact]
    public void GetContainerName_ConsolidatedStrategy_ReturnsCorrectContainerNames()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated,
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            CollectionsContainerName = "collections"
        };
        var resolver = new ContainerResolver(options);

        // Act & Assert
        Assert.Equal("jobs", resolver.GetContainerName(typeof(JobDocument)));
        Assert.Equal("metadata", resolver.GetContainerName(typeof(ServerDocument)));
        Assert.Equal("metadata", resolver.GetContainerName(typeof(LockDocument)));
        Assert.Equal("metadata", resolver.GetContainerName(typeof(QueueDocument)));
        Assert.Equal("metadata", resolver.GetContainerName(typeof(CounterDocument)));
        Assert.Equal("collections", resolver.GetContainerName(typeof(SetDocument)));
        Assert.Equal("collections", resolver.GetContainerName(typeof(HashDocument)));
        Assert.Equal("collections", resolver.GetContainerName(typeof(ListDocument)));
    }

    [Fact]
    public void GetPartitionKey_DedicatedStrategy_ReturnsCorrectPartitionKeys()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated
        };
        var resolver = new ContainerResolver(options);

        var jobDoc = new JobDocument { QueueName = "critical" };
        var serverDoc = new ServerDocument();
        var lockDoc = new LockDocument();
        var setDoc = new SetDocument { Key = "scheduled" };
        var hashDoc = new HashDocument { Key = "job:123" };
        var listDoc = new ListDocument { Key = "processing" };

        // Act & Assert
        Assert.Equal("job:critical", resolver.GetPartitionKey(jobDoc));
        Assert.Equal("servers", resolver.GetPartitionKey(serverDoc));
        Assert.Equal("locks", resolver.GetPartitionKey(lockDoc));
        Assert.Equal("set:scheduled", resolver.GetPartitionKey(setDoc));
        Assert.Equal("hash:job:123", resolver.GetPartitionKey(hashDoc));
        Assert.Equal("list:processing", resolver.GetPartitionKey(listDoc));
    }

    [Fact]
    public void GetPartitionKey_ConsolidatedStrategy_ReturnsCorrectPartitionKeys()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated
        };
        var resolver = new ContainerResolver(options);

        var jobDoc = new JobDocument { QueueName = "critical" };
        var serverDoc = new ServerDocument();
        var lockDoc = new LockDocument();
        var queueDoc = new QueueDocument();
        var counterDoc = new CounterDocument();
        var setDoc = new SetDocument { Key = "scheduled" };
        var hashDoc = new HashDocument { Key = "job:123" };
        var listDoc = new ListDocument { Key = "processing" };

        // Act & Assert
        Assert.Equal("job:critical", resolver.GetPartitionKey(jobDoc));
        Assert.Equal("servers", resolver.GetPartitionKey(serverDoc));
        Assert.Equal("locks", resolver.GetPartitionKey(lockDoc));
        Assert.Equal("queues", resolver.GetPartitionKey(queueDoc));
        Assert.Equal("counters", resolver.GetPartitionKey(counterDoc));
        Assert.Equal("set:scheduled", resolver.GetPartitionKey(setDoc));
        Assert.Equal("hash:job:123", resolver.GetPartitionKey(hashDoc));
        Assert.Equal("list:processing", resolver.GetPartitionKey(listDoc));
    }

    [Fact]
    public void GetAllContainerNames_DedicatedStrategy_ReturnsAllEightContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            JobsContainerName = "jobs",
            ServersContainerName = "servers",
            LocksContainerName = "locks",
            QueuesContainerName = "queues",
            SetsContainerName = "sets",
            HashesContainerName = "hashes",
            ListsContainerName = "lists",
            CountersContainerName = "counters"
        };
        var resolver = new ContainerResolver(options);

        // Act
        var containerNames = resolver.GetAllContainerNames().ToList();

        // Assert
        Assert.Equal(8, containerNames.Count);
        Assert.Contains("jobs", containerNames);
        Assert.Contains("servers", containerNames);
        Assert.Contains("locks", containerNames);
        Assert.Contains("queues", containerNames);
        Assert.Contains("sets", containerNames);
        Assert.Contains("hashes", containerNames);
        Assert.Contains("lists", containerNames);
        Assert.Contains("counters", containerNames);
    }

    [Fact]
    public void GetAllContainerNames_ConsolidatedStrategy_ReturnsThreeContainers()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated,
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            CollectionsContainerName = "collections"
        };
        var resolver = new ContainerResolver(options);

        // Act
        var containerNames = resolver.GetAllContainerNames().ToList();

        // Assert
        Assert.Equal(3, containerNames.Count);
        Assert.Contains("jobs", containerNames);
        Assert.Contains("metadata", containerNames);
        Assert.Contains("collections", containerNames);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContainerResolver(null!));
    }

    [Fact]
    public void GetContainerName_UnknownDocumentType_ThrowsArgumentException()
    {
        // Arrange
        var options = new CosmosStorageOptions();
        var resolver = new ContainerResolver(options);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => resolver.GetContainerName(typeof(string)));
    }

    [Theory]
    [InlineData(CollectionStrategy.Dedicated)]
    [InlineData(CollectionStrategy.Consolidated)]
    public void GetContainerName_WithDocumentInstance_ReturnsCorrectContainer(CollectionStrategy strategy)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = strategy,
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            ServersContainerName = "servers"
        };
        var resolver = new ContainerResolver(options);
        var jobDoc = new JobDocument();

        // Act
        var containerName = resolver.GetContainerName(jobDoc);

        // Assert
        Assert.Equal("jobs", containerName);
    }
}