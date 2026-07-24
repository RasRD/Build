namespace PersonalAi.Embeddings;

interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
}
