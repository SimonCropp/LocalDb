namespace LocalDb;

/// <summary>
/// A delegate that transforms an auto-resolved timestamp.
/// Receives the auto-resolved timestamp (based on assembly or delegate last modified time)
/// and returns the timestamp to use for template database invalidation.
/// </summary>
/// <param name="timestamp">The auto-resolved timestamp.</param>
/// <returns>The transformed timestamp to use.</returns>
public delegate DateTime TimestampTransform(DateTime timestamp);
