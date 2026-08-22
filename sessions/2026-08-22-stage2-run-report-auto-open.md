# Stage 2 — Run report auto-open (tooling, off-hypothesis) — 2026-08-22

## Context

Stage 2 session 3 (aggregating run logs into metrics: Recall@K, MRR, judge
agreement — commit `0b2c3d4`, "feat: add metrics mode aggregating Recall@K,
MRR, judge agreement from run logs") had already landed on `master` before
this session started, but has no session note of its own — it isn't
documented in `sessions/`, and `CLAUDE.md`'s "Current Session" section still
describes that work as "not yet run." This session does not retroactively
write that note: there's no first-hand context here for the design decisions
made during it, only the diff. Flagging the gap so it isn't mistaken for
closed out.

## What this session actually was

Not a stage-progressing Build session under the project's Working
Method — no learning hypothesis about AI systems was tested, so there's no
"Hypothesis" / "Acceptance criterion" pair below. The user asked for a small
UX/tooling change that `CLAUDE.md`'s Scope Rules call out as normally out of
scope ("UI ... unless explicitly approved"); approval was given explicitly
in-conversation, so it proceeded as a scoped, isolated addition rather than
as part of the metrics-aggregation hypothesis.

## Request

Could the run log open in the browser automatically after a run? Clarified
via two follow-ups: **which** content (a summary of the main pipeline's
judge assessment / top-K / per-query breakdown — i.e. the `RunLog` written
after a normal run, not the separate `metrics` diff mode) and **what form**
(a simple HTML page, not the raw JSON).

## Implementation

- `src/PersonalAi/Evaluation/RunLogHtmlReport.cs` (new) — static
  `Render(RunLog) -> string`. Builds one self-contained HTML page per run:
  a header (timestamps, chunk count, TopK, judge model), then one block per
  golden-set query with an expected-note/top-K badge and a table of
  retrieved chunks (score, source file, judge verdict badge, reasoning,
  chunk preview). All text fields are escaped via
  `System.Net.WebUtility.HtmlEncode`, since chunk previews and judge
  reasoning originate from markdown notes / LLM output and can contain
  `<`/`>`/`&`. No new NuGet packages — inline CSS only, no external assets
  or JS.
- `src/PersonalAi/Program.cs` (changed) — inside the existing
  `if (settings.RunLog.Enabled)` block, right after the `runs/*.json` write:
  writes `runs/<timestamp>.html` (`Path.ChangeExtension` on the same path),
  then `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })`
  to hand the file to the OS's default handler.
- Not changed: `metrics` mode (console-only diff between two run logs) — the
  clarified request was specifically about the main run's judge/top-K
  breakdown, not the aggregation-mode output.

## Verification

No acceptance criterion was stated up front, since this wasn't a Build
session with a learning hypothesis — verified end-to-end instead:

```
dotnet build
dotnet run --launch-profile Experiment
```

Build: 0 warnings, 0 errors. Run: against Ollama (`llama3`) and the ONNX
model, 58 chunks, all 7 golden-set queries judged without exceptions.
Produced `runs/20260822-141027.json` and `runs/20260822-141027.html`
(11 KB; 7 query blocks; 28 `<tr>` = 7 header rows + 21 chunk rows, matching
7 queries × 3 top-K chunks). Confirmed via `osascript` that Chrome actually
opened a tab at `file:///.../runs/20260822-141027.html` after the run, not
merely that Chrome happened to already be running.

## Commit

`70c3735` — "feat: render run log as an HTML report and open it after each
run", pushed to `origin/master`. Staged only `Program.cs` and the new file;
deliberately left out an unrelated pre-existing uncommitted whitespace/
comment edit in `OnnxEmbeddingService.cs` that predates this session.

## Deferred / not done

- No session note exists yet for the metrics-aggregation work already on
  `master` (commit `0b2c3d4`) — should be written from that session's own
  context, not reconstructed after the fact.
- `metrics` mode still has no HTML/browser view — only the main run's
  `RunLog` does.
- `CLAUDE.md`'s "Current Stage" section is stale relative to `master` (it
  describes the metrics-aggregation hypothesis as not yet run, and doesn't
  mention this session at all) — worth reconciling before the next session,
  not done here since it wasn't part of this request.
