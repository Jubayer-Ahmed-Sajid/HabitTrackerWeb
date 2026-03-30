using System.Net;
using System.Text.Json;
using HabitTrackerWeb.Core.Contracts.Services;
using Microsoft.Extensions.Options;
using WebPush;

namespace HabitTrackerWeb.Services.Push;

public sealed class PushNotificationService : IPushNotificationService
{
    private const string SubscriptionStoreFile = "App_Data/push-subscriptions.json";
    private const string VapidStoreFile = "App_Data/push-vapid-keys.json";
    private const string PreferenceStoreFile = "App_Data/push-preferences.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly SemaphoreSlim _storeLock = new(1, 1);
    private readonly VapidDetails? _vapidDetails;

    public bool IsEnabled { get; }

    public PushNotificationService(
        IWebHostEnvironment environment,
        IOptions<PushNotificationOptions> options,
        ILogger<PushNotificationService> logger)
    {
        _environment = environment;
        _logger = logger;

        var vapid = LoadOrCreateVapidKeys(options.Value);
        if (vapid is null)
        {
            IsEnabled = false;
            _vapidDetails = null;
            return;
        }

        _vapidDetails = vapid;
        IsEnabled = true;
    }

    public string? GetPublicKey()
    {
        return IsEnabled ? _vapidDetails?.PublicKey : null;
    }

