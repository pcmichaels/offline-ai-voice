# offline-ai-voice

**Repository:** [github.com/pcmichaels/offline-ai-voice](https://github.com/pcmichaels/offline-ai-voice)

## Overview

Proof-of-concept for **local voice conversation with an LLM**: you speak, the app transcribes your speech, sends text to a locally hosted model, and speaks the reply aloud. The LLM runs in **LM Studio** on your machine (not in Docker). Speech-to-text (**Faster Whisper**) and text-to-speech (**Piper**) are packaged in **Docker** and started by a single run script.

## Technology stack

| Component | Technology |
|-----------|------------|
| Application | .NET 9 console app |
| Terminal UI | Spectre.Console |
| LLM | LM Studio (host, user-started) |
| STT | Faster Whisper (Docker) |
| TTS | Piper (Docker) |
| Run entry point | `utils/run-docker.ps1` |

See [docs/spec.md](docs/spec.md) for full requirements and architecture.

## Implemented

- **Phase 1 — Solution shell:** .NET solution under `src/`; Spectre welcome UI; configuration from `data/appsettings.json`; clean exit.
- **Phase 2 — Docker services:** `docker/docker-compose.yml` with STT (port 5001) and TTS (port 5002); Faster Whisper and Piper images; `utils/run-docker.ps1` build/up/health wait + client launch; Spectre **service health** panel (`healthy` / `unreachable`).
- **Phase 3 — STT echo:** Push-to-talk microphone capture (NAudio); HTTP transcription via STT container; session transcript panel with **`You said:`** labels (including `(no speech detected)` for silence).
- **Phase 4 — LLM text echo:** LM Studio OpenAI-compatible chat; in-memory history capped by `Session:MaxHistoryMessages`; startup LM Studio connectivity check; session transcript shows **`Assistant:`** after each spoken turn.
- **Phase 5 — Voice reply:** Piper TTS via Docker (`audio/wav` response); NAudio speaker playback after assistant text is shown (text remains on screen; audio errors reported separately).
- **Phase 6 — Multi-turn:** Session loop until `q`; rolling Spectre transcript panel with turn count; LM Studio payload capped via `Session:MaxHistoryMessages` (`ChatHistoryTrimmer` / `LlmConversationMessages`); separate **Transcribing** / **Thinking** status; follow-up questions retain prior context.
- **Phase 5b — Self-test:** `-SelfTest` / `--self-test` broadcasts `SelfTest:Phrase` via Piper while the mic records concurrently; console shows **`Broadcast:`** and optional **`Heard:`** (when STT is up). See [spec section 19](docs/spec.md#19-self-test-mode-self-test-replaces-mic-test).
- **Phase 7 — POC sign-off:** Automated unit tests; optional gated integration tests; configuration validation at startup; manual checklist in [docs/poc-checklist.md](docs/poc-checklist.md).

## Not Yet Implemented

- **Post-transcription readback** — after `You said:`, optionally hear the transcript spoken back (TTS) or play the recording ([spec §20](docs/spec.md#20-post-transcription-readback-optional))
- **Optional translation** — offer Spanish, Mandarin, or German via LM Studio; show `Translation (...):` in the log ([spec §21](docs/spec.md#21-optional-translation-spanish-mandarin-german))
- Other Phase 8 enhancements (VAD, streaming, script flags) — see [docs/todo.md](docs/todo.md)

## Planned incremental milestones

| Phase | What you will see when it ships |
|-------|--------------------------------|
| 1 | Spectre welcome + config (`dotnet run`) |
| 2 | Docker STT/TTS up; service health in console |
| 3 | Speak -> **text echo** (`You said: ...`) — **implemented** |
| 4 | + **Assistant text** from LM Studio — **implemented** |
| 5 | + **Spoken** reply (text still on screen) — **implemented** |
| 5b | **Self-test**: broadcast configured phrase while mic listens (`-SelfTest`) — **implemented** |
| 6 | Multi-turn conversation in session log — **implemented** |
| 7 | Tests + POC sign-off — **implemented** |
| 8a | Optional **readback** of transcribed text (TTS) |
| 8b | Optional **translation** (Spanish / Mandarin / German) |

Details: [docs/todo.md](docs/todo.md), [docs/spec.md](docs/spec.md) sections 17-18.

## Prerequisites

1. Windows with **Docker Desktop** running
2. **.NET SDK** 9.0+
3. **PowerShell** to run `.\utils\run-docker.ps1`
4. **LM Studio** with local server started (required from Phase 4 onward)

## How to run

### Phase 2+ (recommended)

From the repository root:

```powershell
.\utils\run-docker.ps1
# same as:
.\docker-run.ps1
```

The script will:

1. Verify Docker is available
2. Build and start STT/TTS containers (first build may take several minutes)
3. Wait for health endpoints on ports 5001 and 5002
4. Build and launch the .NET client

The console shows configuration, **STT/TTS/LM Studio health**, then a **multi-turn push-to-talk** session: record speech, see `You said:` and `Assistant:` text, hear the reply, then ask follow-up questions (`q` to exit). LM Studio is not started by this script.

Optional flags:

```powershell
.\utils\run-docker.ps1 -SkipBuild
.\utils\run-docker.ps1 -Configuration Debug
.\utils\run-docker.ps1 -NonInteractive   # health panel only; no voice session
```

`-NonInteractive` (or `dotnet run -- --no-prompt`) is useful when you only want service health checks and a clean exit.

**Self-test:** `.\docker-run.ps1 -SelfTest` — plays `SelfTest:Phrase` from `data/appsettings.json` through speakers while the mic records (starts Docker STT/TTS; no LM Studio). Optional duration (2–60 s): `-SelfTestSeconds 8` or `--self-test-seconds=8` (default from `SelfTest:DurationSeconds` in config). Deprecated aliases `-MicTest` / `--mic-test` still work with a warning. See [docs/spec.md](docs/spec.md) section 19 and [docs/build.md](docs/build.md#self-test-phase-5b).

You can run the same script from `utils/` as `.\run-docker.ps1` (repo root is resolved automatically).

See [docs/build.md](docs/build.md) for Docker API contract, pinned versions, and troubleshooting.

### Phase 1 only (no Docker)

```powershell
dotnet run --project src/AiVoiceTest
```

Configuration summary only; service health checks will show `unreachable` unless containers are already running.

## Project structure

- `docker-run.ps1` - Root wrapper for `utils/run-docker.ps1`
- `utils/run-docker.ps1` - Primary run script (documented above and in [docs/build.md](docs/build.md))
- `docker/` - Docker Compose, Dockerfiles, STT/TTS HTTP services
- `agents/` - Local agent personality files (gitignored; see `AGENTS.md`)
- `data/` - Configuration and persisted data
- `docs/` - `spec.md`, `todo.md`, `build.md`
- `src/` - Source code
- `tests/` - Unit and integration test projects
- `utils/` - Helper scripts and tools
- `AGENTS.md` - Cursor agent and project rules

## Testing

Unit tests (default; excludes integration):

```powershell
dotnet test src/AiVoiceTest.sln --filter "Category!=Integration"
```

Optional integration tests (Docker STT/TTS + LM Studio must be running):

```powershell
$env:AI_VOICE_TEST_INTEGRATION = "1"
dotnet test src/AiVoiceTest.sln --filter "Category=Integration"
```

| Project | Coverage |
|---------|----------|
| `tests/AiVoiceTest.Core.Tests` | History cap, LLM payload, config validation, transcript/self-test labels, session log |
| `tests/AiVoiceTest.Infrastructure.Tests` | `VoiceSessionOrchestrator` (STT/LLM wiring) |
| `tests/AiVoiceTest.Integration.Tests` | Live health checks (gated) |

Manual POC acceptance: [docs/poc-checklist.md](docs/poc-checklist.md) (phase gates 1-6, speech echo criteria).

## Documentation

- [Specification](docs/spec.md)
- [Task list](docs/todo.md)
- [Build and Docker](docs/build.md)
