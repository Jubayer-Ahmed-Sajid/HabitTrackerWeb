using System.Text;
using System.Text.Json;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.Services.ExternalSync;

public sealed class LeetCodeActivityProvider : IExternalActivityProvider
{
    private const string ClientName = "ExternalSync.LeetCode";

    private readonly IHttpClientFactory _httpClientFactory;

    public LeetCodeActivityProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ExternalActivitySource Source => ExternalActivitySource.LeetCode;

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

        var payload = JsonSerializer.Serialize(new
        {
            operationName = "recentAcSubmissionList",
            query = "query recentAcSubmissionList($username: String!) { recentAcSubmissionList(username: $username) { titleSlug timestamp } }",
            variables = new { username = link.ExternalUserName }
        });

        using var response = await client.PostAsync(
            "https://leetcode.com/graphql",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var dataNode)
            || !dataNode.TryGetProperty("recentAcSubmissionList", out var submissionsNode)
            || submissionsNode.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<ExternalActivityItem>();

        foreach (var item in submissionsNode.EnumerateArray())
        {
            if (!item.TryGetProperty("timestamp", out var timestampNode)
                || !long.TryParse(timestampNode.GetString(), out var unixTime))
            {
                continue;
            }

            var submissionDate = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime);
            if (submissionDate != date)
            {
                continue;
            }

            var titleSlug = item.TryGetProperty("titleSlug", out var slugNode)
                ? slugNode.GetString()
                : null;

            results.Add(new ExternalActivityItem(
                Source: Source,
                ActivityDate: date,
                ExternalUserName: link.ExternalUserName,
                MatchKey: titleSlug,
                Quantity: 1,
                Description: "LeetCode accepted submission"));
        }

        return results;
    }
}
