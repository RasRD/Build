# Stage 2 — Golden set & LLM-as-judge — Session 2 — 2026-08-14

## Hypothesis

Centralizing parameters into one settings file and persisting one structured
JSON record per run (parameters + per-query retrieval/judge outcomes) makes
retrieval experiments reproducible and comparable — changing one setting and
re-running should produce a distinct, inspectable run log, without touching
code.

## Acceptance criterion

Two runs with different `appsettings.json` `TopK` values produce two distinct
`runs/*.json` files, each correctly recording its own parameter set, with at
least one query's retrieved-chunk list differing between them — and all
golden-set queries execute end-to-end against the expanded note corpus
without exceptions.

## Scope decisions

Per the project's own scope discipline, this session was split from a larger
user request into two parts:

- **This session**: config externalization, a larger/harder note corpus, and
  run-log persistence (raw data capture only).
- **Deferred to the next session**: aggregating run logs into metrics
  (Recall@K, MRR, judge-vs-expected agreement rate) and experimentally
  comparing two retrieval configurations, after which the project moves to
  Stage 3.

The synthetic-note batch was further scoped down mid-planning from an
originally proposed ~30 files (10 topics) to **9 files (3 topics)**, to keep
the session inside a reviewable size — the user explicitly capped it at "~10
files to start."

## Implementation

- `src/PersonalAi/Configuration/AppSettings.cs` (new) — settings POCO:
  `NotesDirectory`, `ModelDirectory`, `RunLogDirectory`, `TopK`,
  `PreviewChars`, `ChunkMaxChars`, `EmbeddingMaxTokens`, `JudgeModel`,
  `OllamaBaseUrl`. Defaults reproduce Session 1's hardcoded values exactly.
- `src/PersonalAi/appsettings.json` (new) — committed default values;
  confirmed the existing `.gitignore` `appsettings.*.json` pattern does not
  match the bare filename.
- `src/PersonalAi/PersonalAi.csproj` (changed) — added
  `Microsoft.Extensions.Configuration`, `.Json`, `.Binder` (binder-only, no
  DI container / generic host — `ConfigurationBuilder` + `Get<AppSettings>()`
  called directly in `Program.cs`, per the project's manual-construction
  rule). Added a `CopyToOutputDirectory` item for `appsettings.json`.
- `src/PersonalAi/Embeddings/OnnxEmbeddingService.cs` (changed) — `MaxTokens`
  const → `maxTokens` constructor parameter (default unchanged, `256`).
- `src/PersonalAi/Evaluation/LlmJudge.cs` (changed) — the previously `static`
  `HttpClient` with a hardcoded `BaseAddress` is now an instance field built
  from a new `baseUrl` constructor parameter, so `OllamaBaseUrl` is
  settings-driven.
- `src/PersonalAi/Evaluation/RunLog.cs` (new) — `RunParameters`,
  `ChunkVerdict`, `QueryRunResult`, `RunLog` records. Built in memory while
  the golden-set loop runs, then serialized once per run to
  `runs/{yyyyMMdd-HHmmss}.json`. Records raw per-query timestamps and chunk
  *previews* (not full text or precomputed latency), so a later session can
  derive Recall@K / MRR / agreement rate / latency from the JSON alone
  without re-running anything.
- `src/PersonalAi/Program.cs` (rewritten) — wires `IConfiguration`, resolves
  all three directories through one `ResolveRepoPath` helper, removes the
  Session-1-era duplicate hardcoded `queries` array + print-only loop (fully
  redundant with the judge loop once top-K/preview/chunking are all
  settings-driven), parameterizes every remaining call site, and
  builds+writes the run log at the end.
- `src/PersonalAi/Evaluation/GoldenSet.cs` (changed) — grew from 4 to 7
  entries: one new query per new topic (`load-balancing.md`,
  `message-queues.md`, `jwt-vs-session-tokens.md`), each targeting the
  canonical file.
- `samples/notes/` (+9 files) — three new topics, each with a canonical
  note, a paraphrased near-duplicate (`-alt.md`, same facts reworded, tests
  whether the embedding model generalizes past exact phrasing), and a
  distractor (`-distractor.md`) that shares vocabulary with one of the
  *existing* Session-1 notes while actually answering a different question:
  `load-balancing-distractor.md` ↔ `rate-limiter.md` (traffic/threshold
  vocabulary), `message-queues-distractor.md` ↔ `caching.md`
  ("reduce load on downstream" vocabulary), `jwt-vs-session-tokens-distractor.md`
  ↔ `redis.md` (TTL/expiry/session-token vocabulary — the strongest overlap,
  see results below).
