using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PersonalAi.Agent;
using PersonalAi.Chunking;
using PersonalAi.Configuration;
using PersonalAi.Embeddings;
using PersonalAi.Evaluation;
using PersonalAi.Models;
using PersonalAi.Notes;
using PersonalAi.Retrieval;

if (args.Length > 0 && args[0] == "metrics")
{
    RunMetricsMode(args.Skip(1).ToArray());
    return;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();
var settings = configuration.Get<AppSettings>() ?? new AppSettings();

var notesDirectory = ResolveRepoPath(settings.Corpus.NotesDirectory);
var modelDirectory = ResolveRepoPath(settings.Embedding.ModelDirectory);
var runLogDirectory = ResolveRepoPath(settings.RunLog.Directory);

Console.WriteLine("Loading distiluse-base-multilingual-cased-v2 (ONNX)...");
using var embeddingService = new OnnxEmbeddingService(modelDirectory, settings.Embedding.MaxTokens);
Console.WriteLine();

var chunks = new List<NoteChunk>();
foreach (var (fileName, text) in MarkdownNoteReader.ReadAll(notesDirectory))
{
    foreach (var chunkText in MarkdownChunker.Chunk(text, settings.Chunking.MaxChars))
    {
        var embedding = await embeddingService.EmbedAsync(chunkText);
        chunks.Add(new NoteChunk(fileName, chunkText, embedding));
    }
}

Console.WriteLine($"Loaded {chunks.Count} chunks from '{notesDirectory}'.");
Console.WriteLine();

if (args.Length > 0 && args[0] == "agent")
{
    Console.WriteLine($"=== Stage 3 agent (Ollama, model: {settings.Agent.Model}) ===");
    Console.WriteLine();

    var userMessage = args.Length > 1
        ? string.Join(' ', args.Skip(1))
        : ReadAgentPrompt();

    var tools = new AgentTools(notesDirectory, chunks, embeddingService, settings.Retrieval.TopK);
    var client = new OllamaAgentClient(settings.Agent.Model, settings.Agent.OllamaBaseUrl);
    var runner = new AgentRunner(client, tools, settings.Agent.MaxSteps);

    var answer = await runner.RunAsync(userMessage);
    Console.WriteLine();
    Console.WriteLine("Final answer:");
    Console.WriteLine(answer);
    return;
}

Console.WriteLine("PersonalAi – Stage 2: Golden set and LLM-as-judge evaluation");
Console.WriteLine();
Console.WriteLine($"=== LLM-as-judge (Ollama, model: {settings.Judge.Model}) ===");
Console.WriteLine();

var judge = new LlmJudge(settings.Judge.Model, settings.Judge.OllamaBaseUrl);
var runStartedAtUtc = DateTimeOffset.UtcNow;
var queryResults = new List<QueryRunResult>();

foreach (var golden in GoldenSet.Queries)
{
    var queryStartedAtUtc = DateTimeOffset.UtcNow;

    var queryEmbedding = await embeddingService.EmbedAsync(golden.Query);
    var results = SimilaritySearch.TopK(chunks, queryEmbedding, settings.Retrieval.TopK).ToList();
    var expectedInTopK = results.Any(r => r.Chunk.SourceFile == golden.ExpectedNoteFile);

    Console.WriteLine($"Query: \"{golden.Query}\"");
    Console.WriteLine($"  Expected note: {golden.ExpectedNoteFile} (in top-{settings.Retrieval.TopK}: {expectedInTopK})");

    var chunkVerdicts = new List<ChunkVerdict>();
    foreach (var (chunk, score) in results)
    {
        var verdict = await judge.JudgeAsync(golden.Query, chunk.Text);
        var preview = chunk.Text.Length > settings.RunLog.PreviewChars
            ? chunk.Text[..settings.RunLog.PreviewChars] + "..."
            : chunk.Text;
        var previewOneLine = preview.ReplaceLineEndings(" ");

        Console.WriteLine(
            $"  [{score:F3}] {chunk.SourceFile} :: judge relevant={verdict.Relevant} :: {verdict.Reasoning}");
        Console.WriteLine($"      chunk: {previewOneLine}");

        chunkVerdicts.Add(new ChunkVerdict(chunk.SourceFile, score, previewOneLine, verdict.Relevant, verdict.Reasoning));
    }
    Console.WriteLine();

    queryResults.Add(new QueryRunResult(
        golden.Query, golden.ExpectedNoteFile, expectedInTopK, chunkVerdicts,
        queryStartedAtUtc, DateTimeOffset.UtcNow));
}

if (settings.RunLog.Enabled)
{
    var runParameters = new RunParameters(
        settings.Corpus.NotesDirectory, settings.Embedding.ModelDirectory, settings.Chunking.MaxChars,
        settings.Embedding.MaxTokens, settings.Retrieval.TopK, settings.Judge.Model, settings.Judge.OllamaBaseUrl,
        settings.RunLog.PreviewChars);
    var runLog = new RunLog(runStartedAtUtc, DateTimeOffset.UtcNow, chunks.Count, runParameters, queryResults);

    Directory.CreateDirectory(runLogDirectory);
    var runLogPath = Path.Combine(runLogDirectory, $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json");
    await File.WriteAllTextAsync(runLogPath, JsonSerializer.Serialize(runLog, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Run log written to {runLogPath}");

    var runReportPath = Path.ChangeExtension(runLogPath, ".html");
    await File.WriteAllTextAsync(runReportPath, RunLogHtmlReport.Render(runLog));
    Console.WriteLine($"Run report written to {runReportPath}");
    Process.Start(new ProcessStartInfo(runReportPath) { UseShellExecute = true });
}
else
{
    Console.WriteLine("Run log skipped (RunLog:Enabled is false; use the 'Experiment' launch profile to enable it).");
}

static string ReadAgentPrompt()
{
    Console.Write("> ");
    var value = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException("Agent prompt cannot be empty.");
    return value;
}

static void RunMetricsMode(string[] paths)
{
    if (paths.Length == 0)
    {
        Console.WriteLine("Usage: dotnet run -- metrics <run1.json> [run2.json]");
        return;
    }

    var results = new List<(string Path, RunMetrics Metrics)>();
    foreach (var path in paths)
    {
        var log = RunMetrics.Load(path);
        var metrics = RunMetrics.Compute(log);
        results.Add((path, metrics));

        Console.WriteLine($"{Path.GetFileName(path)} (TopK={log.Parameters.TopK}):");
        Console.WriteLine($"  Recall@K:        {metrics.RecallAtK:P1}");
        Console.WriteLine($"  MRR:             {metrics.MeanReciprocalRank:F3}");
        Console.WriteLine($"  Judge agreement: {metrics.JudgeAgreementRate:P1}");
        Console.WriteLine();
    }

    if (results.Count == 2)
    {
        var (pathA, a) = results[0];
        var (pathB, b) = results[1];
        Console.WriteLine($"Diff ({Path.GetFileName(pathB)} - {Path.GetFileName(pathA)}):");
        Console.WriteLine($"  Recall@K:        {Signed(b.RecallAtK - a.RecallAtK, "P1")}");
        Console.WriteLine($"  MRR:             {Signed(b.MeanReciprocalRank - a.MeanReciprocalRank, "F3")}");
        Console.WriteLine($"  Judge agreement: {Signed(b.JudgeAgreementRate - a.JudgeAgreementRate, "P1")}");
    }
}

static string Signed(double value, string format)
{
    var formatted = value.ToString(format);
    return value >= 0 ? "+" + formatted : formatted;
}

string ResolveRepoPath(string relativePath) => Path.Combine(FindRepositoryRoot(), relativePath);

static string FindRepositoryRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PersonalAi.sln")))
        dir = dir.Parent;
    if (dir is null)
        throw new DirectoryNotFoundException("Could not locate repository root (PersonalAi.sln not found).");
    return dir.FullName;
}
