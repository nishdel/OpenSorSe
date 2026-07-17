using CommunityToolkit.Mvvm.Input;

namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Presents static application metadata and emits external-resource requests without launching processes.
/// </summary>
public sealed class AboutViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes static metadata commands.
    /// </summary>
    public AboutViewModel()
    {
        OpenRepositoryCommand = new RelayCommand(() => RequestExternalLink(RepositoryUri));
        OpenDocumentationCommand = new RelayCommand(() => RequestExternalLink(DocumentationUri));
    }

    /// <summary>
    /// Occurs when the user requests that a host open a vetted external resource.
    /// </summary>
    public event EventHandler<Uri>? ExternalLinkRequested;

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName => "TidyMind";

    /// <summary>
    /// Gets the declared v0.1 application version.
    /// </summary>
    public string Version => "v0.1.0";

    /// <summary>
    /// Gets the license declared by the v0.1 implementation specification.
    /// </summary>
    public string License => "MIT License";

    /// <summary>
    /// Gets a concise acknowledgement of the local-first project intent.
    /// </summary>
    public string Acknowledgements => "Built with .NET and Avalonia UI for local-first file organization.";

    /// <summary>
    /// Gets the project repository URI.
    /// </summary>
    public Uri RepositoryUri { get; } = new("https://github.com/TidyMind/TidyMind", UriKind.Absolute);

    /// <summary>
    /// Gets the public project documentation URI.
    /// </summary>
    public Uri DocumentationUri { get; } = new("https://github.com/TidyMind/TidyMind/tree/main/docs", UriKind.Absolute);

    /// <summary>
    /// Gets the command that requests opening the project repository.
    /// </summary>
    public IRelayCommand OpenRepositoryCommand { get; }

    /// <summary>
    /// Gets the command that requests opening project documentation.
    /// </summary>
    public IRelayCommand OpenDocumentationCommand { get; }

    private void RequestExternalLink(Uri uri) => ExternalLinkRequested?.Invoke(this, uri);
}
