using FluentAssertions;
using Hangfire.States;
using Hangfire.Storage;
using HangfireCosmos.Storage;
using HangfireCosmos.Storage.Connection;
using HangfireCosmos.Storage.Documents;
using HangfireCosmos.Storage.Repository;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace HangfireCosmos.Tests.Connection;

/// <summary>
/// Unit tests for the CosmosWriteOnlyTransaction class.
/// </summary>
public class CosmosWriteOnlyTransactionTests
{
    private readonly Mock<ICosmosDocumentRepository> _mockRepository;
    private readonly CosmosStorageOptions _options;
    private readonly CosmosWriteOnlyTransaction _transaction;

    public CosmosWriteOnlyTransactionTests()
    {
        _mockRepository = new Mock<ICosmosDocumentRepository>();
        _options = new CosmosStorageOptions
        {
            JobsContainerName = "jobs",
            CountersContainerName = "counters",
            SetsContainerName = "sets",
            ListsContainerName = "lists",
            HashesContainerName = "hashes"
        };
        _transaction = new CosmosWriteOnlyTransaction(_mockRepository.Object, _options);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosWriteOnlyTransaction(null!, _options);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosWriteOnlyTransaction(_mockRepository.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var transaction = new CosmosWriteOnlyTransaction(_mockRepository.Object, _options);

        // Assert
        transaction.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ExpireJob_WithInvalidJobId_ShouldThrowArgumentNullException(string? jobId)
    {
        // Act & Assert
        var action = () => _transaction.ExpireJob(jobId!, TimeSpan.FromHours(1));
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobId");
    }

    [Fact]
    public void ExpireJob_WithValidJobId_ShouldAddOperation()
    {
        // Arrange
        var jobDocument = new JobDocument { Id = "job-1", JobId = "job-1" };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName,
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jobDocument });

        // Act
        _transaction.ExpireJob("job-1", TimeSpan.FromHours(1));
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.JobsContainerName, It.IsAny<JobDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PersistJob_WithInvalidJobId_ShouldThrowArgumentNullException(string? jobId)
    {
        // Act & Assert
        var action = () => _transaction.PersistJob(jobId!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobId");
    }

    [Fact]
    public void PersistJob_WithValidJobId_ShouldAddOperation()
    {
        // Arrange
        var jobDocument = new JobDocument { Id = "job-1", JobId = "job-1", ExpireAt = DateTime.UtcNow.AddHours(1) };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName,
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jobDocument });

        // Act
        _transaction.PersistJob("job-1");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.JobsContainerName, It.Is<JobDocument>(j => j.ExpireAt == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetJobState_WithInvalidJobId_ShouldThrowArgumentNullException(string? jobId)
    {
        // Arrange
        var state = new Mock<IState>();

        // Act & Assert
        var action = () => _transaction.SetJobState(jobId!, state.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobId");
    }

    [Fact]
    public void SetJobState_WithNullState_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _transaction.SetJobState("job-1", null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("state");
    }

    [Fact]
    public void SetJobState_WithValidParameters_ShouldUpdateJobStateAndHistory()
    {
        // Arrange
        var jobDocument = new JobDocument { Id = "job-1", JobId = "job-1", StateHistory = new List<StateHistoryEntry>() };
        var mockState = new Mock<IState>();
        mockState.Setup(x => x.Name).Returns("Processing");
        mockState.Setup(x => x.Reason).Returns("Job started");
        mockState.Setup(x => x.SerializeData()).Returns(new Dictionary<string, string> { { "StartedAt", "2023-01-01" } });

        _mockRepository.Setup(x => x.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName,
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jobDocument });

        // Act
        _transaction.SetJobState("job-1", mockState.Object);
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.JobsContainerName, 
            It.Is<JobDocument>(j => j.State == "Processing" && j.StateHistory.Count == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddJobState_WithValidParameters_ShouldAddToStateHistoryOnly()
    {
        // Arrange
        var jobDocument = new JobDocument { Id = "job-1", JobId = "job-1", State = "Enqueued", StateHistory = new List<StateHistoryEntry>() };
        var mockState = new Mock<IState>();
        mockState.Setup(x => x.Name).Returns("Processing");
        mockState.Setup(x => x.Reason).Returns("Job started");
        mockState.Setup(x => x.SerializeData()).Returns(new Dictionary<string, string>());

        _mockRepository.Setup(x => x.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName,
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jobDocument });

        // Act
        _transaction.AddJobState("job-1", mockState.Object);
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.JobsContainerName, 
            It.Is<JobDocument>(j => j.State == "Enqueued" && j.StateHistory.Count == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "job-1")]
    [InlineData("", "job-1")]
    [InlineData("queue", null)]
    [InlineData("queue", "")]
    public void AddToQueue_WithInvalidParameters_ShouldThrowArgumentNullException(string? queue, string? jobId)
    {
        // Act & Assert
        var action = () => _transaction.AddToQueue(queue!, jobId!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddToQueue_WithValidParameters_ShouldUpdateJobQueue()
    {
        // Arrange
        var jobDocument = new JobDocument { Id = "job-1", JobId = "job-1" };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<JobDocument>(
                _options.JobsContainerName,
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jobDocument });

        // Act
        _transaction.AddToQueue("critical", "job-1");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.JobsContainerName, 
            It.Is<JobDocument>(j => j.QueueName == "critical" && j.State == "enqueued"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IncrementCounter_WithInvalidKey_ShouldThrowArgumentNullException(string? key)
    {
        // Act & Assert
        var action = () => _transaction.IncrementCounter(key!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void IncrementCounter_WithNewCounter_ShouldCreateAndIncrement()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName,
                "counter:test",
                "counters",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterDocument?)null);

        // Act
        _transaction.IncrementCounter("test");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.CountersContainerName, 
            It.Is<CounterDocument>(c => c.Key == "test" && c.Value == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IncrementCounter_WithExistingCounter_ShouldIncrement()
    {
        // Arrange
        var existingCounter = new CounterDocument { Id = "counter:test", Key = "test", Value = 5 };
        _mockRepository.Setup(x => x.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName,
                "counter:test",
                "counters",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCounter);

        // Act
        _transaction.IncrementCounter("test");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.CountersContainerName, 
            It.Is<CounterDocument>(c => c.Key == "test" && c.Value == 6), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IncrementCounter_WithExpiration_ShouldSetExpireAt()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName,
                "counter:test",
                "counters",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterDocument?)null);

        // Act
        _transaction.IncrementCounter("test", TimeSpan.FromHours(1));
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.CountersContainerName, 
            It.Is<CounterDocument>(c => c.Key == "test" && c.Value == 1 && c.ExpireAt.HasValue), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DecrementCounter_WithExistingCounter_ShouldDecrement()
    {
        // Arrange
        var existingCounter = new CounterDocument { Id = "counter:test", Key = "test", Value = 5 };
        _mockRepository.Setup(x => x.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName,
                "counter:test",
                "counters",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCounter);

        // Act
        _transaction.DecrementCounter("test");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.CountersContainerName, 
            It.Is<CounterDocument>(c => c.Key == "test" && c.Value == 4), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DecrementCounter_WithNonExistentCounter_ShouldNotCallUpdate()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetDocumentAsync<CounterDocument>(
                _options.CountersContainerName,
                "counter:test",
                "counters",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CounterDocument?)null);

        // Act
        _transaction.DecrementCounter("test");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(_options.CountersContainerName, 
            It.IsAny<CounterDocument>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData("key", null)]
    [InlineData("key", "")]
    public void AddToSet_WithInvalidParameters_ShouldThrowArgumentNullException(string? key, string? value)
    {
        // Act & Assert
        var action = () => _transaction.AddToSet(key!, value!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddToSet_WithValidParameters_ShouldUpsertSetDocument()
    {
        // Act
        _transaction.AddToSet("scheduled", "job:123");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.SetsContainerName, 
            It.Is<SetDocument>(s => s.Key == "scheduled" && s.Value == "job:123"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddToSet_WithScore_ShouldUpsertSetDocumentWithScore()
    {
        // Act
        _transaction.AddToSet("scheduled", "job:123", 1640995200.0);
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.SetsContainerName, 
            It.Is<SetDocument>(s => s.Key == "scheduled" && s.Value == "job:123" && s.Score == 1640995200.0), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RemoveFromSet_WithValidParameters_ShouldDeleteDocument()
    {
        // Act
        _transaction.RemoveFromSet("scheduled", "job:123");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(_options.SetsContainerName, 
            "set:scheduled:job:123", 
            "set:scheduled", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void InsertToList_WithValidParameters_ShouldCreateListDocument()
    {
        // Arrange
        _mockRepository.Setup(x => x.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName,
                It.IsAny<QueryDefinition>(),
                "list:queue:default",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ListDocument>());

        // Act
        _transaction.InsertToList("queue:default", "job:123");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.CreateDocumentAsync(_options.ListsContainerName, 
            It.Is<ListDocument>(l => l.Key == "queue:default" && l.Value == "job:123" && l.Index == 0), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RemoveFromList_WithValidParameters_ShouldDeleteMatchingDocuments()
    {
        // Arrange
        var listDocuments = new List<ListDocument>
        {
            new() { Id = "list:queue:default:0", Key = "queue:default", Value = "job:123", Index = 0, PartitionKey = "list:queue:default" }
        };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName,
                It.IsAny<QueryDefinition>(),
                "list:queue:default",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(listDocuments);

        // Act
        _transaction.RemoveFromList("queue:default", "job:123");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(_options.ListsContainerName, 
            "list:queue:default:0", 
            "list:queue:default", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TrimList_WithValidParameters_ShouldDeleteDocumentsOutsideRange()
    {
        // Arrange
        var listDocuments = new List<ListDocument>
        {
            new() { Id = "list:queue:default:0", Key = "queue:default", Index = 0, PartitionKey = "list:queue:default" },
            new() { Id = "list:queue:default:5", Key = "queue:default", Index = 5, PartitionKey = "list:queue:default" }
        };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<ListDocument>(
                _options.ListsContainerName,
                It.IsAny<QueryDefinition>(),
                "list:queue:default",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(listDocuments);

        // Act
        _transaction.TrimList("queue:default", 1, 4);
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(_options.ListsContainerName, 
            It.IsAny<string>(), 
            "list:queue:default", 
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void SetRangeInHash_WithValidParameters_ShouldUpsertHashDocuments()
    {
        // Arrange
        var keyValuePairs = new Dictionary<string, string>
        {
            { "field1", "value1" },
            { "field2", "value2" }
        };

        // Act
        _transaction.SetRangeInHash("job:123", keyValuePairs);
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.HashesContainerName, 
            It.IsAny<HashDocument>(), 
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void RemoveHash_WithValidParameters_ShouldDeleteAllHashDocuments()
    {
        // Arrange
        var hashDocuments = new List<HashDocument>
        {
            new() { Id = "hash:job:123:field1", Key = "job:123", Field = "field1", PartitionKey = "hash:job:123" },
            new() { Id = "hash:job:123:field2", Key = "job:123", Field = "field2", PartitionKey = "hash:job:123" }
        };
        _mockRepository.Setup(x => x.QueryDocumentsAsync<HashDocument>(
                _options.HashesContainerName,
                It.IsAny<QueryDefinition>(),
                "hash:job:123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashDocuments);

        // Act
        _transaction.RemoveHash("job:123");
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(_options.HashesContainerName, 
            It.IsAny<string>(), 
            "hash:job:123", 
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void Commit_WithOperations_ShouldExecuteAllOperations()
    {
        // Arrange
        _transaction.IncrementCounter("test1");
        _transaction.IncrementCounter("test2");

        // Act
        _transaction.Commit();

        // Assert
        _mockRepository.Verify(x => x.UpsertDocumentAsync(_options.CountersContainerName, 
            It.IsAny<CounterDocument>(), 
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void Dispose_ShouldClearOperations()
    {
        // Arrange
        _transaction.IncrementCounter("test");

        // Act
        _transaction.Dispose();

        // Assert - Should not throw and operations should be cleared
        var action = () => _transaction.Commit();
        action.Should().NotThrow();
    }
}

/// <summary>
/// Unit tests for the CosmosDistributedLock class.
/// </summary>
public class CosmosDistributedLockTests
{
    private readonly Mock<ICosmosDocumentRepository> _mockRepository;
    private readonly CosmosStorageOptions _options;
    private readonly LockDocument _lockDocument;

    public CosmosDistributedLockTests()
    {
        _mockRepository = new Mock<ICosmosDocumentRepository>();
        _options = new CosmosStorageOptions
        {
            LocksContainerName = "locks"
        };
        _lockDocument = new LockDocument
        {
            Id = "lock:test-resource",
            Resource = "test-resource",
            Owner = "server-1",
            PartitionKey = "locks"
        };
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosDistributedLock(null!, _options, _lockDocument);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosDistributedLock(_mockRepository.Object, null!, _lockDocument);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLockDocument_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosDistributedLock(_mockRepository.Object, _options, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("lockDocument");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var distributedLock = new CosmosDistributedLock(_mockRepository.Object, _options, _lockDocument);

        // Assert
        distributedLock.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldDeleteLockDocument()
    {
        // Arrange
        var distributedLock = new CosmosDistributedLock(_mockRepository.Object, _options, _lockDocument);

        // Act
        distributedLock.Dispose();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(
            _options.LocksContainerName,
            _lockDocument.Id,
            _lockDocument.PartitionKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_WhenDeleteFails_ShouldNotThrow()
    {
        // Arrange
        _mockRepository.Setup(x => x.DeleteDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Delete failed"));

        var distributedLock = new CosmosDistributedLock(_mockRepository.Object, _options, _lockDocument);

        // Act & Assert
        var action = () => distributedLock.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldOnlyDeleteOnce()
    {
        // Arrange
        var distributedLock = new CosmosDistributedLock(_mockRepository.Object, _options, _lockDocument);

        // Act
        distributedLock.Dispose();
        distributedLock.Dispose();

        // Assert
        _mockRepository.Verify(x => x.DeleteDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

/// <summary>
/// Unit tests for the CosmosFetchedJob class.
/// </summary>
public class CosmosFetchedJobTests
{
    private readonly Mock<ICosmosDocumentRepository> _mockRepository;
    private readonly CosmosStorageOptions _options;
    private readonly JobDocument _jobDocument;

    public CosmosFetchedJobTests()
    {
        _mockRepository = new Mock<ICosmosDocumentRepository>();
        _options = new CosmosStorageOptions
        {
            JobsContainerName = "jobs"
        };
        _jobDocument = new JobDocument
        {
            Id = "job-1",
            JobId = "job-1",
            State = "enqueued",
            QueueName = "default"
        };
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosFetchedJob(null!, _options, _jobDocument);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosFetchedJob(_mockRepository.Object, null!, _jobDocument);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullJobDocument_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CosmosFetchedJob(_mockRepository.Object, _options, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("jobDocument");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Assert
        fetchedJob.Should().NotBeNull();
        fetchedJob.JobId.Should().Be("job-1");
    }

    [Fact]
    public void JobId_ShouldReturnJobDocumentJobId()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act & Assert
        fetchedJob.JobId.Should().Be(_jobDocument.JobId);
    }

    [Fact]
    public void RemoveFromQueue_ShouldUpdateJobStateToProcessing()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.RemoveFromQueue();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "processing"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RemoveFromQueue_CalledMultipleTimes_ShouldOnlyUpdateOnce()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.RemoveFromQueue();
        fetchedJob.RemoveFromQueue();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<JobDocument>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Requeue_ShouldUpdateJobStateToEnqueued()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.Requeue();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "enqueued"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Requeue_CalledMultipleTimes_ShouldOnlyUpdateOnce()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.Requeue();
        fetchedJob.Requeue();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<JobDocument>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Requeue_AfterRemoveFromQueue_ShouldNotUpdate()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.RemoveFromQueue();
        fetchedJob.Requeue();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "processing"),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "enqueued"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Dispose_WithoutRemoveOrRequeue_ShouldRequeue()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.Dispose();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "enqueued"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_AfterRemoveFromQueue_ShouldNotRequeue()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.RemoveFromQueue();
        fetchedJob.Dispose();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "processing"),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "enqueued"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Dispose_AfterRequeue_ShouldNotRequeueTwice()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.Requeue();
        fetchedJob.Dispose();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            _options.JobsContainerName,
            It.Is<JobDocument>(j => j.State == "enqueued"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldOnlyRequeuOnce()
    {
        // Arrange
        var fetchedJob = new CosmosFetchedJob(_mockRepository.Object, _options, _jobDocument);

        // Act
        fetchedJob.Dispose();
        fetchedJob.Dispose();

        // Assert
        _mockRepository.Verify(x => x.UpdateDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<JobDocument>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}