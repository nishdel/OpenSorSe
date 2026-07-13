namespace TidyMind.Core.Tasks;

/// <summary>
/// Provides an immutable view of a managed background operation.
/// </summary>
public sealed class BackgroundTaskSnapshot
{
    /// <summary>
    /// Initializes a snapshot of a managed background operation.
    /// </summary>
    /// <param name="id">The unique task identifier.</param>
    /// <param name="name">The descriptive operation name.</param>
    /// <param name="status">The current task status.</param>
    /// <param name="progress">The optional normalized progress value.</param>
    /// <param name="failure">The optional failure that stopped the operation.</param>
    public BackgroundTaskSnapshot(
        Guid id,
        string name,
        BackgroundTaskStatus status,
        double? progress,
        Exception? failure)
    {
        Id = id;
        Name = name;
        Status = status;
        Progress = progress;
        Failure = failure;
    }

    /// <summary>Gets the unique task identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the descriptive operation name.</summary>
    public string Name { get; }

    /// <summary>Gets the current task status.</summary>
    public BackgroundTaskStatus Status { get; }

    /// <summary>Gets the normalized progress value when it has been reported.</summary>
    public double? Progress { get; }

    /// <summary>Gets the failure that ended the operation, if any.</summary>
    public Exception? Failure { get; }
}
