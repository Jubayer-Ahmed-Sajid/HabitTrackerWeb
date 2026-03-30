using System.Text.Json;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class CodeforcesActivityProvider : IExternalActivityProvider
{
    private const string ClientName = "ExternalSync.Codeforces";

    private readonly IHttpClientFactory _httpClientFactory;

    public CodeforcesActivityProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ExternalActivitySource Source => ExternalActivitySource.Codeforces;

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
        var requestUri = $"https://codeforces.com/api/user.status?handle={Uri.EscapeDataString(link.ExternalUserName)}&from=1&count=200";

        using var response = await client.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("status", out var statusNode)
            || !string.Equals(statusNode.GetString(), "OK", StringComparison.Ordinal)
            || !document.RootElement.TryGetProperty("result", out var resultNode)
            || resultNode.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<ExternalActivityItem>();

        foreach (var submission in resultNode.EnumerateArray())
        {
            if (!submission.TryGetProperty("creationTimeSeconds", out var tsNode)
                || !tsNode.TryGetInt64(out var unixTs))
            {
                continue;
            }

            var submissionDate = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixTs).UtcDateTime);
            if (submissionDate != date)
            {
                continue;
            }

            string? problemKey = null;
            if (submission.TryGetProperty("problem", out var problemNode))
            {
                var contestId = problemNode.TryGetProperty("contestId", out var cidNode) ? cidNode.GetRawText() : null;
                var index = problemNode.TryGetProperty("index", out var idxNode) ? idxNode.GetString() : null;
                problemKey = contestId is null || index is null ? null : $"{contestId}{index}";
            }

            items.Add(new ExternalActivityItem(
                Source: Source,
                ActivityDate: date,
                ExternalUserName: link.ExternalUserName,
                MatchKey: problemKey,
                Quantity: 1,
                Description: "Codeforces submission"));
        }

        return items;
    }
}
