#Requires -Version 5.1

<#

.SYNOPSIS

  Build STT/TTS Docker services and run the .NET voice client.



.DESCRIPTION

  Phase 2+ entry point: preflight Docker, compose build/up, wait for health,

  then dotnet build and dotnet run. Does NOT start LM Studio.



  Script location: utils/run-docker.ps1 (repository root is parent of utils/).



.EXAMPLE

  # From repository root

  .\utils\run-docker.ps1



.EXAMPLE

  # From utils/

  .\run-docker.ps1



.EXAMPLE

  # Self-test: broadcast configured phrase while mic records (no LM Studio)

  .\run-docker.ps1 -SelfTest

  .\run-docker.ps1 --self-test



.EXAMPLE

  # From repo root (wrapper script)

  .\docker-run.ps1 -SelfTest

#>

[CmdletBinding()]

param(

    [switch] $SkipBuild,

    [switch] $NonInteractive,

    [switch] $SelfTest,



    # String (not int) so '--self-test' is never coerced as a positional integer argument

    [string] $SelfTestSeconds = "",



    # Deprecated aliases (see -SelfTest)

    [switch] $MicTest,

    [string] $MicTestSeconds = "",



    [string] $Configuration = "Release",



    [Parameter(ValueFromRemainingArguments = $true)]

    [string[]] $ExtraArgs

)



$ErrorActionPreference = "Stop"

$script:DeprecationWarningShown = $false



$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

$composeFile = Join-Path $repoRoot "docker/docker-compose.yml"

$solutionPath = Join-Path $repoRoot "src/AiVoiceTest.sln"

$projectPath = Join-Path $repoRoot "src/AiVoiceTest/AiVoiceTest.csproj"

$appSettingsPath = Join-Path $repoRoot "data/appsettings.json"

$sttHealthUrl = "http://localhost:5001/health"

$ttsHealthUrl = "http://localhost:5002/health"



$extraArgList = if ($null -eq $ExtraArgs) { @() } else { $ExtraArgs }

foreach ($a in $extraArgList) {

    if ($a -eq "--self-test") { $SelfTest = $true }

    elseif ($a -eq "--mic-test") { $SelfTest = $true; $MicTest = $true }

    elseif ($a -match "^--self-test-seconds=(\d+)$") {

        $SelfTestSeconds = $Matches[1]

        $SelfTest = $true

    }

    elseif ($a -match "^--mic-test-seconds=(\d+)$") {

        $MicTestSeconds = $Matches[1]

        $SelfTest = $true

        $MicTest = $true

    }

}



if ($MicTest) {

    $SelfTest = $true

}



if ($SelfTestSeconds -eq "--self-test" -or $SelfTestSeconds -eq "--mic-test") {

    $SelfTest = $true

    $SelfTestSeconds = ""

}



if ([string]::IsNullOrWhiteSpace($SelfTestSeconds) -and -not [string]::IsNullOrWhiteSpace($MicTestSeconds)) {

    $SelfTestSeconds = $MicTestSeconds

}



function Show-DeprecationWarning {

    if ($script:DeprecationWarningShown) { return }

    if ($MicTest -or -not [string]::IsNullOrWhiteSpace($MicTestSeconds)) {

        Write-Host "Note: -MicTest / --mic-test are deprecated; use -SelfTest / --self-test instead." -ForegroundColor Yellow

        $script:DeprecationWarningShown = $true

    }

}



function Get-SelfTestDurationSeconds {

    param([string] $CliOverride)



    if (-not [string]::IsNullOrWhiteSpace($CliOverride)) {

        if ($CliOverride -notmatch "^\d+$") {

            throw "Invalid -SelfTestSeconds: '$CliOverride'. Use an integer 2-60, or `--self-test-seconds=N`."

        }



        $parsed = [int]$CliOverride

        if ($parsed -lt 2 -or $parsed -gt 60) {

            throw "Self-test duration must be between 2 and 60 seconds. Got: $parsed."

        }



        return $parsed

    }



    if (Test-Path -LiteralPath $script:appSettingsPath) {

        try {

            $json = Get-Content $script:appSettingsPath -Raw | ConvertFrom-Json

            if ($null -ne $json.SelfTest.DurationSeconds) {

                $fromConfig = [int]$json.SelfTest.DurationSeconds

                if ($fromConfig -ge 2 -and $fromConfig -le 60) {

                    return $fromConfig

                }

            }

        }

        catch {

            # fall through to default

        }

    }



    return 10

}



function Write-Step([string] $Message) {

    Write-Host ""

    Write-Host "==> $Message" -ForegroundColor Cyan

}



function Assert-RepositoryLayout {

    param(

        [string] $RepoRoot,

        [switch] $ForSelfTest

    )



    if (-not (Test-Path -LiteralPath $RepoRoot)) {

        throw "Repository root not found: $RepoRoot"

    }



    if (-not (Test-Path -LiteralPath $script:solutionPath)) {

        throw "Required file missing: $script:solutionPath. Use docker-run.ps1 from the repository root, or run utils\run-docker.ps1 from a full clone."

    }



    if (-not (Test-Path -LiteralPath $script:appSettingsPath)) {

        throw "Required file missing: $script:appSettingsPath. Restore data/appsettings.json."

    }



    if (-not $ForSelfTest) {

        if (-not (Test-Path -LiteralPath $script:composeFile)) {

            throw "Required file missing: $script:composeFile. Restore docker/docker-compose.yml."

        }

    }

    else {

        if (-not (Test-Path -LiteralPath $script:composeFile)) {

            throw "Required file missing: $script:composeFile. Self-test needs the TTS Docker service."

        }

    }

}



