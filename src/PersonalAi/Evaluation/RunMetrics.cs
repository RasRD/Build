using System.Text.Json;

namespace PersonalAi.Evaluation;

record RunMetrics(double RecallAtK, double MeanReciprocalRank, double JudgeAgreementRate)
{
    public static RunMetrics Compute(RunLog log)
    {
        var queries = log.Queries;
        if (queries.Count == 0)
            return new RunMetrics(0, 0, 0);

        var recallAtK = queries.Count(q => q.ExpectedInTopK) / (double)queries.Count;

        var mrr = queries
            .Select(q =>
            {
                var rank = FindRank(q.Chunks, q.ExpectedNoteFile);
                return rank >= 0 ? 1.0 / (rank + 1) : 0.0;
            })
            .Average();

        var allChunks = queries.SelectMany(q => q.Chunks.Select(chunk => (Query: q, Chunk: chunk))).ToList();
        var judgeAgreementRate = allChunks.Count == 0
            ? 0
            : allChunks.Count(x => (x.Chunk.SourceFile == x.Query.ExpectedNoteFile) == x.Chunk.JudgeRelevant)
              / (double)allChunks.Count;

        return new RunMetrics(recallAtK, mrr, judgeAgreementRate);
    }

    public static RunLog Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RunLog>(json)
            ?? throw new InvalidDataException($"Could not parse run log '{path}'.");
    }

    static int FindRank(IReadOnlyList<ChunkVerdict> chunks, string expectedNoteFile)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            if (chunks[i].SourceFile == expectedNoteFile)
                return i;
        }
        return -1;
    }
}
