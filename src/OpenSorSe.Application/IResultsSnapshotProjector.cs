using OpenSorSe.Application.Models;

namespace OpenSorSe.Application;

/// <summary>
/// Projects a completed processing session into an immutable, presentation-safe result snapshot.
/// </summary>
public interface IResultsSnapshotProjector
{
    /// <summary>
    /// Creates a complete in-memory result snapshot from a completed processing session.
    /// </summary>
    /// <param name="sessionResult">The completed session result to project.</param>
    /// <returns>An immutable snapshot that contains no raw content hashes.</returns>
    ResultsSnapshot Project(ProcessingSessionResult sessionResult);
}
