# Personal AI Build Project

## Project Goal

This is a learning project for building a personal AI platform over Markdown notes from Obsidian.

The project evolves cumulatively through these stages:

1. Mini-RAG over Markdown notes
2. Golden set and LLM-as-judge evaluation
3. Agent with 2–3 tools and execution tracing
4. MCP server
5. Prompt injection experiments and basic defenses
6. Caching and routing between cheaper and more capable models

The goal is to understand the components of modern AI systems experimentally.

This is not intended to become a production-ready product.

## Working Method

Work is organized into 60-minute Build sessions.

Each session must have:

* one active stage;
* one explicit learning hypothesis;
* one concrete result;
* one observable acceptance criterion.

Before changing code:

1. Inspect the current repository.
2. Restate the session hypothesis and acceptance criterion.
3. Propose a minimal implementation plan.
4. List the files that will be created or changed.
5. Wait for approval before implementation.

Do not produce a large complete solution immediately.

## User and Agent Responsibilities

The agent writes most of the code.

The user must remain able to:

* understand the proposed design;
* approve the scope;
* review the changed files;
* verify build and runtime behavior;
* understand important technologies and trade-offs.

Explain unfamiliar AI concepts when they become relevant to a decision.

Do not hide important behavior behind frameworks or abstractions.

## Scope Rules

Only the current stage is active.

Do not add features that are not required to test the current learning hypothesis.

Out of scope unless explicitly approved:

* UI;
* public deployment;
* README polishing;
* demo packaging;
* premature generalization;
* speculative extension points;
* production infrastructure;
* unrelated refactoring.

Record new ideas separately instead of adding them to the current scope.

If a stage reaches a third session, propose a smaller scope.

## Technical Direction

Primary stack:

* .NET
* C#
* console applications unless another host is required by the active experiment

Prefer simple, explicit code over framework-heavy solutions.

For the initial implementation:

* use one console project;
* organize code by capability;
* use manual dependency construction in `Program.cs`;
* introduce interfaces only at meaningful external boundaries;
* keep vector data in memory;
* do not introduce Clean Architecture, CQRS, MediatR, or a DI container without a demonstrated need.

Use another language only when it provides a substantial learning advantage and explain the reason first.

## Change Discipline

Keep changes small and reviewable.

Before implementation, state:

* why each changed file is needed;
* what behavior it introduces;
* what is intentionally deferred.

During implementation:

* do not modify unrelated files;
* do not silently expand the task;
* do not add packages without explaining their purpose;
* do not replace clear code with unnecessary abstractions;
* do not claim success without running the relevant commands.

After implementation, report:

* files changed;
* commands executed;
* actual results;
* remaining limitations;
* decisions or mistakes worth recording.

## Current Stage

Stage 2: Golden set and LLM-as-judge evaluation.

Stage 1 (Mini-RAG over Markdown notes) is done: hashing embeddings, then a
local ONNX multilingual embedding model, then heading-based chunking. See
`sessions/2026-07-31-stage1-mini-rag.md` through
`sessions/2026-08-01-stage1-mini-rag-session3.md`.

Stage 2 session 1 (golden set formalized, Ollama `llama3` LLM-as-judge
wired up) is done. See
`sessions/2026-08-06-stage2-golden-set-llm-judge-session1.md`.

Stage 2 session 2 (settings externalized into `appsettings.json`, run
outcomes persisted to `runs/*.json`, corpus expanded with paraphrase/
distractor notes) is done. See
`sessions/2026-08-14-stage2-golden-set-llm-judge-session2.md`.

## Current Session Hypothesis

Aggregating the run logs already captured in `runs/*.json` into simple
retrieval metrics (e.g. Recall@K, Mean Reciprocal Rank, judge-vs-expected
agreement rate) makes it possible to experimentally compare two retrieval
configurations side by side, using numbers instead of re-reading console
output by hand.

## Current Session Result

Not yet run — this is the next session, anticipated but not started. Likely
shape: a small aggregation step (script or console mode) that reads one or
more `runs/*.json` files and prints/compares metrics; see the "Deferred"
section of `sessions/2026-08-14-stage2-golden-set-llm-judge-session2.md` for
the open methodological question (how to score queries where more than one
note in the corpus legitimately answers the query) that this session should
resolve before writing the aggregation logic.

## Acceptance Criterion

Not yet defined — to be restated at the start of that session per the
Working Method above.

## Current Session Non-Goals

Do not add:

* a paid/cloud LLM API (Anthropic, OpenAI, etc.) — local only, decided
  explicitly because of cost;
* a vector database;
* an agent framework;
* MCP;
* prompt-injection defenses;
* caching or model routing;
* processing of the complete Obsidian vault;
* production error handling.


## Mandatory Approval Gate

At the beginning of every session, remain read-only.

Do not create, modify, rename, or delete files.
Do not install packages.
Do not run commands that change repository state.

First present the implementation plan and proposed file list.

Implementation is allowed only after the user sends the exact phrase:

`План принят, начинай реализацию.`

Any other discussion, question, or approval wording must not be treated
as permission to modify the repository.
