using FluentAssertions;
using Hangfire;
using HangfireCosmos.Storage.Documents;
using Newtonsoft.Json;
using Xunit;

namespace HangfireCosmos.Tests.Documents;

/// <summary>
/// Unit tests for the JobDocument class and related classes.
/// </summary>
public class JobDocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var jobDocument = new JobDocument();

        // Assert
        jobDocument.DocumentType.Should().Be("job");
        jobDocument.JobId.Should().Be(string.Empty);
        jobDocument.QueueName.Should().Be("default");
        jobDocument.State.Should().Be(string.Empty);
        jobDocument.StateData.Should().NotBeNull().And.BeEmpty();
        jobDocument.StateHistory.Should().NotBeNull().And.BeEmpty();
        jobDocument.InvocationData.Should().NotBeNull();
        jobDocument.Parameters.Should().NotBeNull().And.BeEmpty();
        jobDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        jobDocument.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobId_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        const string expectedJobId = "job-123";

        // Act
        jobDocument.JobId = expectedJobId;

        // Assert
        jobDocument.JobId.Should().Be(expectedJobId);
    }

    [Fact]
    public void QueueName_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        const string expectedQueueName = "critical";

        // Act
        jobDocument.QueueName = expectedQueueName;

        // Assert
        jobDocument.QueueName.Should().Be(expectedQueueName);
    }

    [Fact]
    public void State_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        const string expectedState = "Processing";

        // Act
        jobDocument.State = expectedState;

        // Assert
        jobDocument.State.Should().Be(expectedState);
    }

    [Fact]
    public void StateData_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedStateData = new Dictionary<string, string>
        {
            { "StartedAt", "2023-01-01T00:00:00Z" },
            { "ServerId", "server-1" }
        };

        // Act
        jobDocument.StateData = expectedStateData;

        // Assert
        jobDocument.StateData.Should().BeEquivalentTo(expectedStateData);
    }

    [Fact]
    public void StateHistory_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedStateHistory = new List<StateHistoryEntry>
        {
            new() { State = "Enqueued", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { State = "Processing", CreatedAt = DateTime.UtcNow }
        };

        // Act
        jobDocument.StateHistory = expectedStateHistory;

        // Assert
        jobDocument.StateHistory.Should().BeEquivalentTo(expectedStateHistory);
    }

    [Fact]
    public void InvocationData_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedInvocationData = new InvocationData
        {
            Type = "MyNamespace.MyClass",
            Method = "MyMethod",
            ParameterTypes = new[] { "System.String", "System.Int32" },
            Arguments = new[] { "\"test\"", "42" }
        };

        // Act
        jobDocument.InvocationData = expectedInvocationData;

        // Assert
        jobDocument.InvocationData.Should().BeEquivalentTo(expectedInvocationData);
    }

    [Fact]
    public void Parameters_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedParameters = new Dictionary<string, string>
        {
            { "CurrentCulture", "en-US" },
            { "CurrentUICulture", "en-US" }
        };

        // Act
        jobDocument.Parameters = expectedParameters;

        // Assert
        jobDocument.Parameters.Should().BeEquivalentTo(expectedParameters);
    }

    [Fact]
    public void CreatedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedCreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        jobDocument.CreatedAt = expectedCreatedAt;

        // Assert
        jobDocument.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public void UpdatedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var jobDocument = new JobDocument();
        var expectedUpdatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        jobDocument.UpdatedAt = expectedUpdatedAt;

        // Assert
        jobDocument.UpdatedAt.Should().Be(expectedUpdatedAt);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var jobDocument = new JobDocument
        {
            Id = "job-doc-1",
            PartitionKey = "jobs",
            JobId = "job-123",
            QueueName = "critical",
            State = "Processing",
            StateData = new Dictionary<string, string> { { "StartedAt", "2023-01-01T00:00:00Z" } },
            StateHistory = new List<StateHistoryEntry>
            {
                new() { State = "Enqueued", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            },
            InvocationData = new InvocationData
            {
                Type = "MyClass",
                Method = "MyMethod",
                ParameterTypes = new[] { "System.String" },
                Arguments = new[] { "\"test\"" }
            },
            Parameters = new Dictionary<string, string> { { "Culture", "en-US" } },
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonConvert.SerializeObject(jobDocument);
        var deserializedDocument = JsonConvert.DeserializeObject<JobDocument>(json);

        // Assert
        deserializedDocument.Should().NotBeNull();
        deserializedDocument!.Should().BeEquivalentTo(jobDocument);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var jobDocument = new JobDocument
        {
            JobId = "job-123",
            QueueName = "default",
            State = "Enqueued"
        };

        // Act
        var json = JsonConvert.SerializeObject(jobDocument);

        // Assert
        json.Should().Contain("\"jobId\":");
        json.Should().Contain("\"queueName\":");
        json.Should().Contain("\"state\":");
        json.Should().Contain("\"stateData\":");
        json.Should().Contain("\"stateHistory\":");
        json.Should().Contain("\"invocationData\":");
        json.Should().Contain("\"parameters\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"updatedAt\":");
    }

    [Theory]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("critical")]
    [InlineData("low-priority")]
    public void QueueName_WithVariousValues_ShouldSetCorrectly(string queueName)
    {
        // Arrange
        var jobDocument = new JobDocument();

        // Act
        jobDocument.QueueName = queueName;

        // Assert
        jobDocument.QueueName.Should().Be(queueName);
    }

    [Theory]
    [InlineData("Enqueued")]
    [InlineData("Processing")]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    [InlineData("Deleted")]
    public void State_WithHangfireStates_ShouldSetCorrectly(string state)
    {
        // Arrange
        var jobDocument = new JobDocument();

        // Act
        jobDocument.State = state;

        // Assert
        jobDocument.State.Should().Be(state);
    }
}

