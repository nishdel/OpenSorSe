using OpenSorSe.Executor.Models;
namespace OpenSorSe.Executor;
/// <summary>Reverses explicit executor undo records conservatively.</summary>
public interface IUndoEngine
{
    /// <summary>Attempts supplied undo records in caller order.</summary>
    /// <param name="records">Explicit undo records.</param>
    /// <param name="progress">Optional progress observer.</param>
    /// <param name="cancellationToken">Token stopping later records.</param>
    /// <returns>Undo outcomes.</returns>
    Task<UndoExecutionResult> UndoAsync(IReadOnlyCollection<UndoRecord> records, IProgress<UndoExecutionProgress>? progress = null, CancellationToken cancellationToken = default);
}
