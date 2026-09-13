using System.Text.Json;
using PersonalAi.Embeddings;
using PersonalAi.Models;
using PersonalAi.Retrieval;

namespace PersonalAi.Agent;

sealed record AgentToolCall(string Name, IReadOnlyDictionary<string, string> Arguments);

sealed class AgentTools(
    string notesDirectory,
    IReadOnlyList<NoteChunk> chunks,
    IEmbeddingService embeddingService,
    int topK)
{
    public async Task<string> ExecuteAsync(AgentToolCall call)
    {
        return call.Name switch
        {
            "search_notes" => await SearchNotesAsync(GetRequiredArgument(call, "query")),
            "read_note" => ReadNote(GetRequiredArgument(call, "fileName")),
            _ => throw new InvalidOperationException($"Unknown tool '{call.Name}'.")
        };
    }

    async Task<string> SearchNotesAsync(string query)
    {
        var queryEmbedding = await embeddingService.EmbedAsync(query);
        var results = SimilaritySearch.TopK(chunks, queryEmbedding, topK)
            .Select(result => new
            {
                fileName = result.Chunk.SourceFile,
                score = Math.Round(result.Score, 3),
                text = result.Chunk.Text
            });

        return JsonSerializer.Serialize(results);
    }

    string ReadNote(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var path = Path.Combine(notesDirectory, safeFileName);

        return File.Exists(path)
            ? File.ReadAllText(path)
            : $"Note '{safeFileName}' was not found.";
    }

    static string GetRequiredArgument(AgentToolCall call, string name)
    {
        if (call.Arguments.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        throw new InvalidOperationException($"Tool '{call.Name}' requires argument '{name}'.");
    }
}
