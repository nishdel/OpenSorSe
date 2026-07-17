using TidyMind.Scanner.Models;

namespace TidyMind.Application.Models;

/// <summary>Provides a point-in-time pipeline update without fabricated percentage estimates.</summary>
/// <param name="Stage">The current processing stage.</param>
/// <param name="ScanProgress">The optional direct scanner progress snapshot.</param>
public sealed record ProcessingProgress(ProcessingProgressStage Stage, ScanProgress? ScanProgress = null);
