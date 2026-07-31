# Stage 1 — Mini-RAG — Session 2026-07-31

## Hypothesis

Embedding-based retrieval может найти релевантный фрагмент Markdown, даже если
формулировка запроса не совпадает по словам с текстом заметки.

## Acceptance criterion

Прогнать 3 заранее заданных запроса против 3 сэмпл-заметок (`caching.md`,
`rate-limiter.md`, `redis.md`). Эксперимент принят, если ожидаемая заметка
попадает в top-3 для каждого запроса, путь от файла до результата объясним,
показаны фактические выводы и зафиксированы наблюдаемые ошибки ретрива.

## Embedding provider decision

Выбран локальный детерминированный provider (без сети и ключей): bag-of-words /
hashing-trick — токенизация (lowercase, `[a-z0-9]+`), FNV-1a хэш токена в индекс
256-мерного вектора, TF-накопление, L2-нормализация. Cosine similarity поверх
этого вектора — по сути лексическое совпадение, а не семантика. Причина выбора:
минимальная сложность (без пакетов, без ключей, без сети), соответствует
`CLAUDE.md` (simple, explicit code; console app). Явно принятый компромисс:
такой provider плохо подходит для проверки исходной гипотезы про "разные слова —
похожий смысл", т.к. по построению реагирует на пересечение токенов, а не на
смысл.

## Implementation

- `src/PersonalAi/Embeddings/HashingEmbeddingService.cs` (новый) — реализация
  `IEmbeddingService` описанным выше способом.
- `src/PersonalAi/Program.cs` (изменён) — пайплайн: `MarkdownNoteReader.ReadAll`
  → `MarkdownChunker.Chunk` → `HashingEmbeddingService.EmbedAsync` → `NoteChunk`
  → для 3 запросов `SimilaritySearch.TopK` → печать `[score] file :: preview`.
- Не изменялись: `MarkdownNoteReader.cs`, `MarkdownChunker.cs`, `NoteChunk.cs`,
  `SimilaritySearch.cs`, `IEmbeddingService.cs`, `samples/notes/*`.

## Commands run

```
dotnet build src/PersonalAi/PersonalAi.csproj
dotnet run --project src/PersonalAi/PersonalAi.csproj --no-build
```

Build: успех, 0 предупреждений, 0 ошибок. Загружено 8 чанков из 3 заметок.

## Actual results

| Query | Ожидаемая заметка | Top-3 (score) |
|---|---|---|
| "how do I stop clients from hitting my service too often" | rate-limiter.md | caching.md 0.158, redis.md 0.142, **rate-limiter.md 0.133** |
| "why would recently accessed data be removed when memory runs out" | caching.md | **caching.md 0.172**, **caching.md 0.157**, redis.md 0.156 |
| "how can I make a login token disappear automatically after a while" | redis.md | rate-limiter.md 0.478, **redis.md 0.155**, rate-limiter.md 0.147 |

Acceptance criterion формально выполнен: ожидаемая заметка присутствует в top-3
для всех трёх запросов.

## Observed retrieval mistakes / limitations

1. Query 1 — ожидаемая заметка заняла последнее место в top-3 при почти
   неразличимых scores (0.158 vs 0.133) — слабый, не уверенный сигнал.
2. Query 3 — верхний результат (score 0.478, заметно выше остальных) —
   `rate-limiter.md`, а не ожидаемая `redis.md`. Причина: слово "token" в
   query совпало с "Token Bucket" в rate-limiter.md сильнее по частоте
   токенов, чем "session tokens" в redis.md — совпадение слова победило
   совпадение смысла.
3. Все scores по всем запросам скучены в диапазоне ~0.13–0.18 (кроме одного
   выброса) — слабая дискриминативность, ожидаемая для хэш bag-of-words на
   коротких запросах против абзацных чанков.

## Conclusion

Гипотеза подтверждается лишь частично и хрупко: bag-of-words/hashing embedding
местами угадывает нужную заметку, но по лексическому пересечению, а не по
смыслу. Query 3 — прямой контрпример: топ-результат релевантен по случайному
слову, а не по смыслу запроса. Для честной проверки исходной гипотезы
(семантическое сходство при разных словах) потребуется провайдер с настоящей
семантикой (например, локальная ONNX-модель) — обсуждалось до реализации и
сознательно отложено в пользу более простого варианта для этой сессии.

## Deferred (out of scope for this session)

Финальная генерация ответа, векторная БД, реальный embedding API, обработка
полного Obsidian vault, продвинутый Markdown-парсинг, production error
handling.
