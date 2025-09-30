namespace HangfireCosmos.Storage.Resilience;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - requests are allowed through.
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuit is open - requests are rejected immediately.
    /// </summary>
    Open,
    
    /// <summary>
    /// Testing if service has recovered - limited requests are allowed.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Configuration options for the circuit breaker.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before opening the circuit.
    /// Default is 5.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the time to wait before attempting to close the circuit from open state.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Gets or sets the number of successful calls needed to close the circuit from half-open state.
    /// Default is 3.
    /// </summary>
    public int SuccessThreshold { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the timeout for individual operations.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to enable circuit breaker functionality.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validates the circuit breaker options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (FailureThreshold <= 0)
            throw new ArgumentException("FailureThreshold must be greater than 0.", nameof(FailureThreshold));

        if (OpenTimeout <= TimeSpan.Zero)
            throw new ArgumentException("OpenTimeout must be positive.", nameof(OpenTimeout));

        if (SuccessThreshold <= 0)
            throw new ArgumentException("SuccessThreshold must be greater than 0.", nameof(SuccessThreshold));

        if (OperationTimeout <= TimeSpan.Zero)
            throw new ArgumentException("OperationTimeout must be positive.", nameof(OperationTimeout));
    }
}