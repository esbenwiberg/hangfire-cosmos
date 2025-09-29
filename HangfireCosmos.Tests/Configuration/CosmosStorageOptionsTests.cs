using FluentAssertions;
using HangfireCosmos.Storage;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace HangfireCosmos.Tests.Configuration;

/// <summary>
/// Unit tests for the CosmosStorageOptions class and related configuration classes.
/// </summary>
public class CosmosStorageOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new CosmosStorageOptions();

        // Assert
        options.DatabaseName.Should().Be("hangfire");
        options.JobsContainerName.Should().Be("jobs");
        options.ServersContainerName.Should().Be("servers");
        options.LocksContainerName.Should().Be("locks");
        options.QueuesContainerName.Should().Be("queues");
        options.SetsContainerName.Should().Be("sets");
        options.HashesContainerName.Should().Be("hashes");
        options.ListsContainerName.Should().Be("lists");
        options.CountersContainerName.Should().Be("counters");
        options.DefaultThroughput.Should().Be(400);
        options.UseSharedThroughput.Should().BeFalse();
        options.ConsistencyLevel.Should().Be(ConsistencyLevel.Session);
        options.DefaultJobExpiration.Should().Be(TimeSpan.FromDays(7));
        options.ServerTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.LockTimeout.Should().Be(TimeSpan.FromMinutes(1));
        options.EnableCaching.Should().BeTrue();
        options.CacheExpiration.Should().Be(TimeSpan.FromMinutes(5));
        options.MaxRetryAttempts.Should().Be(5);
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.AutoCreateDatabase.Should().BeTrue();
        options.AutoCreateContainers.Should().BeTrue();
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.MaxConnectionLimit.Should().Be(50);
        options.PreferredRegions.Should().NotBeNull().And.BeEmpty();
        options.EnableContentResponseOnWrite.Should().BeFalse();
        options.BulkExecutionOptions.Should().NotBeNull();
        options.CustomIndexingPolicies.Should().NotBeNull().And.BeEmpty();
        options.TtlSettings.Should().NotBeNull();
        options.Performance.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseName_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var options = new CosmosStorageOptions();
        const string expectedDatabaseName = "my-hangfire-db";

        // Act
        options.DatabaseName = expectedDatabaseName;

        // Assert
        options.DatabaseName.Should().Be(expectedDatabaseName);
    }

    [Fact]
    public void JobsContainerName_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var options = new CosmosStorageOptions();
        const string expectedContainerName = "my-jobs";

        // Act
        options.JobsContainerName = expectedContainerName;

        // Assert
        options.JobsContainerName.Should().Be(expectedContainerName);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(1000)]
    [InlineData(4000)]
    public void DefaultThroughput_WithValidValues_ShouldSetCorrectly(int throughput)
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Act
        options.DefaultThroughput = throughput;

        // Assert
        options.DefaultThroughput.Should().Be(throughput);
    }

    [Theory]
    [InlineData(ConsistencyLevel.Eventual)]
    [InlineData(ConsistencyLevel.Session)]
    [InlineData(ConsistencyLevel.BoundedStaleness)]
    [InlineData(ConsistencyLevel.Strong)]
    [InlineData(ConsistencyLevel.ConsistentPrefix)]
    public void ConsistencyLevel_WithValidValues_ShouldSetCorrectly(ConsistencyLevel consistencyLevel)
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Act
        options.ConsistencyLevel = consistencyLevel;

        // Assert
        options.ConsistencyLevel.Should().Be(consistencyLevel);
    }

    [Fact]
    public void PreferredRegions_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var options = new CosmosStorageOptions();
        var expectedRegions = new List<string> { "East US", "West US", "North Europe" };

        // Act
        options.PreferredRegions = expectedRegions;

        // Assert
        options.PreferredRegions.Should().BeEquivalentTo(expectedRegions);
    }

    [Fact]
    public void CustomIndexingPolicies_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var options = new CosmosStorageOptions();
        var indexingPolicy = new IndexingPolicy();
        indexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
        var expectedPolicies = new Dictionary<string, IndexingPolicy>
        {
            { "jobs", indexingPolicy }
        };

        // Act
        options.CustomIndexingPolicies = expectedPolicies;

        // Assert
        options.CustomIndexingPolicies.Should().BeEquivalentTo(expectedPolicies);
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            DefaultThroughput = 400,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            RequestTimeout = TimeSpan.FromSeconds(30),
            MaxConnectionLimit = 100
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidDatabaseName_ShouldThrowArgumentException(string? databaseName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = databaseName!
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("DatabaseName cannot be null or empty. (Parameter 'DatabaseName')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidJobsContainerName_ShouldThrowArgumentException(string? jobsContainerName)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = jobsContainerName!
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("JobsContainerName cannot be null or empty. (Parameter 'JobsContainerName')");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(399)]
    public void Validate_WithInvalidDefaultThroughput_ShouldThrowArgumentException(int throughput)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            DefaultThroughput = throughput
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("DefaultThroughput must be at least 400 RU/s. (Parameter 'DefaultThroughput')");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WithNegativeMaxRetryAttempts_ShouldThrowArgumentException(int maxRetryAttempts)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            MaxRetryAttempts = maxRetryAttempts
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("MaxRetryAttempts cannot be negative. (Parameter 'MaxRetryAttempts')");
    }

    [Fact]
    public void Validate_WithNegativeRetryDelay_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            RetryDelay = TimeSpan.FromMilliseconds(-100)
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("RetryDelay cannot be negative. (Parameter 'RetryDelay')");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidRequestTimeout_ShouldThrowArgumentException(int timeoutSeconds)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            RequestTimeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("RequestTimeout must be positive. (Parameter 'RequestTimeout')");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidMaxConnectionLimit_ShouldThrowArgumentException(int maxConnectionLimit)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            MaxConnectionLimit = maxConnectionLimit
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("MaxConnectionLimit must be positive. (Parameter 'MaxConnectionLimit')");
    }

    [Fact]
    public void AllContainerNames_ShouldHaveDefaultValues()
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Assert
        options.ServersContainerName.Should().Be("servers");
        options.LocksContainerName.Should().Be("locks");
        options.QueuesContainerName.Should().Be("queues");
        options.SetsContainerName.Should().Be("sets");
        options.HashesContainerName.Should().Be("hashes");
        options.ListsContainerName.Should().Be("lists");
        options.CountersContainerName.Should().Be("counters");
    }

    [Fact]
    public void TimeoutProperties_ShouldHaveReasonableDefaults()
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Assert
        options.DefaultJobExpiration.Should().Be(TimeSpan.FromDays(7));
        options.ServerTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.LockTimeout.Should().Be(TimeSpan.FromMinutes(1));
        options.CacheExpiration.Should().Be(TimeSpan.FromMinutes(5));
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.RequestTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void UseSharedThroughput_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Act
        options.UseSharedThroughput = true;

        // Assert
        options.UseSharedThroughput.Should().BeTrue();
    }

    [Fact]
    public void UseSharedThroughput_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var options = new CosmosStorageOptions();

        // Assert
        options.UseSharedThroughput.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSharedThroughput_WithBothValues_ShouldSetCorrectly(bool useSharedThroughput)
    {
        // Arrange
        var options = new CosmosStorageOptions();

        // Act
        options.UseSharedThroughput = useSharedThroughput;

        // Assert
        options.UseSharedThroughput.Should().Be(useSharedThroughput);
    }

    [Theory]
    [InlineData(400, true)]
    [InlineData(1000, true)]
    [InlineData(4000, true)]
    [InlineData(400, false)]
    [InlineData(1000, false)]
    [InlineData(4000, false)]
    public void Validate_WithValidSharedThroughputConfiguration_ShouldNotThrow(int throughput, bool useSharedThroughput)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            DefaultThroughput = throughput,
            UseSharedThroughput = useSharedThroughput
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(399, true)]
    public void Validate_WithInvalidSharedThroughputConfiguration_ShouldThrowArgumentException(int throughput, bool useSharedThroughput)
    {
        // Arrange
        var options = new CosmosStorageOptions
        {
            DatabaseName = "test-db",
            JobsContainerName = "test-jobs",
            DefaultThroughput = throughput,
            UseSharedThroughput = useSharedThroughput
        };

        // Act & Assert
        var action = () => options.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("*DefaultThroughput must be at least 400 RU/s*");
    }
}

