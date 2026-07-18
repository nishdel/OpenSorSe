using OpenSorSe.Application.Catalog;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies catalog presentation remains opt-in and opens only application-owned saved snapshots.
/// </summary>
public sealed class CatalogViewModelTests
{
    /// <summary>
    /// Verifies a disabled setting leaves persistence unread and presents an opt-in explanation.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenDisabled_DoesNotReadTheCatalog()
    {
        var store = new RecordingCatalogStore();
        var viewModel = new CatalogViewModel(new TestConfigurationService(enabled: false), store);

        await viewModel.RefreshAsync();

        Assert.Equal(0, store.ListCallCount);
        Assert.True(viewModel.HasNoEntries);
        Assert.Contains("disabled", viewModel.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies an enabled catalog can list and request shell-owned opening of a selected saved snapshot.
    /// </summary>
    [Fact]
    public async Task RefreshAndOpenAsync_EnabledCatalog_ListsAndRaisesSavedEntry()
    {
        var entry = CreateEntry();
        var store = new RecordingCatalogStore(entry);
        var viewModel = new CatalogViewModel(new TestConfigurationService(enabled: true), store);
        CatalogEntry? opened = null;
        viewModel.EntryOpened += (_, value) => opened = value;

        await viewModel.RefreshAsync();
        viewModel.SelectedEntry = Assert.Single(viewModel.Entries);
        await viewModel.OpenSelectedCommand.ExecuteAsync(null);

        Assert.Equal(1, store.ListCallCount);
        Assert.Equal(1, store.LoadCallCount);
        Assert.Same(entry, opened);
        Assert.Contains("not been refreshed", viewModel.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies selected removal and two-step clear operate only after explicit commands against application-owned catalog storage.
    /// </summary>
    [Fact]
    public async Task MaintenanceCommands_RemoveSelectedAndRequireConfirmationBeforeClear()
    {
        var first = CreateEntry();
        var second = CreateEntry() with { Id = "catalog:two", SavedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(1) };
        var store = new MutableCatalogStore(first, second);
        var viewModel = new CatalogViewModel(new TestConfigurationService(enabled: true), store);

        await viewModel.RefreshAsync();
        viewModel.SelectedEntry = viewModel.Entries.Single(entry => entry.Id == first.Id);
        await viewModel.RemoveSelectedCommand.ExecuteAsync(null);

        Assert.Equal([second.Id], viewModel.Entries.Select(entry => entry.Id));
        Assert.Equal(0, store.ClearCallCount);
        viewModel.RequestClearAllCommand.Execute(null);
        Assert.True(viewModel.IsClearConfirmationPending);
        Assert.Equal(0, store.ClearCallCount);
        await viewModel.ConfirmClearAllCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsClearConfirmationPending);
        Assert.Empty(viewModel.Entries);
        Assert.Equal(1, store.ClearCallCount);
    }

    /// <summary>
    /// Verifies a selected snapshot name can be normalized, replaced, and explicitly cleared without changing its scope.
    /// </summary>
    [Fact]
    public async Task SaveDisplayNameAsync_SetsAndClearsOnlyApplicationOwnedIdentity()
    {
        var entry = CreateEntry() with { SourceRoots = ["C:\\Selected"] };
        var store = new MutableCatalogStore(entry);
        using var viewModel = new CatalogViewModel(new TestConfigurationService(enabled: true), store);
        var changes = 0;
        viewModel.CatalogChanged += (_, _) => changes++;

        await viewModel.RefreshAsync();
        viewModel.SelectedEntry = Assert.Single(viewModel.Entries);
        Assert.Equal("Unnamed snapshot", viewModel.SelectedEntry.Title);
        Assert.Contains("C:\\Selected", viewModel.SelectedEntry.SourceScope, StringComparison.Ordinal);

        viewModel.DisplayNameInput = "  Quarterly baseline  ";
        await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

        Assert.Equal("Quarterly baseline", viewModel.SelectedEntry!.DisplayName);
        Assert.Equal(["C:\\Selected"], viewModel.SelectedEntry.SourceRoots);
        Assert.Equal(1, store.SaveCallCount);

        viewModel.DisplayNameInput = "   ";
        await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

        Assert.Null(viewModel.SelectedEntry!.DisplayName);
        Assert.Equal(2, store.SaveCallCount);
        Assert.Equal(2, changes);
    }

    /// <summary>Verifies invalid names are rejected before the store is called.</summary>
    [Fact]
    public async Task SaveDisplayNameAsync_InvalidName_DoesNotWrite()
    {
        var store = new MutableCatalogStore(CreateEntry());
        using var viewModel = new CatalogViewModel(new TestConfigurationService(enabled: true), store);
        await viewModel.RefreshAsync();
        viewModel.SelectedEntry = Assert.Single(viewModel.Entries);
        viewModel.DisplayNameInput = new string('x', CatalogLimits.MaximumDisplayNameLength + 1);

        await viewModel.SaveDisplayNameCommand.ExecuteAsync(null);

        Assert.Equal(0, store.SaveCallCount);
        Assert.Contains("no longer", viewModel.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies maximum source scope remains bounded in one accessible list-row summary.</summary>
    [Fact]
    public void CatalogEntryRow_MaximumSourceScope_UsesBoundedSummary()
    {
        var summary = new CatalogEntrySummary("catalog:many-roots", DateTimeOffset.UnixEpoch, 1, 1, 0, 0)
        {
            SourceRoots = Enumerable.Range(0, CatalogLimits.MaximumSourceRootCount).Select(index => $"/source/{index}").ToArray(),
        };

        var row = CatalogEntryRow.FromSummary(summary);

        Assert.Contains("/source/0", row.SourceScope, StringComparison.Ordinal);
        Assert.DoesNotContain("/source/31", row.SourceScope, StringComparison.Ordinal);
        Assert.Contains($"+{CatalogLimits.MaximumSourceRootCount - 3} more", row.SourceScope, StringComparison.Ordinal);
    }

    private static CatalogEntry CreateEntry()
    {
        var file = new ResultFile("file:one", "C:\\Selected\\one.txt", "one.txt", ".txt", 1, DateTimeOffset.UnixEpoch, FileCategory.Document, "Document", DuplicateStatus.Unique, null, false);
        var snapshot = new ResultsSnapshot(
            "session:one",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [file],
            [new ResultDirectory("C:\\Selected", "Selected")],
            [],
            [],
            [],
            new ResultsSnapshotStatistics(1, 1, 0, 0, 0, 0, 0),
            true);
        return new CatalogEntry("catalog:one", DateTimeOffset.UnixEpoch, snapshot, []);
    }

    private sealed class TestConfigurationService : IConfigurationService
    {
        public TestConfigurationService(bool enabled)
        {
            Current = new ApplicationSettings { Catalog = new CatalogSettings { Enabled = enabled } };
        }

        public ApplicationSettings Current { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken)
        {
            Current = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingCatalogStore : IResultsCatalogStore
    {
        private readonly CatalogEntry? _entry;

        public RecordingCatalogStore(CatalogEntry? entry = null)
        {
            _entry = entry;
        }

        public int ListCallCount { get; private set; }

        public int LoadCallCount { get; private set; }

        public Task<IReadOnlyList<CatalogEntrySummary>> ListAsync(CancellationToken cancellationToken)
        {
            ListCallCount++;
            IReadOnlyList<CatalogEntrySummary> summaries = _entry is null
                ? []
                : [new CatalogEntrySummary(_entry.Id, _entry.SavedAtUtc, 1, 1, 0, 0)];
            return Task.FromResult(summaries);
        }

        public Task<CatalogEntry?> LoadAsync(string entryId, CancellationToken cancellationToken)
        {
            LoadCallCount++;
            return Task.FromResult(string.Equals(entryId, _entry?.Id, StringComparison.Ordinal) ? _entry : null);
        }

        public Task<CatalogEntrySummary> SaveAsync(CatalogEntry entry, CancellationToken cancellationToken) => Task.FromResult(
            new CatalogEntrySummary(entry.Id, entry.SavedAtUtc, entry.Snapshot.Files.Count, entry.Snapshot.Directories.Count, 0, 0));

        public Task<bool> RemoveAsync(string entryId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class MutableCatalogStore : IResultsCatalogStore
    {
        private readonly List<CatalogEntry> _entries;

        public MutableCatalogStore(params CatalogEntry[] entries)
        {
            _entries = entries.ToList();
        }

        public int ClearCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public Task<IReadOnlyList<CatalogEntrySummary>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CatalogEntrySummary>>(
            _entries.OrderByDescending(entry => entry.SavedAtUtc)
                .Select(ToSummary)
                .ToArray());

        public Task<CatalogEntry?> LoadAsync(string entryId, CancellationToken cancellationToken) => Task.FromResult<CatalogEntry?>(
            _entries.FirstOrDefault(entry => string.Equals(entry.Id, entryId, StringComparison.Ordinal)));

        public Task<CatalogEntrySummary> SaveAsync(CatalogEntry entry, CancellationToken cancellationToken)
        {
            SaveCallCount++;
            _entries.RemoveAll(candidate => string.Equals(candidate.Id, entry.Id, StringComparison.Ordinal));
            _entries.Add(entry);
            return Task.FromResult(ToSummary(entry));
        }

        public Task<bool> RemoveAsync(string entryId, CancellationToken cancellationToken) => Task.FromResult(
            _entries.RemoveAll(entry => string.Equals(entry.Id, entryId, StringComparison.Ordinal)) > 0);

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            ClearCallCount++;
            _entries.Clear();
            return Task.CompletedTask;
        }

        private static CatalogEntrySummary ToSummary(CatalogEntry entry) => new(entry.Id, entry.SavedAtUtc, 1, 1, 0, 0)
        {
            DisplayName = entry.DisplayName,
            SourceRoots = entry.SourceRoots,
        };
    }
}
