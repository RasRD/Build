using System.Text;
using System.Text.Json;

namespace PersonalAi.Evaluation;

record JudgeVerdict(bool Relevant, string Reasoning);

/// <summary>
/// Asks a locally running Ollama model whether a retrieved chunk is relevant to a query.
/// Requires `ollama serve` (default install runs it as a background service) and the
/// given model already pulled, e.g. `ollama pull llama3`.
/// </summary>
sealed class LlmJudge(string model = "llama3")
{
    static readonly HttpClient Http = new() { BaseAddress = new Uri("http://localhost:11434/") };

    public async Task<JudgeVerdict> JudgeAsync(string query, string chunkText)
    {
        var prompt =
            $$"""
            You are judging search retrieval quality.
            Query: "{{query}}"
            Retrieved text: "{{chunkText}}"

            Does the retrieved text help answer the query? Respond with strict JSON only,
            no other text: {"relevant": true or false, "reasoning": "one short sentence"}
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            format = "json",
            stream = false,
        });

        using var response = await Http.PostAsync(
            "api/chat", new StringContent(requestBody, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString()!;

        using var verdictDoc = JsonDocument.Parse(content);
        var relevant = verdictDoc.RootElement.GetProperty("relevant").GetBoolean();
        var reasoning = verdictDoc.RootElement.GetProperty("reasoning").GetString() ?? "";
        return new JudgeVerdict(relevant, reasoning);
    }
}
