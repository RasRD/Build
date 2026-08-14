namespace PersonalAi.Evaluation;

record RunParameters(
    string NotesDirectory,
    string ModelDirectory,
    int ChunkMaxChars,
    int EmbeddingMaxTokens,
    int TopK,
    string JudgeModel,
    string OllamaBaseUrl,
    int PreviewChars);

record ChunkVerdict(
    string SourceFile,
    float Score,
    string ChunkPreview,
    bool JudgeRelevant,
    string JudgeReasoning);

record QueryRunResult(
    string Query,
    string ExpectedNoteFile,
    bool ExpectedInTopK,
    IReadOnlyList<ChunkVerdict> Chunks,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);

record RunLog(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ChunkCount,
    RunParameters Parameters,
    IReadOnlyList<QueryRunResult> Queries);
