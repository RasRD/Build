# Stage 1 — Mini-RAG — Session 3 (heading-based chunking) — 2026-08-01

## Hypothesis

Session 2 left query 3 (and its Russian counterpart, query 4) unresolved: real
semantic embeddings narrowed but didn't fix the "token" ambiguity, because
`rate-limiter.md`'s "Token Bucket" section was merged by `maxChars`-based
chunking into one large chunk together with the "Rate Limiting" intro and a
dangling "Fixed Window Counter" heading. Splitting chunks strictly at Markdown
heading boundaries should isolate "Token Bucket" as its own narrowly-scoped
chunk, which may change (hopefully improve) query 3/4's ranking so `redis.md`'s
"Expiry" chunk outranks it.

## Acceptance criterion

Rerun the same 4 queries (3 English + 1 Russian) against the same 3 notes with
the new chunker, same ONNX embedding pipeline from session 2. No regression:
query 1 and query 2 keep the expected note in top-3. Main check: whether
`redis.md` now wins (or at least gets meaningfully closer) for query 3 and the
Russian bonus query, recorded honestly either way.

## Chunking decision

`src/PersonalAi/Chunking/MarkdownChunker.cs` changed: a paragraph starting with
`#` now always flushes the current buffer and starts a new chunk (in addition to
the existing `maxChars` cap). One-line change to the `foreach` loop's flush
condition. No other files touched — same model, same `Program.cs`, same
samples.

Effect on `rate-limiter.md`: went from 3 chunks (session 2, headings merged) to
5 chunks (one per section: intro, Token Bucket, Fixed Window Counter, Sliding
Window Log, Leaky Bucket). Total chunk count across all notes: 16 (up from 8 in
session 1/2).

## Commands run

```
dotnet build src/PersonalAi/PersonalAi.csproj
dotnet run --project src/PersonalAi/PersonalAi.csproj --no-build
```

Build: успех, 0 предупреждений, 0 ошибок. Загружено 16 чанков из 3 заметок
(было 8 в сессиях 1–2).

## Actual results

| Query | Ожидаемая заметка | Top-3 (score) | Session 2 top-1 |
|---|---|---|---|
| "how do I stop clients from hitting my service too often" | rate-limiter.md | **rate-limiter.md 0.354**, rate-limiter.md 0.134, rate-limiter.md 0.131 | rate-limiter.md 0.257 |
| "why would recently accessed data be removed when memory runs out" | caching.md | redis.md 0.278, **caching.md 0.271**, **caching.md 0.270** | caching.md 0.266 |
| "how can I make a login token disappear automatically after a while" | redis.md | **redis.md 0.343**, rate-limiter.md 0.277, caching.md 0.175 | rate-limiter.md 0.265 (redis.md #2) |
| (RU) "как сделать так, чтобы токен сессии переставал действовать через некоторое время" | redis.md | **redis.md 0.216**, rate-limiter.md 0.118, rate-limiter.md 0.107 | rate-limiter.md 0.146 (redis.md #2) |

Acceptance criterion formally met: expected note still in top-3 for all four
queries.

## Observed retrieval mistakes / limitations

1. **Query 3 fixed.** `redis.md`'s isolated "## Expiry" chunk ("Keys can be
   given a TTL... useful for session tokens and temporary data.") now beats
   rate-limiter.md's isolated "## Token Bucket" chunk, 0.343 vs 0.277 — a clean
   win, and a much wider, more confident margin than session 2's 0.265 vs
   0.237 near-tie (which was itself the wrong ranking). Narrower chunking
   plausibly helped here because the "Expiry" chunk is no longer diluted by
   being merged with the unrelated "Pub/Sub" section, and "Token Bucket" is no
   longer padded with the generic "Rate Limiting" intro paragraph — both
   chunks became purer expressions of their actual topic.
2. **Query 4 (Russian) fixed the same way**, with an even larger relative
   margin (0.216 vs 0.118) — `rate-limiter.md`'s "Token Bucket" chunk isn't
   even in the top-3 anymore, replaced by its "Fixed Window Counter" and intro
   chunks, both clearly behind `redis.md`. Reinforces that the model treats the
   Russian query consistently with its English equivalent.
3. **New regression on query 2**: top-1 flipped from `caching.md` (session 2)
   to `redis.md`'s own intro chunk ("# Redis — Redis is an in-memory data
   structure store used as a database, cache, and message broker.", score
   0.278). `caching.md` is still #2 and #3, so the top-3 acceptance criterion
   still holds, but the *correct* note is no longer top-1. Cause: isolating the
   Redis intro into its own tiny 2-sentence chunk concentrated the phrase
   "in-memory... cache" into a very short, keyword-dense chunk that now matches
   "memory runs out" / "cache" very strongly, even though the intro sentence
   isn't actually about eviction policy — the thing the query is really asking
   about. This is the mirror image of what fixed query 3: narrower chunking
   sharpens a chunk's topical signal, which helps when that signal is genuinely
   on-topic (Token Bucket, Expiry) and hurts when a short chunk happens to
   contain query-relevant keywords out of context (the Redis intro line).
4. Query 1 also strengthened (0.354 vs 0.257), with a cleaner sweep (all three
   top-3 slots now `rate-limiter.md`, vs. one `caching.md` slipping into #3 in
   session 2).

## Conclusion

The hypothesis is **confirmed for its stated target** (query 3/4's "token"
ambiguity) but **not for free**: heading-based chunking fixed the exact
prediction — isolating "Token Bucket" resolved the near-miss into a clear win —
and even improved query 1's margin as a bonus. It also introduced a new,
different near-miss on query 2 caused by the same mechanism (sharper,
shorter chunks can overfit to a locally keyword-dense but topically-generic
sentence). Net effect across the 4-query set: 3 clear wins (1, 3, 4) and one
new soft regression (2, still within top-3). This matches the general
retrieval lesson that chunking strategy and embedding quality are coupled
knobs — improving one can shift, not eliminate, the failure modes exposed by
the other.

## Deferred (out of scope for this session)

Final answer generation, vector database, agent framework, MCP,
prompt-injection defenses, caching/routing, processing the full Obsidian vault,
production error handling, fixing query 2's new intro-chunk regression (would
likely need either a minimum chunk length / merging very short leading
sections back into their first body paragraph, or a smarter chunker than
pure heading-splitting — a good candidate for a future, smaller-scoped
session rather than folding into this one), GPU inference, quantization
comparison.
