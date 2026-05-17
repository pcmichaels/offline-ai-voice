#Requires -Version 5.1
<#
.SYNOPSIS
  Wrapper for utils/run-docker.ps1 (same parameters and behavior).

.DESCRIPTION
  Run from the repository root so paths resolve consistently:
    .\docker-run.ps1
    .\docker-run.ps1 -SelfTest
    .\docker-run.ps1 --self-test
#>
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $scriptDir "utils\run-docker.ps1") @args
