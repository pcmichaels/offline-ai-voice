# Utils

Helper scripts and run orchestration for the POC.

## `run-docker.ps1`

Primary entry point from **Phase 2 onward**: builds STT/TTS Docker images, starts containers, waits for health, then builds and runs the .NET client on the host.

**Does not** start or configure LM Studio.

### How to run

From the **repository root** (recommended):

```powershell
.\utils\run-docker.ps1
```

Or use the root wrapper (same script):

```powershell
.\docker-run.ps1
```

From this directory (`utils/`):

```powershell
.\run-docker.ps1
```

The script resolves the repository root automatically and runs `docker compose` / `dotnet` from there.

### Options

```powershell
.\utils\run-docker.ps1 -SkipBuild
.\utils\run-docker.ps1 -Configuration Debug
.\utils\run-docker.ps1 -NonInteractive   # health check only; no push-to-talk prompt
```

Use `-NonInteractive` (or run `dotnet run` with `-- --no-prompt`) when you only want the service health panel and a clean exit.

### Self-test (broadcast + mic capture)

Simultaneously **plays** `SelfTest:Phrase` from `data/appsettings.json` through speakers (Piper TTS) while **recording** the microphone. No LM Studio.

From the repo root:

```powershell
.\docker-run.ps1 -SelfTest
# or
.\docker-run.ps1 --self-test
```

From `utils/`:

```powershell
.\run-docker.ps1 -SelfTest
```

Optional duration (2–60 seconds). If omitted, uses `SelfTest:DurationSeconds` from config (default **10**):

```powershell
.\docker-run.ps1 -SelfTest -SelfTestSeconds 8
.\docker-run.ps1 --self-test --self-test-seconds=8
```

Use **`-Configuration`** for Debug/Release — do not pass it positionally (the first positional argument may bind to `-SelfTestSeconds`).

**Deprecated aliases:** `-MicTest`, `--mic-test`, and `--mic-test-seconds=N` still work with a one-time warning.

The self-test path starts Docker STT/TTS, waits for health, then runs `dotnet run -- --self-test`.

### Prerequisites

- Docker Desktop running
- .NET SDK 9.0+
- PowerShell 5.1+ or PowerShell 7+

See [docs/build.md](../docs/build.md) for Docker API details and troubleshooting.
