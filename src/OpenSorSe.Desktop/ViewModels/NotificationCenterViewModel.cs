using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Queues and dismisses non-blocking user notifications without generating business decisions.
/// </summary>
public sealed class NotificationCenterViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan DefaultTemporaryLifetime = TimeSpan.FromSeconds(5);
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly ObservableCollection<NotificationMessage> _notifications = [];
    private readonly TimeProvider _timeProvider;
    private readonly ITimer _timer;
    private long _nextIdentifier;
    private NotificationMessage? _selectedNotification;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a notification center using the system clock.
    /// </summary>
    public NotificationCenterViewModel()
        : this(TimeProvider.System)
    {
    }

    internal NotificationCenterViewModel(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _synchronizationContext = SynchronizationContext.Current;
        Notifications = new ReadOnlyObservableCollection<NotificationMessage>(_notifications);
        _notifications.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNotifications));
        DismissSelectedCommand = new RelayCommand(DismissSelected, () => SelectedNotification is not null);
        _timer = _timeProvider.CreateTimer(static state => ((NotificationCenterViewModel)state!).DispatchExpiration(), this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Gets currently displayed notifications in insertion order.
    /// </summary>
    public ReadOnlyObservableCollection<NotificationMessage> Notifications { get; }

    /// <summary>
    /// Gets whether at least one notification is currently displayed.
    /// </summary>
    public bool HasNotifications => _notifications.Count > 0;

    /// <summary>
    /// Gets or sets the notification selected for manual dismissal.
    /// </summary>
    public NotificationMessage? SelectedNotification
    {
        get => _selectedNotification;
        set
        {
            if (SetProperty(ref _selectedNotification, value))
            {
                DismissSelectedCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the command that dismisses the currently selected notification.
    /// </summary>
    public IRelayCommand DismissSelectedCommand { get; }

    /// <summary>
    /// Queues a validated notification and returns its immutable displayed representation.
    /// </summary>
    /// <param name="request">The user-safe notification request.</param>
    /// <returns>The notification added to the queue.</returns>
    public NotificationMessage Publish(NotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Severity)
            || string.IsNullOrWhiteSpace(request.Message)
            || (request.Lifetime is { } requestedLifetime && requestedLifetime <= TimeSpan.Zero))
        {
            throw new ArgumentException("The notification request is invalid.", nameof(request));
        }

        var createdAtUtc = _timeProvider.GetUtcNow();
        var lifetime = request.Lifetime ?? GetDefaultLifetime(request.Severity);
        var notification = new NotificationMessage(
            $"notification:{Interlocked.Increment(ref _nextIdentifier)}",
            request.Severity,
            request.Message.Trim(),
            createdAtUtc,
            lifetime is null ? null : createdAtUtc + lifetime);
        _notifications.Add(notification);
        return notification;
    }

    /// <summary>
    /// Dismisses the identified notification when it is currently displayed.
    /// </summary>
    /// <param name="notificationId">The current-process notification identifier.</param>
    /// <returns><see langword="true"/> when a notification was removed; otherwise, <see langword="false"/>.</returns>
    public bool Dismiss(string notificationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);
        var notification = _notifications.FirstOrDefault(candidate => string.Equals(candidate.Id, notificationId, StringComparison.Ordinal));
        if (notification is null)
        {
            return false;
        }

        _notifications.Remove(notification);
        if (SelectedNotification == notification)
        {
            SelectedNotification = null;
        }

        return true;
    }

    /// <summary>
    /// Removes notifications whose configured lifetime has elapsed at the supplied UTC time.
    /// </summary>
    /// <param name="utcNow">The UTC time used for deterministic expiration evaluation.</param>
    public void DismissExpired(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The expiration time must be UTC.", nameof(utcNow));
        }

        foreach (var notification in _notifications.Where(notification => notification.ExpiresAtUtc is { } expiry && expiry <= utcNow).ToArray())
        {
            Dismiss(notification.Id);
        }
    }

    /// <summary>
    /// Stops automatic notification expiration.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _timer.Dispose();
        _isDisposed = true;
    }

    private static TimeSpan? GetDefaultLifetime(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Information or NotificationSeverity.Success => DefaultTemporaryLifetime,
        NotificationSeverity.Warning or NotificationSeverity.Error => null,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), "The notification severity is unsupported."),
    };

    private void DismissSelected()
    {
        if (SelectedNotification is not null)
        {
            Dismiss(SelectedNotification.Id);
        }
    }

    private void DispatchExpiration()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_synchronizationContext is null)
        {
            DismissExpired(_timeProvider.GetUtcNow());
            return;
        }

        _synchronizationContext.Post(static state =>
        {
            var center = (NotificationCenterViewModel)state!;
            if (!center._isDisposed)
            {
                center.DismissExpired(center._timeProvider.GetUtcNow());
            }
        }, this);
    }
}
