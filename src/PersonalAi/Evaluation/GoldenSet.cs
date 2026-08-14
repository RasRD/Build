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
        new("how do I spread incoming requests across several servers so one doesn't get overwhelmed", "load-balancing.md"),
        new("how can one part of my system hand off work to another part without waiting for it right now", "message-queues.md"),
        // Deliberately close to the redis.md / jwt-vs-session-tokens-distractor.md "session expiry" language —
        // a robustness check on whether retrieval/judge can tell "does the server need to remember session
        // state to expire a login" apart from the Redis TTL note it's phrased to resemble.
        new("does the server need to remember session state to expire a login, or can the token expire on its own", "jwt-vs-session-tokens.md"),
    ];
}
