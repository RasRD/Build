# Stage 2 — Golden set & LLM-as-judge — Session 1 — 2026-08-06

## Hypothesis

A local LLM (via Ollama, running entirely offline with no API costs) can
automatically judge whether a retrieved chunk is relevant to a query, and its
verdicts agree with the manual "expected note" judgments already recorded in
the Stage 1 session logs.

## Acceptance criterion

For every golden-set query: show the judge's relevance verdict + reasoning
for all top-3 chunks, and compare against the expected-note judgments already
recorded in Stage 1. Accepted if every verdict is explainable (has a
reasoning string) and any disagreement with the Stage 1 manual judgment is
recorded, not hidden.

## Provider decision

Local LLM via **Ollama** (`llama3`, already pulled on this machine — no
download, no API key, no cost), called over its local HTTP API
(`http://localhost:11434/api/chat`, `format: "json"`) from plain
`HttpClient` + `System.Text.Json` — no new NuGet packages. Chosen explicitly
over Anthropic/OpenAI APIs after confirming they'd require separate paid
credentials outside the user's existing chat subscriptions (discussed before
implementation). Chosen over hand-rolling generation via
`Microsoft.ML.OnnxRuntimeGenAI` because Ollama already solves tokenization,
sampling, and KV-cache management — keeping this session's diff to a plain
HTTP call, consistent with Stage 1's "simple, explicit code" direction.

Verified before writing code: `ollama list` showed `llama3`, `mistral`, and
`phi3` already present locally; a manual `curl` probe of `/api/chat` with
`format: "json"` confirmed the response shape (`message.content` is itself a
JSON string that must be parsed a second time).

## Implementation

- `src/PersonalAi/Evaluation/GoldenSet.cs` (new) — formalizes the 4 queries
  used across Stage 1 sessions 1–4 (3 English + 1 Russian) as
  `(Query, ExpectedNoteFile)` records, instead of a bare string array with
  the "expected" answer only living in prose in the session logs.
- `src/PersonalAi/Evaluation/LlmJudge.cs` (new) — `LlmJudge.JudgeAsync(query,
  chunkText)`: builds a judge prompt, POSTs to Ollama's `/api/chat` with
  `format: "json"`, parses `message.content` into `{Relevant, Reasoning}`.
- `src/PersonalAi/Program.cs` (changed) — added a second section after the
  existing Stage 1 retrieval printout: runs the golden set through the same
  (unchanged) retrieval pipeline, then calls the judge on each of the top-3
  chunks per query and prints score + judge verdict + reasoning.
- `CLAUDE.md` (changed) — `Current Stage` moved from Stage 1 to Stage 2, with
  a revised hypothesis/result/acceptance-criterion/non-goals block for this
  session.
- Not changed: `MarkdownChunker.cs`, `OnnxEmbeddingService.cs`,
  `SimilaritySearch.cs`, `MarkdownNoteReader.cs`, sample notes, model files —
  the retrieval pipeline itself is reused exactly as Stage 1 left it.

Known, accepted redundancy: the golden-set loop re-embeds and re-retrieves
the same 4 queries already printed by the original Stage 1 loop just above
it, instead of reusing those results. Left as-is to keep the diff minimal and
avoid touching working Stage 1 code in a session about adding a judge, not
refactoring retrieval.

## Commands run

```
dotnet build src/PersonalAi/PersonalAi.csproj
dotnet run --project src/PersonalAi/PersonalAi.csproj --no-build
```

First build failed: `error CS9006` — a `$"""..."""` raw string interpolation
containing literal `{"relevant": ...}` JSON collided with the interpolation
braces. Fixed by switching to `$$"""..."""` (double-`$`) so interpolated
values use `{{query}}` and the literal JSON braces stay single. Second build:
success, 0 warnings, 0 errors. Run: success, 16 chunks loaded, all 4
golden-set queries judged without exceptions (Ollama's first call included
an ~11s one-time model load; subsequent calls were sub-second).

## Actual results

| Query | Expected note | Judge verdicts (score → relevant, reasoning gist) |
|---|---|---|
| "...clients hitting my service too often" | rate-limiter.md | rate-limiter.md ×3, **all relevant=True** |
| "...recently accessed data...memory runs out" | caching.md | redis.md (0.278) **relevant=False**; caching.md ×2 relevant=True |
| "...login token disappear..." | redis.md | redis.md (Expiry) relevant=True; rate-limiter.md (Token Bucket) relevant=True; caching.md relevant=False |
| (RU) "...токен сессии переставал действовать..." | redis.md | redis.md (Expiry) relevant=True; rate-limiter.md (Fixed Window Counter) relevant=True; rate-limiter.md (intro) relevant=False |

Full verdict text and reasoning is in the console output from the run above.

## Observed agreement / disagreement with Stage 1's manual judgments

1. **Judge independently caught the exact regression Stage 1 Session 3 found.**
   For query 2, the embedding pipeline ranks `redis.md`'s intro chunk
   ("in-memory... cache...") as top-1 — the known soft regression documented
   in `sessions/2026-08-01-stage1-mini-rag-session3.md`. The judge marked
   this exact chunk `relevant=False` ("does not directly address... memory
   runs out") while marking both `caching.md` chunks `relevant=True`. This is
   a genuine, useful disagreement between the retrieval ranking and the
   judge — and it matches what a human (us, in session 3) already
   concluded by reading the source text. This is the strongest positive
   result of the session: the judge adds real signal beyond "is the expected
   file present in top-3," which the raw ranking metric can't express.
2. **Judge did *not* catch the "token" ambiguity from sessions 3/4.** For
   query 3 and its Russian counterpart, `rate-limiter.md`'s Token Bucket /
   Fixed Window Counter chunks are marked `relevant=True` alongside the
   correct `redis.md` Expiry chunk — the judge is pulled by the same
   "token"/session-lifecycle word association the embedding model is,
   rather than flagging it as a near-miss. The Russian-query justification
   for the Fixed Window Counter chunk ("limit the impact of token session
   effects by resetting counters") is a stretch that doesn't hold up to
   scrutiny — the chunk is about rate-limiting, not token expiry. This is a
   judge weakness worth recording plainly: a single relevance judgment per
   chunk, with no comparison across candidates, doesn't reliably catch
   "plausible-sounding but wrong" verdicts.
3. All four golden-set queries still have their expected note in top-3
   (unchanged from Stage 1 Session 4) — the acceptance criterion here is
   about the judge's own explainability and disagreement-recording, not
   about re-litigating retrieval quality.

## Conclusion

The hypothesis is **confirmed for its narrow claim**: a local, free LLM
judge, called with a few lines of plain HTTP code, produces relevance
verdicts with real, inspectable reasoning, and in one case (query 2)
independently reproduced a retrieval mistake a human had to read the source
notes to diagnose in Stage 1. It is **not** a reliable substitute for human
judgment yet: it missed the token-ambiguity near-miss in query 3/4, and one
of its own justifications was weak. As an automated, always-available signal
layered on top of (not replacing) manual inspection, it already pulled its
weight this session.

## Deferred (out of scope for this session)

Aggregating verdicts into a precision/recall-style score over the golden
set, testing whether a different local model (`mistral`, `phi3`) agrees or
disagrees with `llama3`'s verdicts, giving the judge more than one candidate
at a time (pairwise/listwise judging instead of pointwise, which might catch
the query-3 near-miss the current pointwise setup missed), a paid cloud
judge for comparison, an agent framework, MCP, prompt-injection defenses,
caching/model routing, production error handling.
