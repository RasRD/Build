using System.Text.RegularExpressions;

namespace PersonalAi.Embeddings;

sealed class HashingEmbeddingService : IEmbeddingService
{
    const int Dimensions = 256;
    static readonly Regex TokenPattern = new(@"[a-z0-9]+", RegexOptions.Compiled);

    public Task<float[]> EmbedAsync(string text)
    {
        var vector = new float[Dimensions];

        foreach (Match match in TokenPattern.Matches(text.ToLowerInvariant()))
        {
            var index = (int)(Fnv1aHash(match.Value) % Dimensions);
            vector[index] += 1f;
        }

        Normalize(vector);
        return Task.FromResult(vector);
    }

    static uint Fnv1aHash(string token)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in token)
        {
            hash ^= c;
            hash *= prime;
        }
        return hash;
    }

    static void Normalize(float[] vector)
    {
        var normSquared = 0f;
        foreach (var value in vector)
            normSquared += value * value;

        if (normSquared == 0f)
            return;

        var norm = MathF.Sqrt(normSquared);
        for (var i = 0; i < vector.Length; i++)
            vector[i] /= norm;
    }
}
