using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Models;

/// <summary>Defines all explicit inputs required for one deterministic v0.1 processing run.</summary>
/// <param name="ScanRequest">The root directories and scanner options.</param>
/// <param name="Rules">The ordered rule set, which may be empty.</param>
public sealed record ProcessingRequest(ScanRequest ScanRequest, IReadOnlyList<FileRule> Rules);
