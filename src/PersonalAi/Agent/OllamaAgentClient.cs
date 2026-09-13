using System.Text;
using System.Text.Json;

namespace PersonalAi.Agent;

sealed record AgentMessage(string Role, string Content);
sealed record AgentDecision(string Type, string? Tool, Dictionary<string, string>? Arguments, string? Answer);
sealed record AgentModelResponse(string RawContent, AgentDecision Decision);

sealed class OllamaAgentClient(string model, string baseUrl)
{
    readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<AgentModelResponse> NextAsync(IReadOnlyList<AgentMessage> messages)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = messages.Select(message => new { role = message.Role, content = message.Content }),
            format = "json",
            stream = false
        });

        using var response = await _http.PostAsync(
            "api/chat", new StringContent(requestBody, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("Ollama returned an empty agent response.");

        var decision = JsonSerializer.Deserialize<AgentDecision>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Could not parse the agent decision.");

        return new AgentModelResponse(content, decision);
    }
}
