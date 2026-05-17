# POC manual acceptance checklist (Phase 7)

Use this checklist before POC sign-off. Automated unit tests: `dotnet test src/AiVoiceTest.sln --filter "Category!=Integration"`.

## Prerequisites

1. Windows, Docker Desktop running, .NET SDK 9+
2. LM Studio running with local server (Phases 4-6)
3. Microphone and speakers on the host

## Phase gates

| Phase | Command | Verify |
|-------|---------|--------|
| 1 | `dotnet run --project src/AiVoiceTest -- --no-prompt` | Welcome, config summary, exit 0 (services may be unreachable) |
| 2 | `.\utils\run-docker.ps1 -NonInteractive` | STT/TTS health `healthy` in panel |
| 3 | `.\utils\run-docker.ps1` (LM Studio optional) | Speak once; `You said:` appears in session transcript |
| 4 | `.\utils\run-docker.ps1` (LM Studio required) | `You said:` and `Assistant:` text after one question |
| 5 | Same as Phase 4 | Assistant text remains; reply is **audible** |
| 5b | `.\utils\run-docker.ps1 -SelfTest` | `Broadcast:` line; configured phrase plays while mic records |
| 6 | Same as Phase 5 | Two turns; second answer uses context from first |
| 7 | `dotnet test src/AiVoiceTest.sln --filter "Category!=Integration"` | All unit tests pass |

## Speech echo (spec 18.5)

1. User speech appears as text after each recording (`You said:`).
2. Assistant reply appears as text before/during TTS (`Assistant:`).
3. Silence shows `You said: (no speech detected)` (not blank).
4. TTS failure still leaves assistant text visible.

## Failure paths

1. Docker stopped: `run-docker.ps1` fails with clear message.
2. LM Studio stopped: client exits non-zero with LM Studio URL hint.
3. STT/TTS unhealthy: client exits before voice session.

## Optional integration tests

With Docker + LM Studio running:

```powershell
$env:AI_VOICE_TEST_INTEGRATION = "1"
dotnet test src/AiVoiceTest.sln --filter "Category=Integration"
```
