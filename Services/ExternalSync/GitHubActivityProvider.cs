using System.Net.Http.Headers;
using System.Text.Json;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class GitHubActivityProvider : IExternalActivityProvider
{
    private const string ClientName = "ExternalSync.GitHub";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubActivityProvider> _logger;

    public GitHubActivityProvider(IHttpClientFactory httpClientFactory, ILogger<GitHubActivityProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public ExternalActivitySource Source => ExternalActivitySource.GitHub;

    public async Task<IReadOnlyList<ExternalActivityItem>> PullDailyActivityAsync(
        ExternalAccountLink link,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(link.ExternalUserName))
        {
            return [];
        }

        var client = _httpClientFactory.CreateClient(ClientName);

        if (!string.IsNullOrWhiteSpace(link.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", link.AccessToken);
        }

        var requestUri = $"https://api.github.com/users/{Uri.EscapeDataString(link.ExternalUserName)}/events?per_page=100";
        using var response = await client.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug(
                "GitHub pull skipped for {User}. Status code: {StatusCode}",
                link.ExternalUserName,
                response.StatusCode);
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<ExternalActivityItem>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (!element.TryGetProperty("type", out var typeNode)
                || !string.Equals(typeNode.GetString(), "PushEvent", StringComparison.Ordinal))
            {
                continue;
            }

            if (!element.TryGetProperty("created_at", out var createdAtNode)
                || !DateTime.TryParse(createdAtNode.GetString(), out var createdAtUtc))
            {
                continue;
            }

            if (DateOnly.FromDateTime(createdAtUtc.ToUniversalTime()) != date)
            {
                continue;
            }

            string? repoName = null;
            if (element.TryGetProperty("repo", out var repoNode)
                && repoNode.TryGetProperty("name", out var nameNode))
            {
                repoName = nameNode.GetString();
            }

            results.Add(new ExternalActivityItem(
                Source: Source,
                ActivityDate: date,
                ExternalUserName: link.ExternalUserName,
                MatchKey: repoName,
                Quantity: 1,
                Description: "GitHub PushEvent"));
        }

        return results;
    }
}
