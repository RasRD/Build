namespace PersonalAi.Retrieval;

using PersonalAi.Models;

static class SimilaritySearch
{
    public static IEnumerable<(NoteChunk Chunk, float Score)> TopK(
        IReadOnlyList<NoteChunk> chunks, float[] queryEmbedding, int k = 3)
    {
        return chunks
            .Select(c => (Chunk: c, Score: CosineSimilarity(c.Embedding, queryEmbedding)))
            .OrderByDescending(x => x.Score)
            .Take(k);
    }

    static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
