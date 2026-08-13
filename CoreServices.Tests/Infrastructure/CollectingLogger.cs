using Microsoft.Extensions.Logging;

namespace CoreServices.Tests.Infrastructure;

/// <summary>
/// Collects log entries for unit-test assertions.
/// </summary>
internal sealed class CollectingLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>
    /// Gets the collected log entries.
    /// </summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
