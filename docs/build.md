# Build and run

## Prerequisites

1. **Docker Desktop** (or compatible engine) running on Windows.
2. **.NET SDK** 9.0+ (solution targets `net9.0`).
3. **PowerShell** 5.1+ or PowerShell 7+.

LM Studio is **not** required for Phase 2 (service health only) or Phase 3 (STT text echo). From Phase 4 onward, start LM Studio manually before voice chat. Phase 5 also requires a healthy TTS container and host speakers.

## Primary run command (Phase 2+)

From the repository root:

```powershell
.\utils\run-docker.ps1
```

The script (also documented in the root [README.md](../README.md)):

1. Verifies Docker CLI and engine
2. Runs `docker compose -f docker/docker-compose.yml build` (unless `-SkipBuild`)
3. Runs `docker compose up -d`
4. Waits until `http://localhost:5001/health` and `http://localhost:5002/health` return HTTP 200
5. Runs `dotnet build` and `dotnet run` for the console client
6. Returns the client exit code

Optional:

```powershell
.\utils\run-docker.ps1 -SkipBuild
.\utils\run-docker.ps1 -Configuration Debug
```

## Docker services

| Service | Host port | Container port | Image |
|---------|-----------|----------------|-------|
| STT (Faster Whisper) | 5001 | 8080 | `docker/Dockerfile.stt` |
| TTS (Piper) | 5002 | 8080 | `docker/Dockerfile.tts` |

Compose project name: `aivoicetest`.

### Pinned versions (documented in Dockerfiles)

| Component | Pin |
|-----------|-----|
| Python base | `python:3.11.11-slim-bookworm` |
| Faster Whisper (pip) | `1.1.0` |
| Whisper model (build arg) | `small` (CPU, `int8`) |
| Piper release | `2023.11.14-2` (`piper_linux_x86_64`) |
| Piper voice | `en_US-lessac-medium` (rhasspy/piper-voices on Hugging Face) |

First `docker compose build` downloads models and can take several minutes.

### HTTP API contract (stub + health)

Base URLs match `data/appsettings.json` (`Stt:ServiceUrl`, `Tts:ServiceUrl`).

#### STT — Faster Whisper

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Returns `{"status":"healthy","service":"stt","model":"..."}` when model is loaded |
| `POST` | `/v1/transcribe` | Multipart audio file; returns `{"text":"..."}` |

#### TTS — Piper

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Returns `{"status":"healthy","service":"tts","voice_model":"..."}` when Piper binary and voice exist |
| `POST` | `/v1/synthesize` | JSON `{"text":"..."}`; returns `audio/wav` body |

Health endpoints are used by Docker `HEALTHCHECK`, `utils/run-docker.ps1`, and the .NET client service health panel.

## Self-test (Phase 5b)

Broadcast the configured phrase while capturing the microphone (no LM Studio):

```powershell
.\utils\run-docker.ps1 -SelfTest
```

Starts Docker STT/TTS, waits for health, then runs `dotnet run -- --self-test`. Override duration (2–60 s):

```powershell
.\utils\run-docker.ps1 -SelfTest -SelfTestSeconds 8
```

Configure the spoken phrase in `data/appsettings.json` under `SelfTest:Phrase` (default: *I never did mind about the little things*).

Direct client (TTS container must already be healthy):

```powershell
dotnet run --project src/AiVoiceTest -- --self-test
```

## Testing

```powershell
dotnet test src/AiVoiceTest.sln --filter "Category!=Integration"
```

Optional live service checks (Docker + LM Studio):

```powershell
$env:AI_VOICE_TEST_INTEGRATION = "1"
dotnet test src/AiVoiceTest.sln --filter "Category=Integration"
```

Manual POC checklist: [poc-checklist.md](poc-checklist.md).

## Phase 1 only (no Docker)

```powershell
dotnet run --project src/AiVoiceTest
```

Shows configuration only; does not check STT/TTS containers.

## Troubleshooting

| Symptom | Action |
|---------|--------|
| `Docker engine is not running` | Start Docker Desktop |
| STT health timeout on first build | Wait for image build to finish; Whisper model download is slow |
| Port 5001/5002 in use | Stop conflicting processes or change ports in `docker-compose.yml` and `data/appsettings.json` |
| TTS unhealthy | Check `docker compose logs tts`; verify Piper binary and voice downloaded in image |
| Client shows `unreachable` | Ensure containers are up: `docker compose -f docker/docker-compose.yml ps` |

## Manual Docker commands

```powershell
docker compose -f docker/docker-compose.yml build
docker compose -f docker/docker-compose.yml up -d
curl http://localhost:5001/health
curl http://localhost:5002/health
docker compose -f docker/docker-compose.yml down
```
