# TODO

Implementation follows `docs/spec.md`. Execute phases in order.

**Rules:**

1. A phase is **not complete** until its **phase gate** passes (runnable demo of new behavior).
2. From **Phase 2** onward, phase gates use `.\utils\run-docker.ps1` unless noted.
3. **All spoken content is echoed as text** (`You said:` / `Assistant:`) per spec section 18; implement echo in Phase 3 (user) and extend in Phases 4-6 (assistant).

---

## Phase 1: Solution skeleton and runnable shell

**Objective:** .NET solution exists; console app runs with Spectre UI and configuration loaded.

**Dependencies:** None.

**Estimated effort:** Small (0.5-1 day).

**Run for phase gate:** `dotnet run --project src/AiVoiceTest` (Docker not required).

**New distinct behavior:** Welcome banner, configuration summary, milestone label (e.g. `Phase 1: Shell`), clean exit.

- [x] 1.1.1 - Create `src/` solution (`AiVoiceTest.sln`) with `AiVoiceTest`, `AiVoiceTest.Core`, `AiVoiceTest.Infrastructure` projects
- [x] 1.1.2 - Add Spectre.Console and Microsoft.Extensions.* packages to the console host
- [x] 1.1.3 - Add `data/appsettings.json` schema and configuration binding stubs
- [x] 1.1.4 - Add `.gitignore` entries for `data/temp/`, build artifacts, and local secrets
- [x] 1.1.5 - Implement minimal Spectre welcome UI and milestone banner in host
- [x] 1.1.6 - **Phase gate 1:** `dotnet run` shows welcome + config; exits cleanly

---

## Phase 2: Docker infrastructure and `run-docker.ps1`

**Objective:** Containerize STT/TTS; one-command script builds services and launches client.

**Dependencies:** Phase 1.

**Estimated effort:** Medium (1-2 days).

**Run for phase gate:** `.\utils\run-docker.ps1`

**New distinct behavior:** Docker images build; STT/TTS containers start; client reports each service **healthy / unreachable** (no microphone yet).

- [x] 2.1.1 - Add `docker/docker-compose.yml` defining STT and TTS services with published ports and health checks
- [x] 2.1.2 - Add Dockerfiles for Faster Whisper (STT) and Piper (TTS) with pinned base images and models documented
- [x] 2.1.3 - Implement minimal HTTP health (and stub API) contract for STT/TTS containers; document in `docs/build.md`
- [x] 2.1.4 - Create `utils/run-docker.ps1`: preflight, `docker compose build`, `docker compose up -d`, health wait
- [x] 2.1.5 - Extend `utils/run-docker.ps1` to `dotnet build` and `dotnet run`; propagate exit codes
- [x] 2.1.6 - Client startup: Spectre panel listing STT/TTS endpoint status (distinct from Phase 1 config-only view)
- [x] 2.1.7 - Document `utils/run-docker.ps1` in `README.md`, `utils/README.md`, and `docs/build.md`
- [x] 2.1.8 - **Phase gate 2:** `.\utils\run-docker.ps1` completes; terminal shows STT + TTS health status (LM Studio not required)

---

## Phase 3: Microphone capture and speech-to-text echo

**Objective:** User can speak; transcribed text appears in the console.

**Dependencies:** Phase 2.

**Estimated effort:** Medium (1-2 days).

**Run for phase gate:** `.\utils\run-docker.ps1`

**New distinct behavior:** Push-to-talk (or equivalent); after speaking, terminal shows **`You said: <transcript>`** and appends to session transcript log. LM Studio not required.

- [x] 3.1.1 - Implement audio capture on Windows host (push-to-talk minimum)
- [x] 3.1.2 - Implement `ISpeechToTextService` client for STT container
- [x] 3.1.3 - Wire record -> STT -> display transcript with `You said:` label (Spectre panel/log)
- [x] 3.1.4 - Handle empty/silence STT with explicit `(no speech detected)` message
- [x] 3.1.5 - Update milestone banner to `Phase 3: STT echo`
- [x] 3.1.6 - **Phase gate 3:** Speak a short phrase; see accurate (or reasonably close) text echo; prior lines remain in session log

---

## Phase 4: LM Studio integration and assistant text echo

**Objective:** Transcribed speech is sent to LM Studio; assistant reply shown as text.

**Dependencies:** Phase 3.

**Estimated effort:** Medium (1 day).

**Run for phase gate:** `.\utils\run-docker.ps1` (LM Studio local server **must** be running).

**New distinct behavior:** After `You said: ...`, terminal shows **`Assistant: <reply text>`** from LM Studio. No TTS audio required in this phase.

- [x] 4.1.1 - Define remaining service interfaces and `IVoiceSessionOrchestrator` skeleton
- [x] 4.1.2 - Implement `ILlmChatService` (LM Studio HTTP, chat completions, in-memory history)
- [x] 4.1.3 - Connect STT output -> LLM -> print `Assistant:` line in session log
- [x] 4.1.4 - Startup validation for LM Studio with clear Spectre error if unreachable
- [x] 4.1.5 - `run-docker.ps1` LM Studio reminder when LLM check fails (script still does not start LM Studio)
- [x] 4.1.6 - Update milestone banner to `Phase 4: LLM text echo`
- [x] 4.1.7 - **Phase gate 4:** Speak a question; see `You said:` and `Assistant:` text (no spoken reply yet)

---

