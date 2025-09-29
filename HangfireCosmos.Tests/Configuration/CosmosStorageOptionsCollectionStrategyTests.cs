using HangfireCosmos.Storage;
using Xunit;

namespace HangfireCosmos.Tests.Configuration;

public class CosmosStorageOptionsCollectionStrategyTests
{
    [Fact]
    public void Validate_DedicatedStrategy_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            ServersContainerName = "servers",
            LocksContainerName = "locks",
            QueuesContainerName = "queues",
            SetsContainerName = "sets",
            HashesContainerName = "hashes",
            ListsContainerName = "lists",
            CountersContainerName = "counters"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_ConsolidatedStrategy_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            CollectionsContainerName = "collections"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_DedicatedStrategy_MissingServersContainerName_ThrowsArgumentException(string containerName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            ServersContainerName = containerName
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("ServersContainerName", exception.Message);
        Assert.Contains("Dedicated strategy", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_DedicatedStrategy_MissingLocksContainerName_ThrowsArgumentException(string containerName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            ServersContainerName = "servers",
            LocksContainerName = containerName
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("LocksContainerName", exception.Message);
        Assert.Contains("Dedicated strategy", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ConsolidatedStrategy_MissingMetadataContainerName_ThrowsArgumentException(string containerName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            MetadataContainerName = containerName
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MetadataContainerName", exception.Message);
        Assert.Contains("Consolidated strategy", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_ConsolidatedStrategy_MissingCollectionsContainerName_ThrowsArgumentException(string containerName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Consolidated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            MetadataContainerName = "metadata",
            CollectionsContainerName = containerName
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("CollectionsContainerName", exception.Message);
        Assert.Contains("Consolidated strategy", exception.Message);
    }

    [Fact]
    public void DefaultValues_CollectionStrategy_IsDedicated()
    {
        // Arrange & Act
        var options = new CosmosStorageOptions();

        // Assert
        Assert.Equal(CollectionStrategy.Dedicated, options.CollectionStrategy);
    }

    [Fact]
    public void DefaultValues_ConsolidatedContainerNames_AreSet()
    {
        // Arrange & Act
        var options = new CosmosStorageOptions();

        // Assert
        Assert.Equal("metadata", options.MetadataContainerName);
        Assert.Equal("collections", options.CollectionsContainerName);
    }

    [Theory]
    [InlineData(CollectionStrategy.Dedicated)]
    [InlineData(CollectionStrategy.Consolidated)]
    public void Validate_CommonRequiredFields_MissingDatabaseName_ThrowsArgumentException(CollectionStrategy strategy)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = strategy,
            DatabaseName = "",
            JobsContainerName = "jobs"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("DatabaseName", exception.Message);
    }

    [Theory]
    [InlineData(CollectionStrategy.Dedicated)]
    [InlineData(CollectionStrategy.Consolidated)]
    public void Validate_CommonRequiredFields_MissingJobsContainerName_ThrowsArgumentException(CollectionStrategy strategy)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = strategy,
            DatabaseName = "hangfire",
            JobsContainerName = ""
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("JobsContainerName", exception.Message);
    }

    [Fact]
    public void Validate_DedicatedStrategy_AllContainerNamesMissing_ThrowsMultipleExceptions()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            CollectionStrategy = CollectionStrategy.Dedicated,
            DatabaseName = "hangfire",
            JobsContainerName = "jobs",
            ServersContainerName = "",
            LocksContainerName = "",
            QueuesContainerName = "",
            SetsContainerName = "",
            HashesContainerName = "",
            ListsContainerName = "",
            CountersContainerName = ""
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("ServersContainerName", exception.Message);
    }
}