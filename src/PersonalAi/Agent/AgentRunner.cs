namespace PersonalAi.Agent;

sealed class AgentRunner(OllamaAgentClient client, AgentTools tools, int maxSteps)
{
    const string SystemPrompt = """
        You are an agent over a directory of Markdown notes.

        Available tools:
        1. search_notes(query): semantic search over note chunks. Returns JSON with fileName, score, and text.
        2. read_note(fileName): reads the full Markdown note returned by search_notes.

        Decide the next action yourself. Use search_notes when the answer depends on the notes.
        Use read_note when you need the full contents of a particular note, for example when the user asks
        for a detailed explanation or summary and a search snippet is insufficient.

        Return strict JSON only, in exactly one of these forms:
        {"type":"tool_call","tool":"search_notes","arguments":{"query":"..."}}
        {"type":"tool_call","tool":"read_note","arguments":{"fileName":"..."}}
        {"type":"final_answer","answer":"..."}

        Never invent tool results. A tool result will be sent back to you after each tool call.
        """;

    public async Task<string> RunAsync(string userMessage)
    {
        var trace = new AgentTrace();
        trace.Add("USER", userMessage);

        var messages = new List<AgentMessage>
        {
            new("system", SystemPrompt),
            new("user", userMessage)
        };

        for (var step = 0; step < maxSteps; step++)
        {
            var response = await client.NextAsync(messages);
            messages.Add(new AgentMessage("assistant", response.RawContent));

            if (response.Decision.Type == "final_answer")
            {
                var answer = response.Decision.Answer
                    ?? throw new InvalidOperationException("Agent returned final_answer without 'answer'.");
                trace.Add("MODEL final_answer", answer);
                return answer;
            }

            if (response.Decision.Type != "tool_call" || string.IsNullOrWhiteSpace(response.Decision.Tool))
                throw new InvalidOperationException($"Unsupported agent response: {response.RawContent}");

            var call = new AgentToolCall(
                response.Decision.Tool,
                response.Decision.Arguments ?? new Dictionary<string, string>());

            var arguments = string.Join(", ", call.Arguments.Select(pair => $"{pair.Key}={pair.Value}"));
            trace.Add("MODEL tool_call", $"{call.Name}({arguments})");

            var toolResult = await tools.ExecuteAsync(call);
            trace.Add($"TOOL {call.Name}", toolResult);

            messages.Add(new AgentMessage(
                "user",
                $"Tool result for {call.Name}:\n{toolResult}\nChoose the next action."));
        }

        throw new InvalidOperationException($"Agent reached the MaxSteps limit ({maxSteps}) without a final answer.");
    }
}