## Phase 5: Text-to-speech (spoken assistant reply)

**Objective:** Assistant reply is played aloud; text echo retained.

**Dependencies:** Phase 4.

**Estimated effort:** Medium (1 day).

**Run for phase gate:** `.\utils\run-docker.ps1` (LM Studio running).

**New distinct behavior:** After `Assistant:` text is shown, user **hears** the reply via Piper. Text remains on screen during and after playback.

- [x] 5.1.1 - Implement `ITextToSpeechService` client for TTS container
- [x] 5.1.2 - Implement audio playback on Windows host
- [x] 5.1.3 - Orchestrate: LLM text echo first, then TTS + playback (text not cleared)
- [x] 5.1.4 - TTS failure path: assistant text still visible; audio error reported separately
- [x] 5.1.5 - Update milestone banner to `Phase 5: Voice reply`
- [x] 5.1.6 - **Phase gate 5:** One full spoken exchange: `You said:` + `Assistant:` text + audible reply

---

## Phase 5b: Self-test (rename `mic-test`, simultaneous listen + broadcast)

**Objective:** Replace microphone-only test with **self-test**: concurrently capture mic input and TTS-broadcast the configured phrase.

**Dependencies:** Phase 5 (TTS playback infrastructure exists).

**Estimated effort:** Small-Medium (0.5-1 day).

**Run for phase gate:** `.\utils\run-docker.ps1 -SelfTest` (or `dotnet run -- --self-test` if TTS fallback exists).

**New distinct behavior:** User hears `SelfTest:Phrase` from speakers while mic records; console shows **`Broadcast: ...`**; optional **`Heard: ...`** when STT is up.

- [x] 5b.1.1 - Add `SelfTest` section to `data/appsettings.json` (`Phrase`, `DurationSeconds`) with default phrase `I never did mind about the little things`
- [x] 5b.1.2 - Bind `SelfTestOptions` in configuration layer
- [x] 5b.1.3 - Rename `MicTestArgs` / `MicTestRunner` to `SelfTestArgs` / `SelfTestRunner`; CLI `--self-test`, `--self-test-seconds=N`
- [x] 5b.1.4 - Implement concurrent capture + TTS broadcast of `SelfTest:Phrase` (overlap, not sequential)
- [x] 5b.1.5 - Spectre output: `Broadcast:` line; retain peak meter; update completion messaging
- [x] 5b.1.6 - Rename `utils/run-docker.ps1` parameters `-MicTest` -> `-SelfTest`, `-MicTestSeconds` -> `-SelfTestSeconds`; pass `--self-test` to client
- [x] 5b.1.7 - Deprecation aliases: accept `--mic-test` / `-MicTest` with one-time warning (optional)
- [x] 5b.1.8 - Optional: when STT healthy, transcribe capture and print `Heard:` line
- [x] 5b.1.9 - Update `README.md`, `utils/README.md`, `docs/build.md` (remove mic-test docs; document self-test)
- [x] 5b.1.10 - **Phase gate 5b:** `-SelfTest` plays configured phrase while recording; `Broadcast:` visible; changing `appsettings.json` phrase changes spoken output

---

## Phase 6: Multi-turn conversation

**Objective:** Session history across turns; context sent to LM Studio.

**Dependencies:** Phase 5.

**Estimated effort:** Small-Medium (0.5-1 day).

**Run for phase gate:** `.\utils\run-docker.ps1` (LM Studio running).

**New distinct behavior:** Second spoken turn references context from the first (e.g. follow-up question); session log shows **multiple** `You said:` / `Assistant:` pairs.

- [x] 6.1.1 - Enforce `Session:MaxHistoryMessages` when building LM Studio payload
- [x] 6.1.2 - Session loop: repeat record -> STT echo -> LLM -> TTS until user exits
- [x] 6.1.3 - Spectre session transcript scroll/log for all turns
- [x] 6.1.4 - Update milestone banner to `Phase 6: Multi-turn`
- [x] 6.1.5 - **Phase gate 6:** Two-turn dialogue with visible text log for both turns and coherent follow-up answer

---

## Phase 7: Testing and POC sign-off

**Objective:** Automated tests and final acceptance.

**Dependencies:** Phase 6.

**Estimated effort:** Small-Medium (1 day).

**Run for phase gate:** `dotnet test` and `.\run-docker.ps1` full checklist (spec section 12).

**New distinct behavior:** Test suite green; README "Implemented" list complete.

- [x] 7.1.1 - Unit tests: orchestrator, history cap, configuration validation, echo labels present in output helpers
- [x] 7.1.2 - Optional integration tests (gated; LM Studio + Docker)
- [x] 7.1.3 - Manual checklist: all phase gates 1-6 reproducible; speech echo criteria (spec 18.5)
- [x] 7.1.4 - Update README implemented vs. not implemented; mark spec acceptance criteria met
- [x] 7.1.5 - **Phase gate 7:** Tests pass; full POC demo documented in README

---

## Phase 8: Enhancements (post-POC)

**Objective:** Optional improvements after POC acceptance.

**Dependencies:** Phase 7 complete.

**Estimated effort:** Variable.

- [ ] 8.1.1 - Voice activity detection (hands-free end of utterance)
- [ ] 8.1.2 - Streaming STT/LLM/TTS
- [ ] 8.1.3 - `run-docker.ps1` optional flags (`-SkipBuild`, etc.)