/// <summary>
/// Unit tests for the StateHistoryEntry class.
/// </summary>
public class StateHistoryEntryTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var entry = new StateHistoryEntry();

        // Assert
        entry.State.Should().Be(string.Empty);
        entry.Reason.Should().BeNull();
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entry.Data.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void State_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var entry = new StateHistoryEntry();
        const string expectedState = "Processing";

        // Act
        entry.State = expectedState;

        // Assert
        entry.State.Should().Be(expectedState);
    }

    [Fact]
    public void Reason_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var entry = new StateHistoryEntry();
        const string expectedReason = "Job started processing";

        // Act
        entry.Reason = expectedReason;

        // Assert
        entry.Reason.Should().Be(expectedReason);
    }

    [Fact]
    public void CreatedAt_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var entry = new StateHistoryEntry();
        var expectedCreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        entry.CreatedAt = expectedCreatedAt;

        // Assert
        entry.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public void Data_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var entry = new StateHistoryEntry();
        var expectedData = new Dictionary<string, string>
        {
            { "StartedAt", "2023-01-01T00:00:00Z" },
            { "ServerId", "server-1" }
        };

        // Act
        entry.Data = expectedData;

        // Assert
        entry.Data.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var entry = new StateHistoryEntry
        {
            State = "Processing",
            Reason = "Job started",
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Data = new Dictionary<string, string> { { "ServerId", "server-1" } }
        };

        // Act
        var json = JsonConvert.SerializeObject(entry);
        var deserializedEntry = JsonConvert.DeserializeObject<StateHistoryEntry>(json);

        // Assert
        deserializedEntry.Should().NotBeNull();
        deserializedEntry!.Should().BeEquivalentTo(entry);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var entry = new StateHistoryEntry
        {
            State = "Processing",
            Reason = "Job started"
        };

        // Act
        var json = JsonConvert.SerializeObject(entry);

        // Assert
        json.Should().Contain("\"state\":");
        json.Should().Contain("\"reason\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"data\":");
    }
}

