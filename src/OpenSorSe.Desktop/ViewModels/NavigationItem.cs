namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Pairs a shell destination with a concise, user-facing navigation label.
/// </summary>
public sealed record NavigationItem(NavigationDestination Destination, string Label);