/// <summary>
/// Unit tests for the BulkExecutionOptions class.
/// </summary>
public class BulkExecutionOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new BulkExecutionOptions();

        // Assert
        options.MaxBatchSize.Should().Be(100);
        options.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount);
        options.EnableBulkExecution.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MaxBatchSize_ShouldSetAndGetCorrectly(int maxBatchSize)
    {
        // Arrange
        var options = new BulkExecutionOptions();

        // Act
        options.MaxBatchSize = maxBatchSize;

        // Assert
        options.MaxBatchSize.Should().Be(maxBatchSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void MaxDegreeOfParallelism_ShouldSetAndGetCorrectly(int maxDegreeOfParallelism)
    {
        // Arrange
        var options = new BulkExecutionOptions();

        // Act
        options.MaxDegreeOfParallelism = maxDegreeOfParallelism;

        // Assert
        options.MaxDegreeOfParallelism.Should().Be(maxDegreeOfParallelism);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableBulkExecution_ShouldSetAndGetCorrectly(bool enableBulkExecution)
    {
        // Arrange
        var options = new BulkExecutionOptions();

        // Act
        options.EnableBulkExecution = enableBulkExecution;

        // Assert
        options.EnableBulkExecution.Should().Be(enableBulkExecution);
    }
}

/// <summary>
/// Unit tests for the TtlSettings class.
/// </summary>
public class TtlSettingsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new TtlSettings();

        // Assert
        settings.JobDocumentTtl.Should().Be((int)TimeSpan.FromDays(30).TotalSeconds);
        settings.ServerDocumentTtl.Should().Be((int)TimeSpan.FromMinutes(10).TotalSeconds);
        settings.LockDocumentTtl.Should().Be((int)TimeSpan.FromMinutes(5).TotalSeconds);
        settings.CounterDocumentTtl.Should().Be((int)TimeSpan.FromDays(7).TotalSeconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3600)] // 1 hour
    [InlineData(86400)] // 1 day
    public void JobDocumentTtl_ShouldSetAndGetCorrectly(int? ttl)
    {
        // Arrange
        var settings = new TtlSettings();

        // Act
        settings.JobDocumentTtl = ttl;

        // Assert
        settings.JobDocumentTtl.Should().Be(ttl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(300)] // 5 minutes
    [InlineData(600)] // 10 minutes
    public void ServerDocumentTtl_ShouldSetAndGetCorrectly(int? ttl)
    {
        // Arrange
        var settings = new TtlSettings();

        // Act
        settings.ServerDocumentTtl = ttl;

        // Assert
        settings.ServerDocumentTtl.Should().Be(ttl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(60)] // 1 minute
    [InlineData(300)] // 5 minutes
    public void LockDocumentTtl_ShouldSetAndGetCorrectly(int? ttl)
    {
        // Arrange
        var settings = new TtlSettings();

        // Act
        settings.LockDocumentTtl = ttl;

        // Assert
        settings.LockDocumentTtl.Should().Be(ttl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3600)] // 1 hour
    [InlineData(604800)] // 1 week
    public void CounterDocumentTtl_ShouldSetAndGetCorrectly(int? ttl)
    {
        // Arrange
        var settings = new TtlSettings();

        // Act
        settings.CounterDocumentTtl = ttl;

        // Assert
        settings.CounterDocumentTtl.Should().Be(ttl);
    }
}

/// <summary>
/// Unit tests for the PerformanceOptions class.
/// </summary>
public class PerformanceOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new PerformanceOptions();

        // Assert
        options.QueryPageSize.Should().Be(100);
        options.EnableQueryMetrics.Should().BeFalse();
        options.ConnectionMode.Should().Be(ConnectionMode.Direct);
        options.EnableTcpConnectionEndpointRediscovery.Should().BeTrue();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public void QueryPageSize_ShouldSetAndGetCorrectly(int queryPageSize)
    {
        // Arrange
        var options = new PerformanceOptions();

        // Act
        options.QueryPageSize = queryPageSize;

        // Assert
        options.QueryPageSize.Should().Be(queryPageSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableQueryMetrics_ShouldSetAndGetCorrectly(bool enableQueryMetrics)
    {
        // Arrange
        var options = new PerformanceOptions();

        // Act
        options.EnableQueryMetrics = enableQueryMetrics;

        // Assert
        options.EnableQueryMetrics.Should().Be(enableQueryMetrics);
    }

    [Theory]
    [InlineData(ConnectionMode.Direct)]
    [InlineData(ConnectionMode.Gateway)]
    public void ConnectionMode_ShouldSetAndGetCorrectly(ConnectionMode connectionMode)
    {
        // Arrange
        var options = new PerformanceOptions();

        // Act
        options.ConnectionMode = connectionMode;

        // Assert
        options.ConnectionMode.Should().Be(connectionMode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableTcpConnectionEndpointRediscovery_ShouldSetAndGetCorrectly(bool enableTcpConnectionEndpointRediscovery)
    {
        // Arrange
        var options = new PerformanceOptions();

        // Act
        options.EnableTcpConnectionEndpointRediscovery = enableTcpConnectionEndpointRediscovery;

        // Assert
        options.EnableTcpConnectionEndpointRediscovery.Should().Be(enableTcpConnectionEndpointRediscovery);
    }
}