namespace PersonalAi.Configuration;

sealed class AppSettings
{
    public CorpusSettings Corpus { get; init; } = new();
    public EmbeddingSettings Embedding { get; init; } = new();
    public ChunkingSettings Chunking { get; init; } = new();
    public RetrievalSettings Retrieval { get; init; } = new();
    public JudgeSettings Judge { get; init; } = new();
    public AgentSettings Agent { get; init; } = new();
    public RunLogSettings RunLog { get; init; } = new();
}

sealed class CorpusSettings
{
    public string NotesDirectory { get; init; } = "samples/notes";
}

sealed class EmbeddingSettings
{
    public string ModelDirectory { get; init; } = "models/distiluse-base-multilingual-cased-v2";
    public int MaxTokens { get; init; } = 256;
}

sealed class ChunkingSettings
{
    public int MaxChars { get; init; } = 500;
}

sealed class RetrievalSettings
{
    public int TopK { get; init; } = 3;
}

sealed class JudgeSettings
{
    public string Model { get; init; } = "llama3";
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434/";
}

sealed class AgentSettings
{
    public string Model { get; init; } = "llama3";
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434/";
    public int MaxSteps { get; init; } = 5;
}

sealed class RunLogSettings
{
    public string Directory { get; init; } = "runs";
    public int PreviewChars { get; init; } = 80;
    public bool Enabled { get; init; } = false;
}