- `.gitignore` (changed) — added `runs/` (generated, unbounded, same
  treatment as the existing `models/` entry).
- Not changed: `MarkdownChunker.cs`, `SimilaritySearch.cs`,
  `MarkdownNoteReader.cs`, `NoteChunk.cs`, `HashingEmbeddingService.cs`
  (already-unused dead code) — all already sufficiently parameterized or out
  of scope.

## Commands run

```
dotnet add src/PersonalAi/PersonalAi.csproj package Microsoft.Extensions.Configuration
dotnet add src/PersonalAi/PersonalAi.csproj package Microsoft.Extensions.Configuration.Json
dotnet add src/PersonalAi/PersonalAi.csproj package Microsoft.Extensions.Configuration.Binder
dotnet build src/PersonalAi/PersonalAi.csproj
dotnet run --project src/PersonalAi/PersonalAi.csproj --no-build   # TopK: 3 (default)
# edited appsettings.json: TopK 3 -> 5
dotnet run --project src/PersonalAi/PersonalAi.csproj              # TopK: 5
# reverted appsettings.json: TopK 5 -> 3
git status
```

NuGet resolved the config packages at version `10.0.11` (matching the
installed 10.0.101 SDK) even though the project targets `net9.0` — these
packages multi-target down to `netstandard2.0`, so this built and ran without
issue; no explicit version pin was needed beyond what `dotnet add` chose.
Build: 0 warnings, 0 errors both times. Both runs completed end-to-end
against Ollama (`llama3`, already running locally) with no exceptions; 58
chunks loaded from the 12-note corpus (3 original + 9 new).

## Actual results

TopK=3 run (`runs/20260814-203550.json`):

| Query | Expected note | In top-3? |
|---|---|---|
| "...clients hitting my service too often" | rate-limiter.md | True |
| "...recently accessed data...memory runs out" | caching.md | True |
| "...login token disappear..." | redis.md | **False** |
| (RU) "...токен сессии..." | redis.md | **False** |
| "...spread incoming requests across several servers..." | load-balancing.md | True |
| "...hand off work to another part without waiting..." | message-queues.md | True |
| "...remember session state to expire a login..." | jwt-vs-session-tokens.md | True |

TopK=5 run (`runs/20260814-203656.json`, same corpus/settings except `TopK`):
both previously-missed `redis.md` queries flip to **True** — `redis.md`'s
Expiry chunk was sitting just outside the top-3 cutoff and re-enters at
rank 4-5. This is exactly the kind of config-driven, reproducible comparison
this session set out to enable: same code, one changed setting, a different
(and inspectable) outcome captured in its own file.

## Observation: the `jwt-vs-session-tokens-distractor.md` hard negative was too effective

For both token-expiry queries (targeting `redis.md`), the distractor note
didn't just compete with the expected note — it **displaced it from top-3
entirely**, and the LLM judge marked the distractor `relevant=True` with
confident reasoning ("directly addresses the idea of making a login token
disappear automatically"). On inspection this is arguably *correct* judge
behavior, not a failure: `jwt-vs-session-tokens-distractor.md` is genuinely
about token TTL/eviction and does answer "how do I make a token expire," even
though it wasn't the golden set's designated answer. This surfaces a real
methodological question for the next session's metrics design: a binary
"was the *one* expected file in top-K" criterion undercounts when more than
one note in the corpus legitimately answers the query — worth deciding
whether Recall@K should tolerate multiple valid targets before that
aggregation logic is written.

The other two distractors (`load-balancing-distractor.md`,
`message-queues-distractor.md`) behaved as intended: they surfaced as
plausible/relevant *for their own topic's query* rather than displacing the
paired existing note, since their vocabulary overlap with `rate-limiter.md`/
`caching.md` was more surface-level than the redis/token pairing.

## Conclusion

The hypothesis is **confirmed**: settings now live in one file, and the
TopK 3→5 experiment produced two distinct, correctly-parameterized run logs
with genuinely different retrieved-chunk outcomes for two of the seven
queries — without any code change. The larger, harder corpus (paraphrases +
distractors) immediately did its job: it produced a real retrieval miss
(query 3/4 at TopK=3) and a genuinely ambiguous distractor case, rather than
just padding the chunk count.

## Deferred (out of scope for this session)

Aggregating the recorded runs into Recall@K / MRR / judge-vs-expected
agreement metrics, deciding how to handle multiple-valid-answer queries in
that aggregation (see observation above), experimentally comparing two
retrieval configurations side by side, the remaining ~6-7 topics from the
originally proposed larger synthetic corpus, a paid/cloud LLM judge, an agent
framework, MCP, prompt-injection defenses, caching/model routing, production
error handling.
