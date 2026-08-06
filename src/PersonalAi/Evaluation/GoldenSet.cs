namespace PersonalAi.Evaluation;

record GoldenQuery(string Query, string ExpectedNoteFile);

static class GoldenSet
{
    public static readonly IReadOnlyList<GoldenQuery> Queries =
    [
        new("how do I stop clients from hitting my service too often", "rate-limiter.md"),
        new("why would recently accessed data be removed when memory runs out", "caching.md"),
        new("how can I make a login token disappear automatically after a while", "redis.md"),
        // Cross-lingual bonus check: same "session token expiry" meaning as the query above, in Russian.
        new("как сделать так, чтобы токен сессии переставал действовать через некоторое время", "redis.md"),
    ];
}
