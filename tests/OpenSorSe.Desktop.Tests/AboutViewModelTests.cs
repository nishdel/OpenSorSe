using OpenSorSe.Desktop.ViewModels;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies static application metadata and safe external-link requests.
/// </summary>
public sealed class AboutViewModelTests
{
    /// <summary>
    /// Verifies v0.1 metadata is deterministic and non-empty.
    /// </summary>
    [Fact]
    public void Constructor_ExposesDeclaredApplicationMetadata()
    {
        var viewModel = new AboutViewModel();

        Assert.Equal("OpenSorSe", viewModel.ApplicationName);
        Assert.Equal("v0.1.0", viewModel.Version);
        Assert.Equal("MIT License", viewModel.License);
        Assert.NotEmpty(viewModel.Acknowledgements);
    }

    /// <summary>
    /// Verifies link commands emit vetted URIs without launching an external process.
    /// </summary>
    [Fact]
    public void LinkCommands_EmitRepositoryAndDocumentationRequests()
    {
        var viewModel = new AboutViewModel();
        var requestedUris = new List<Uri>();
        viewModel.ExternalLinkRequested += (_, uri) => requestedUris.Add(uri);

        viewModel.OpenRepositoryCommand.Execute(null);
        viewModel.OpenDocumentationCommand.Execute(null);

        Assert.Equal([viewModel.RepositoryUri, viewModel.DocumentationUri], requestedUris);
        Assert.All(requestedUris, uri => Assert.Equal(Uri.UriSchemeHttps, uri.Scheme));
    }
}
