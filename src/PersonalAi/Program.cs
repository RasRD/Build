using PersonalAi.Chunking;
using PersonalAi.Embeddings;
using PersonalAi.Models;
using PersonalAi.Notes;
using PersonalAi.Retrieval;

Console.WriteLine("PersonalAi – Stage 1: Mini-RAG");
Console.WriteLine();

var notesDirectory = FindSamplesDirectory();
var modelDirectory = FindModelDirectory();

Console.WriteLine("Loading distiluse-base-multilingual-cased-v2 (ONNX)...");
using var embeddingService = new OnnxEmbeddingService(modelDirectory);
Console.WriteLine();

var chunks = new List<NoteChunk>();
foreach (var (fileName, text) in MarkdownNoteReader.ReadAll(notesDirectory))
{
    foreach (var chunkText in MarkdownChunker.Chunk(text))
    {
        var embedding = await embeddingService.EmbedAsync(chunkText);
        chunks.Add(new NoteChunk(fileName, chunkText, embedding));
    }
}

Console.WriteLine($"Loaded {chunks.Count} chunks from '{notesDirectory}'.");
Console.WriteLine();

var queries = new[]
{
    "how do I stop clients from hitting my service too often",
    "why would recently accessed data be removed when memory runs out",
    "how can I make a login token disappear automatically after a while",
    // Cross-lingual bonus check: same "session token expiry" meaning as query 3, in Russian.
    "как сделать так, чтобы токен сессии переставал действовать через некоторое время",
};

foreach (var query in queries)
{
    var queryEmbedding = await embeddingService.EmbedAsync(query);
    var results = SimilaritySearch.TopK(chunks, queryEmbedding, 3);

    Console.WriteLine($"Query: \"{query}\"");
    foreach (var (chunk, score) in results)
    {
        var preview = chunk.Text.Length > 80 ? chunk.Text[..80] + "..." : chunk.Text;
        Console.WriteLine($"  [{score:F3}] {chunk.SourceFile} :: {preview.ReplaceLineEndings(" ")}");
    }
    Console.WriteLine();
}

static string FindSamplesDirectory() => Path.Combine(FindRepositoryRoot(), "samples", "notes");

static string FindModelDirectory() =>
    Path.Combine(FindRepositoryRoot(), "models", "distiluse-base-multilingual-cased-v2");

static string FindRepositoryRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PersonalAi.sln")))
        dir = dir.Parent;
    if (dir is null)
        throw new DirectoryNotFoundException("Could not locate repository root (PersonalAi.sln not found).");
    return dir.FullName;
}
