namespace HangfireCosmos.Storage.Resilience;

/// <summary>
/// Exception thrown when a circuit breaker is in the open state and rejects requests.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CircuitBreakerOpenException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CircuitBreakerOpenException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the operation name that was rejected.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets or sets the time when the circuit breaker will attempt to transition to half-open state.
    /// </summary>
    public DateTime? RetryAfter { get; set; }
}