    public async Task<bool> GetUserPreferenceAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return false;
        }

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var preferences = await LoadPreferencesUnsafeAsync(cancellationToken);
            return preferences.TryGetValue(userId, out var preference)
                ? preference.Enabled
                : true;
        }
        finally
        {
            _storeLock.Release();
        }
    }

    public async Task SetUserPreferenceAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var preferences = await LoadPreferencesUnsafeAsync(cancellationToken);
            preferences[userId] = new StoredPushPreference
            {
                UserId = userId,
                Enabled = enabled,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await SavePreferencesUnsafeAsync(preferences, cancellationToken);
        }
        finally
        {
            _storeLock.Release();
        }
    }

    public async Task SaveSubscriptionAsync(
        string userId,
        PushSubscriptionRegistration registration,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadSubscriptionsUnsafeAsync(cancellationToken);

            entries.RemoveAll(e => e.UserId == userId && e.Endpoint == registration.Endpoint);
            entries.Add(new StoredPushSubscription
            {
                UserId = userId,
                Endpoint = registration.Endpoint,
                P256Dh = registration.P256Dh,
                Auth = registration.Auth,
                UpdatedAtUtc = DateTime.UtcNow
            });

            await SaveSubscriptionsUnsafeAsync(entries, cancellationToken);
        }
        finally
        {
            _storeLock.Release();
        }
    }

    public async Task RemoveSubscriptionAsync(
        string userId,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadSubscriptionsUnsafeAsync(cancellationToken);
            entries.RemoveAll(e => e.UserId == userId && e.Endpoint == endpoint);
            await SaveSubscriptionsUnsafeAsync(entries, cancellationToken);
        }
        finally
        {
            _storeLock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetSubscribedUserIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return [];
        }

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadSubscriptionsUnsafeAsync(cancellationToken);
            var preferences = await LoadPreferencesUnsafeAsync(cancellationToken);

            return entries
                .Select(e => e.UserId)
                .Distinct(StringComparer.Ordinal)
                .Where(userId => !preferences.TryGetValue(userId, out var preference) || preference.Enabled)
                .ToList();
        }
        finally
        {
            _storeLock.Release();
        }
    }

    public async Task<int> SendAsync(string userId, PushPayload payload, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return 0;
        }

        if (_vapidDetails is null)
        {
            return 0;
        }

        List<StoredPushSubscription> userSubscriptions;

        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadSubscriptionsUnsafeAsync(cancellationToken);
            var preferences = await LoadPreferencesUnsafeAsync(cancellationToken);
            if (preferences.TryGetValue(userId, out var preference) && !preference.Enabled)
            {
                return 0;
            }

            userSubscriptions = entries
                .Where(e => e.UserId == userId)
                .ToList();
        }
        finally
        {
            _storeLock.Release();
        }

        if (userSubscriptions.Count == 0)
        {
            return 0;
        }

        var client = new WebPushClient();
        var pushBody = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Message,
            url = payload.ActionUrl,
            tag = payload.Tag,
            icon = "/favicon.ico",
            badge = "/favicon.ico"
        });

        var delivered = 0;
        var staleEndpoints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var subscription in userSubscriptions)
        {
            try
            {
                var target = new WebPush.PushSubscription(subscription.Endpoint, subscription.P256Dh, subscription.Auth);
                await client.SendNotificationAsync(target, pushBody, _vapidDetails, cancellationToken: cancellationToken);
                delivered++;
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                staleEndpoints.Add(subscription.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deliver push notification for user {UserId}.", userId);
            }
        }

        if (staleEndpoints.Count > 0)
        {
            await RemoveStaleSubscriptionsAsync(userId, staleEndpoints, cancellationToken);
        }

        return delivered;
    }

    private async Task RemoveStaleSubscriptionsAsync(
        string userId,
        HashSet<string> staleEndpoints,
        CancellationToken cancellationToken)
    {
        await _storeLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadSubscriptionsUnsafeAsync(cancellationToken);
            entries.RemoveAll(e => e.UserId == userId && staleEndpoints.Contains(e.Endpoint));
            await SaveSubscriptionsUnsafeAsync(entries, cancellationToken);
        }
        finally
        {
            _storeLock.Release();
        }
    }

    private async Task<List<StoredPushSubscription>> LoadSubscriptionsUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetStorePath(SubscriptionStoreFile);
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<List<StoredPushSubscription>>(stream, JsonOptions, cancellationToken);
        return data ?? [];
    }

    private async Task SaveSubscriptionsUnsafeAsync(List<StoredPushSubscription> entries, CancellationToken cancellationToken)
    {
        var filePath = GetStorePath(SubscriptionStoreFile);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken);
    }

    private async Task<Dictionary<string, StoredPushPreference>> LoadPreferencesUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = GetStorePath(PreferenceStoreFile);
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, StoredPushPreference>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<List<StoredPushPreference>>(stream, JsonOptions, cancellationToken);

        return (data ?? [])
            .GroupBy(item => item.UserId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(item => item.UpdatedAtUtc)
                .First())
            .ToDictionary(item => item.UserId, StringComparer.Ordinal);
    }

    private async Task SavePreferencesUnsafeAsync(
        Dictionary<string, StoredPushPreference> preferences,
        CancellationToken cancellationToken)
    {
        var filePath = GetStorePath(PreferenceStoreFile);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(
            stream,
            preferences.Values.OrderBy(item => item.UserId).ToList(),
            JsonOptions,
            cancellationToken);
    }

    private VapidDetails? LoadOrCreateVapidKeys(PushNotificationOptions options)
    {
        var configuredPublic = options.PublicKey?.Trim() ?? string.Empty;
        var configuredPrivate = options.PrivateKey?.Trim() ?? string.Empty;
        var subject = string.IsNullOrWhiteSpace(options.Subject)
            ? "mailto:habittracker@local.dev"
            : options.Subject;

        if (!string.IsNullOrWhiteSpace(configuredPublic) && !string.IsNullOrWhiteSpace(configuredPrivate))
        {
            return new VapidDetails(subject, configuredPublic, configuredPrivate);
        }

        try
        {
            var filePath = GetStorePath(VapidStoreFile);
            if (File.Exists(filePath))
            {
                var fileData = JsonSerializer.Deserialize<VapidStoreRecord>(File.ReadAllText(filePath), JsonOptions);
                if (fileData is not null
                    && !string.IsNullOrWhiteSpace(fileData.PublicKey)
                    && !string.IsNullOrWhiteSpace(fileData.PrivateKey))
                {
                    return new VapidDetails(subject, fileData.PublicKey, fileData.PrivateKey);
                }
            }

            var generated = VapidHelper.GenerateVapidKeys();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serialized = JsonSerializer.Serialize(new VapidStoreRecord
            {
                PublicKey = generated.PublicKey,
                PrivateKey = generated.PrivateKey
            }, JsonOptions);

            File.WriteAllText(filePath, serialized);

            _logger.LogInformation("Generated VAPID keys at {KeyPath}.", filePath);
            return new VapidDetails(subject, generated.PublicKey, generated.PrivateKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Push notifications are disabled because VAPID keys could not be loaded.");
            return null;
        }
    }

    private string GetStorePath(string relativePath)
    {
        return Path.Combine(_environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class StoredPushSubscription
    {
        public string UserId { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string P256Dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }

    private sealed class StoredPushPreference
    {
        public string UserId { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime UpdatedAtUtc { get; set; }
    }

    private sealed class VapidStoreRecord
    {
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
    }
}