function Test-DockerAvailable {

    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {

        throw "Docker CLI not found. Install Docker Desktop and ensure 'docker' is on PATH."

    }



    docker info *> $null

    if ($LASTEXITCODE -ne 0) {

        throw "Docker engine is not running. Start Docker Desktop and retry."

    }

}



function Get-LmStudioBaseUrl {

    param([string] $RepoRoot)



    if (-not (Test-Path $script:appSettingsPath)) {

        return "http://localhost:1234"

    }



    try {

        $json = Get-Content $script:appSettingsPath -Raw | ConvertFrom-Json

        if ($json.Llm.BaseUrl) {

            return $json.Llm.BaseUrl.TrimEnd("/")

        }

    }

    catch {

        # fall through to default

    }



    return "http://localhost:1234"

}



function Test-LmStudioReachable {

    param([string] $BaseUrl)



    try {

        $response = Invoke-WebRequest -Uri "$BaseUrl/v1/models" -UseBasicParsing -TimeoutSec 5

        return $response.StatusCode -eq 200

    }

    catch {

        return $false

    }

}



function Wait-ServiceHealth {

    param(

        [string] $Name,

        [string] $Url,

        [int] $TimeoutSeconds = 300

    )



    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {

        try {

            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5

            if ($response.StatusCode -eq 200) {

                Write-Host "  $Name healthy at $Url" -ForegroundColor Green

                return

            }

        }

        catch {

            # retry until timeout

        }



        Start-Sleep -Seconds 3

    }



    throw "Timed out waiting for $Name health at $Url (${TimeoutSeconds}s)."

}



try {

    Set-Location $repoRoot



    if ($SelfTest) {

        Show-DeprecationWarning

        Assert-RepositoryLayout -RepoRoot $repoRoot -ForSelfTest



        if ($NonInteractive) {

            Write-Host "Note: -NonInteractive is ignored for self-test." -ForegroundColor Yellow

        }



        $selfTestSecondsInt = Get-SelfTestDurationSeconds -CliOverride $SelfTestSeconds



        Write-Step "Self-test (TTS broadcast + mic capture; LM Studio skipped)"

        Write-Step "Preflight: Docker"

        Test-DockerAvailable



        if (-not $SkipBuild) {

            Write-Step "Docker compose build"

            docker compose -f $composeFile build

            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        }



        Write-Step "Docker compose up (detached)"

        docker compose -f $composeFile up -d

        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



        Write-Step "Waiting for STT/TTS health"

        Wait-ServiceHealth -Name "STT" -Url $sttHealthUrl

        Wait-ServiceHealth -Name "TTS" -Url $ttsHealthUrl



        if (-not $SkipBuild) {

            Write-Step "Build .NET client ($Configuration)"

            dotnet build $solutionPath -c $Configuration

            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        }



        Write-Step "Run self-test client"



        $runArgs = @(

            "--project", $projectPath,

            "-c", $Configuration,

            "--no-build",

            "--",

            "--self-test",

            "--self-test-seconds=$selfTestSecondsInt"

        )



        dotnet run @runArgs

        exit $LASTEXITCODE

    }



    Assert-RepositoryLayout -RepoRoot $repoRoot



    Write-Step "Preflight: Docker"

    Test-DockerAvailable



    if (-not $SkipBuild) {

        Write-Step "Docker compose build"

        docker compose -f $composeFile build

        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    }



    Write-Step "Docker compose up (detached)"

    docker compose -f $composeFile up -d

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



    Write-Step "Waiting for STT/TTS health"

    Wait-ServiceHealth -Name "STT" -Url $sttHealthUrl

    Wait-ServiceHealth -Name "TTS" -Url $ttsHealthUrl



    Write-Step "Build .NET client ($Configuration)"

    dotnet build $solutionPath -c $Configuration

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



    Write-Step "Preflight: LM Studio"

    $llmBaseUrl = Get-LmStudioBaseUrl -RepoRoot $repoRoot

    if (-not (Test-LmStudioReachable -BaseUrl $llmBaseUrl)) {

        Write-Host "LM Studio does not appear to be running at $llmBaseUrl." -ForegroundColor Yellow

        Write-Host "Open LM Studio, load a model, and start the local server before voice chat (Phase 4+)." -ForegroundColor Yellow

        Write-Host "The client will still launch and report a clear error if the LLM endpoint is unreachable." -ForegroundColor Yellow

    }

    else {

        Write-Host "  LM Studio reachable at $llmBaseUrl" -ForegroundColor Green

    }



    Write-Step "Run .NET client"



    $runArgs = @("--project", $projectPath, "-c", $Configuration, "--no-build")

    if ($NonInteractive) {

        $runArgs += @("--", "--no-prompt")

    }



    dotnet run @runArgs

    exit $LASTEXITCODE

}

catch {

    Write-Host $_.Exception.Message -ForegroundColor Red

    exit 1

}


