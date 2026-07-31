# Stage 1 — Mini-RAG — Session 2 (ONNX semantic embeddings) — 2026-08-01

## Hypothesis

A real pretrained multilingual sentence-embedding model (DistilBERT → mean pooling →
learned Dense+Tanh projection, run locally via ONNX Runtime) retrieves the correct
note by meaning even when the query's wording doesn't lexically overlap with the
note — and specifically fixes session 1's query-3 failure (the "token" lexical
trap), where the hashing baseline was fooled by word overlap instead of meaning.

## Acceptance criterion

Same 3 queries as session 1 against the same 3 sample notes, plus one added
Russian-language query as a cross-lingual bonus check. Accepted if the expected
note appears in top-3 for every query, the full path is explainable, actual
outputs are shown, results are compared against session 1's hashing scores, and
retrieval mistakes are recorded.

## Embedding provider decision

`distiluse-base-multilingual-cased-v2` (sentence-transformers), run in-process via
ONNX Runtime in C#, chosen over a Python sidecar or an external HTTP embedding
server to stay in the primary .NET stack and keep tokenization/pooling visible in
code.

A research pass (Hugging Face) found that pre-converted community ONNX exports of
this model (`Xenova/distiluse-base-multilingual-cased-v2`) only include the base
DistilBERT transformer — they omit the model's trained `Dense(768→512)+Tanh`
projection (a separate `2_Dense/model.safetensors` module in the original
sentence-transformers repo), so the raw ONNX output is a 768-dim mean-pooled
transformer embedding, not the official 512-dim `distiluse` sentence-embedding
space. Confirmed at runtime: the loaded ONNX graph exposes only `input_ids` and
`attention_mask` inputs (DistilBERT has no `token_type_ids`) and a single
`last_hidden_state` output of shape `[-1,-1,768]`.

Decision: reproduce the official embedding space instead of accepting the
approximation — download `2_Dense/model.safetensors` + `2_Dense/config.json`
separately from the original `sentence-transformers/distiluse-base-multilingual-cased-v2`
repo and apply the linear projection + Tanh manually in C#, after hand-rolled
attention-mask-weighted mean pooling over the ONNX output.

## Implementation

- `src/PersonalAi/Embeddings/OnnxEmbeddingService.cs` (new) — implements
  `IEmbeddingService`. Loads the ONNX session, a `FastBertTokenizer.BertTokenizer`
  (cased vocab, `convertInputToLowercase: false`), and the Dense-layer weights
  (hand-parsed from the safetensors binary format: 8-byte header length + JSON
  tensor index + raw `F32` buffer — confirmed via the file's own header before
  writing the parser). `EmbedAsync`: tokenize → ONNX inference → mean pool
  (attention-mask-weighted) → `tanh(W·x + b)` → L2-normalize → `float[512]`.
- `src/PersonalAi/Program.cs` (changed) — added `FindModelDirectory()` alongside
  the existing `FindSamplesDirectory()` (same repo-root-walk pattern, refactored
  into a shared `FindRepositoryRoot()`); swapped `HashingEmbeddingService` for
  `OnnxEmbeddingService` (via `using var`, since it now owns a native ONNX
  session); appended a 4th, Russian-language query to the fixed query list.
- `src/PersonalAi/PersonalAi.csproj` (changed) — added `Microsoft.ML.OnnxRuntime`
  1.28.0 (CPU inference session) and `FastBertTokenizer` 1.0.28 (WordPiece
  tokenizer that reads `vocab.txt` directly; avoids hand-writing WordPiece
  tokenization for a multilingual cased vocab).
- `.gitignore` (changed) — added `models/` so the downloaded binaries are never
  committed.