/// <summary>
/// Unit tests for the InvocationData class.
/// </summary>
public class InvocationDataTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var invocationData = new InvocationData();

        // Assert
        invocationData.Type.Should().Be(string.Empty);
        invocationData.Method.Should().Be(string.Empty);
        invocationData.ParameterTypes.Should().NotBeNull().And.BeEmpty();
        invocationData.Arguments.Should().NotBeNull().And.BeEmpty();
        invocationData.GenericArguments.Should().BeNull();
    }

    [Fact]
    public void Type_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        const string expectedType = "MyNamespace.MyClass, MyAssembly";

        // Act
        invocationData.Type = expectedType;

        // Assert
        invocationData.Type.Should().Be(expectedType);
    }

    [Fact]
    public void Method_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        const string expectedMethod = "ProcessData";

        // Act
        invocationData.Method = expectedMethod;

        // Assert
        invocationData.Method.Should().Be(expectedMethod);
    }

    [Fact]
    public void ParameterTypes_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var expectedParameterTypes = new[] { "System.String", "System.Int32", "System.Boolean" };

        // Act
        invocationData.ParameterTypes = expectedParameterTypes;

        // Assert
        invocationData.ParameterTypes.Should().BeEquivalentTo(expectedParameterTypes);
    }

    [Fact]
    public void Arguments_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var expectedArguments = new[] { "\"test string\"", "42", "true" };

        // Act
        invocationData.Arguments = expectedArguments;

        // Assert
        invocationData.Arguments.Should().BeEquivalentTo(expectedArguments);
    }

    [Fact]
    public void GenericArguments_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var expectedGenericArguments = new[] { "System.String", "System.Int32" };

        // Act
        invocationData.GenericArguments = expectedGenericArguments;

        // Assert
        invocationData.GenericArguments.Should().BeEquivalentTo(expectedGenericArguments);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAllProperties()
    {
        // Arrange
        var invocationData = new InvocationData
        {
            Type = "MyClass",
            Method = "MyMethod",
            ParameterTypes = new[] { "System.String", "System.Int32" },
            Arguments = new[] { "\"test\"", "42" },
            GenericArguments = new[] { "System.String" }
        };

        // Act
        var json = JsonConvert.SerializeObject(invocationData);
        var deserializedData = JsonConvert.DeserializeObject<InvocationData>(json);

        // Assert
        deserializedData.Should().NotBeNull();
        deserializedData!.Should().BeEquivalentTo(invocationData);
    }

    [Fact]
    public void JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var invocationData = new InvocationData
        {
            Type = "MyClass",
            Method = "MyMethod"
        };

        // Act
        var json = JsonConvert.SerializeObject(invocationData);

        // Assert
        json.Should().Contain("\"type\":");
        json.Should().Contain("\"method\":");
        json.Should().Contain("\"parameterTypes\":");
        json.Should().Contain("\"arguments\":");
        json.Should().Contain("\"genericArguments\":");
    }

    [Fact]
    public void JsonSerialization_WithNullGenericArguments_ShouldSerializeCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData
        {
            Type = "MyClass",
            Method = "MyMethod",
            ParameterTypes = new[] { "System.String" },
            Arguments = new[] { "\"test\"" },
            GenericArguments = null
        };

        // Act
        var json = JsonConvert.SerializeObject(invocationData);
        var deserializedData = JsonConvert.DeserializeObject<InvocationData>(json);

        // Assert
        deserializedData.Should().NotBeNull();
        deserializedData!.GenericArguments.Should().BeNull();
    }

    [Fact]
    public void ParameterTypes_WithEmptyArray_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var parameterTypes = Array.Empty<string>();

        // Act
        invocationData.ParameterTypes = parameterTypes;

        // Assert
        invocationData.ParameterTypes.Should().BeEquivalentTo(parameterTypes);
    }

    [Fact]
    public void ParameterTypes_WithSingleElement_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var parameterTypes = new[] { "System.String" };

        // Act
        invocationData.ParameterTypes = parameterTypes;

        // Assert
        invocationData.ParameterTypes.Should().BeEquivalentTo(parameterTypes);
    }

    [Fact]
    public void ParameterTypes_WithMultipleElements_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var parameterTypes = new[] { "System.String", "System.Int32", "System.Boolean" };

        // Act
        invocationData.ParameterTypes = parameterTypes;

        // Assert
        invocationData.ParameterTypes.Should().BeEquivalentTo(parameterTypes);
    }

    [Fact]
    public void Arguments_WithEmptyArray_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var arguments = Array.Empty<string>();

        // Act
        invocationData.Arguments = arguments;

        // Assert
        invocationData.Arguments.Should().BeEquivalentTo(arguments);
    }

    [Fact]
    public void Arguments_WithSingleElement_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var arguments = new[] { "\"test\"" };

        // Act
        invocationData.Arguments = arguments;

        // Assert
        invocationData.Arguments.Should().BeEquivalentTo(arguments);
    }

    [Fact]
    public void Arguments_WithMultipleElements_ShouldSetCorrectly()
    {
        // Arrange
        var invocationData = new InvocationData();
        var arguments = new[] { "\"test\"", "42", "true" };

        // Act
        invocationData.Arguments = arguments;

        // Assert
        invocationData.Arguments.Should().BeEquivalentTo(arguments);
    }
}