- `nuget.config` (new, repo root) — **environment fix, not part of the original
  plan's file list.** This machine's global `~/.nuget/NuGet/NuGet.Config` points
  every package source at an internal corporate artifactory
  (`artifactory.s.o3.ru`) that isn't reachable from this environment, which
  blocked `dotnet add package` entirely. Rather than edit the global config
  (used by the user's other, unrelated work repositories), added a project-local
  `nuget.config` that clears and repoints sources to `nuget.org` for this
  repository only.
- Downloaded into a new, gitignored `models/distiluse-base-multilingual-cased-v2/`
  (not committed):
  - `model.onnx` (135 MB, int8-quantized) + `vocab.txt`, from
    `huggingface.co/Xenova/distiluse-base-multilingual-cased-v2`.
  - `2_Dense/model.safetensors` (1.5 MB) + `2_Dense/config.json`, from
    `huggingface.co/sentence-transformers/distiluse-base-multilingual-cased-v2`.
- Not changed: `MarkdownNoteReader.cs`, `MarkdownChunker.cs`, `NoteChunk.cs`,
  `SimilaritySearch.cs` (already dimension-agnostic, works unchanged with
  512-dim vectors), `IEmbeddingService.cs`, `HashingEmbeddingService.cs` (left in
  place, unused).

## Commands run

```
dotnet add src/PersonalAi/PersonalAi.csproj package Microsoft.ML.OnnxRuntime --version 1.28.0
dotnet add src/PersonalAi/PersonalAi.csproj package FastBertTokenizer --version 1.0.28
curl (4x, model.onnx / vocab.txt / 2_Dense/model.safetensors / 2_Dense/config.json)
dotnet build src/PersonalAi/PersonalAi.csproj
dotnet run --project src/PersonalAi/PersonalAi.csproj --no-build
```

Build: успех, 0 предупреждений, 0 ошибок. Загружено 8 чанков из 3 заметок.
Confirmed at runtime (printed at startup): `ONNX inputs: input_ids[-1,-1],
attention_mask[-1,-1]`, `ONNX output used: last_hidden_state[-1,-1,768]`,
`Dense projection: 768 -> 512 (+ Tanh)`.

## Actual results

| Query | Ожидаемая заметка | Top-3 (score) | Session 1 (hashing) top-1 |
|---|---|---|---|
| "how do I stop clients from hitting my service too often" | rate-limiter.md | **rate-limiter.md 0.257**, rate-limiter.md 0.143, caching.md 0.089 | caching.md 0.158 (rate-limiter.md last, 0.133) |
| "why would recently accessed data be removed when memory runs out" | caching.md | **caching.md 0.266**, **caching.md 0.263**, **caching.md 0.206** | caching.md 0.172 (mixed with redis.md at #3) |
| "how can I make a login token disappear automatically after a while" | redis.md | rate-limiter.md 0.265, **redis.md 0.237**, rate-limiter.md 0.231 | rate-limiter.md 0.478 (**redis.md 0.155**, last) |
| (bonus, RU) "как сделать так, чтобы токен сессии переставал действовать через некоторое время" | redis.md | rate-limiter.md 0.146, **redis.md 0.139**, caching.md 0.118 | — (no RU baseline) |

Acceptance criterion formally met: the expected note appears in the top-3 for all
four queries.

## Observed retrieval mistakes / limitations

1. **Query 3's "token" trap is not fixed, only weakened.** In session 1 the wrong
   top result (`rate-limiter.md`, via lexical collision with "Token Bucket") won
   by a huge margin (0.478 vs 0.155 for `redis.md`). With real semantic
   embeddings, `rate-limiter.md` still wins, but the margin has collapsed to
   0.265 vs 0.237 — `redis.md` is a close second instead of a distant third.
   Reading the source text explains why this is a *harder* case than pure word
   overlap: rate-limiter.md's "Token Bucket" section describes tokens being
   granted, consumed, and running out — a resource with a lifecycle — which is
   conceptually adjacent to "a session token that expires," not just a shared
   word. A semantic model correctly notices that similarity; it isn't fooled by
   spelling, it's genuinely torn between two related senses of "token."
2. **Query 4 (Russian) reproduces the exact same pattern as query 3** (its
   English near-equivalent): `rate-limiter.md` first, `redis.md` a close second,
   `caching.md` a clear third. This is a good sign for the multilingual claim —
   the model treats the Russian query consistently with its English counterpart
   rather than randomly — but it inherits the same unresolved ambiguity.
   Tokenization sanity check: the Russian query's ranking is coherent and
   `redis.md`/`rate-limiter.md` clearly separate from the unrelated
   `caching.md`, which would not happen if Cyrillic input were being reduced to
   mostly `[UNK]` tokens — so no separate tokenizer dump was needed to trust
   this result.
3. **Query 2 became a clean sweep** (all three top-3 slots are `caching.md`
   chunks, vs. a mix with `redis.md` at #3 in session 1) — the clearest
   improvement of the four queries.
4. Absolute score magnitudes are not comparable between session 1 (hashing,
   256-dim, TF-based) and session 2 (real embeddings, 512-dim) — only the
   *ranking* and *relative margins* are meaningfully comparable.

## Conclusion

The hypothesis is **partially confirmed, more convincingly than session 1, but
still not fully**. Two of four queries (1 and 2) show a clear, unambiguous
improvement over the hashing baseline — the expected note now wins by a wide,
confident margin instead of a weak or reversed one. Query 3 (and its Russian
counterpart, query 4) shows the semantic model narrowing session 1's outright
failure into a close, defensible near-miss rather than resolving it: real
semantics reduced the "token" confusion's severity but didn't eliminate it,
because the confusion here is not purely lexical — it's a genuine, harder
semantic ambiguity between two related uses of "token." The cross-lingual bonus
query is a genuinely new, positive result this session: the model's Russian
query produces the same qualitative ranking as its English equivalent, which is
exactly the behavior a multilingual model is supposed to exhibit and which the
session 1 hashing baseline could not have shown at all (it has no notion of
cross-lingual similarity).

## Deferred (out of scope for this session)

Final answer generation, vector database, agent framework, MCP, prompt-injection
defenses, caching/routing, processing the full Obsidian vault, sophisticated
Markdown parsing, production error handling, GPU inference, comparing quantized
vs. full-precision ONNX weights, explicit `[UNK]`-rate instrumentation for the
Russian tokenizer path, resolving the query-3 token ambiguity itself (would
likely need better chunking — e.g. splitting "Token Bucket" as its own more
narrowly-scoped chunk — rather than a different embedding model).
