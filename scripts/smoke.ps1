param(
    [string]$Configuration = "Debug",
    [string]$ApiUrl = "https://localhost:7189",
    [string]$WebUrl = "https://localhost:7068",
    [string]$ApiLaunchProfile = "https",
    [string]$WebLaunchProfile = "https",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

function Wait-ForReady {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSeconds = 60,
        [System.Diagnostics.Process]$Process = $null,
        [string]$ProcessName = "process"
    )

    $start = Get-Date
    while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSeconds) {
        if ($Process -and $Process.HasExited) {
            throw "$ProcessName exited early with code $($Process.ExitCode) while waiting for $Url"
        }

        $code = & curl.exe -k -s -o NUL -w "%{http_code}" $Url
        if ($code -eq "200") {
            return
        }

        Start-Sleep -Milliseconds 750
    }

    $apiOutTail = if (Test-Path "api-smoke.out.log") { (Get-Content "api-smoke.out.log" -Tail 120 -ErrorAction SilentlyContinue) -join [Environment]::NewLine } else { "(missing api-smoke.out.log)" }
    $apiErrTail = if (Test-Path "api-smoke.err.log") { (Get-Content "api-smoke.err.log" -Tail 120 -ErrorAction SilentlyContinue) -join [Environment]::NewLine } else { "(missing api-smoke.err.log)" }
    $webOutTail = if (Test-Path "web-smoke.out.log") { (Get-Content "web-smoke.out.log" -Tail 120 -ErrorAction SilentlyContinue) -join [Environment]::NewLine } else { "(missing web-smoke.out.log)" }
    $webErrTail = if (Test-Path "web-smoke.err.log") { (Get-Content "web-smoke.err.log" -Tail 120 -ErrorAction SilentlyContinue) -join [Environment]::NewLine } else { "(missing web-smoke.err.log)" }

    throw "Timed out waiting for $Url`n--- api-smoke.out.log ---`n$apiOutTail`n--- api-smoke.err.log ---`n$apiErrTail`n--- web-smoke.out.log ---`n$webOutTail`n--- web-smoke.err.log ---`n$webErrTail"
}

function Resolve-ListeningUrl {
    param(
        [Parameter(Mandatory = $true)][string]$LogPath,
        [Parameter(Mandatory = $true)][string]$FallbackUrl,
        [int]$TimeoutSeconds = 45
    )

    $start = Get-Date
    while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSeconds) {
        if (Test-Path $LogPath) {
            $content = Get-Content $LogPath -Raw -ErrorAction SilentlyContinue
            if ($null -eq $content) {
                $content = ""
            }
            $match = [regex]::Match($content, 'Now listening on:\s+(https?://[^\s]+)')
            if ($match.Success) {
                return $match.Groups[1].Value.TrimEnd('/')
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return $FallbackUrl.TrimEnd('/')
}

function Invoke-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Url,
        [string]$Method = "GET",
        [string]$BodyFile = "",
        [string[]]$Headers = @(),
        [int]$ExpectedStatus = 200
    )

    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        $args = @("-k", "-s", "-o", $tmp, "-w", "%{http_code}", "-X", $Method)
        foreach ($h in $Headers) {
            $args += @("-H", $h)
        }

        if (-not [string]::IsNullOrWhiteSpace($BodyFile)) {
            $args += @("--data-binary", "@$BodyFile")
        }

        $args += $Url
        $status = (& curl.exe @args)
        $body = Get-Content $tmp -Raw
        if ([int]$status -ne $ExpectedStatus) {
            throw "$Name failed. Expected $ExpectedStatus got $status. Body: $body"
        }

        Write-Host "PASS: $Name ($status)"
        return $body
    }
    finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
}

$apiProcess = $null
$webProcess = $null
$previousUseInMemory = $env:UseInMemoryDb
$previousServiceApi = $env:services__api__https__0
$previousServiceApiHttp = $env:services__api__http__0
$previousAspNetEnv = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnv = $env:DOTNET_ENVIRONMENT
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS
$previousEnableHttpsRedirection = $env:EnableHttpsRedirection

try {
    $env:UseInMemoryDb = "true"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:EnableHttpsRedirection = "false"
    $env:ASPNETCORE_URLS = $ApiUrl

    $apiOut = "api-smoke.out.log"
    $apiErr = "api-smoke.err.log"
    Remove-Item $apiOut, $apiErr -ErrorAction SilentlyContinue
    $apiArgs = @("run", "--project", "src/RadioPulse.Api/RadioPulse.Api.csproj", "--configuration", $Configuration)
    if ($ApiLaunchProfile -eq "none")
    {
        $apiArgs += "--no-launch-profile"
        $apiArgs += "-p:UseAppHost=false"
    }
    else
    {
        $apiArgs += @("--launch-profile", $ApiLaunchProfile)
    }
    if ($NoBuild) {
        $apiArgs += "--no-build"
    }

    $apiProcess = Start-Process dotnet `
        -ArgumentList $apiArgs `
        -PassThru `
        -RedirectStandardOutput $apiOut `
        -RedirectStandardError $apiErr

    $resolvedApiUrl = Resolve-ListeningUrl -LogPath $apiOut -FallbackUrl $ApiUrl
    Wait-ForReady -Url "$resolvedApiUrl/api/status" -TimeoutSeconds 90 -Process $apiProcess -ProcessName "api"

    $env:services__api__https__0 = $resolvedApiUrl
    $env:services__api__http__0 = $resolvedApiUrl

    $webOut = "web-smoke.out.log"
    $webErr = "web-smoke.err.log"
    Remove-Item $webOut, $webErr -ErrorAction SilentlyContinue
    $env:ASPNETCORE_URLS = $WebUrl
    $webArgs = @("run", "--project", "src/RadioPulse.Web/RadioPulse.Web.csproj", "--configuration", $Configuration)
    if ($WebLaunchProfile -eq "none")
    {
        $webArgs += "--no-launch-profile"
        $webArgs += "-p:UseAppHost=false"
    }
    else
    {
        $webArgs += @("--launch-profile", $WebLaunchProfile)
    }
    if ($NoBuild) {
        $webArgs += "--no-build"
    }

    $webProcess = Start-Process dotnet `
        -ArgumentList $webArgs `
        -PassThru `
        -RedirectStandardOutput $webOut `
        -RedirectStandardError $webErr

    $resolvedWebUrl = Resolve-ListeningUrl -LogPath $webOut -FallbackUrl $WebUrl
    Wait-ForReady -Url "$resolvedWebUrl/auth" -TimeoutSeconds 60 -Process $webProcess -ProcessName "web"

    Invoke-Check -Name "API status" -Url "$resolvedApiUrl/api/status" | Out-Null
    Invoke-Check -Name "API shows" -Url "$resolvedApiUrl/api/shows" | Out-Null
    Invoke-Check -Name "Web auth page" -Url "$resolvedWebUrl/auth" | Out-Null
    Invoke-Check -Name "Web engagement page" -Url "$resolvedWebUrl/engagement" | Out-Null
    Invoke-Check -Name "Web media page" -Url "$resolvedWebUrl/media" | Out-Null
    Invoke-Check -Name "Web diagnostics page" -Url "$resolvedWebUrl/diagnostics" | Out-Null
    Invoke-Check -Name "Web favicon" -Url "$resolvedWebUrl/favicon.ico" | Out-Null

    $tokenJson = Invoke-Check -Name "Dev token" -Url "$resolvedApiUrl/api/auth/dev-token/22222222-2222-2222-2222-222222222222"
    $token = ($tokenJson | ConvertFrom-Json).access_token
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Dev token endpoint returned an empty token."
    }

    $pollBody = [System.IO.Path]::GetTempFileName()
    $voteBody = [System.IO.Path]::GetTempFileName()
    $shoutBody = [System.IO.Path]::GetTempFileName()
    $badBody = [System.IO.Path]::GetTempFileName()

    try {
        Set-Content -Path $pollBody -Value '{"showId":"11111111-1111-1111-1111-111111111111","question":"Smoke poll"}' -Encoding ascii
        Invoke-Check -Name "Create poll" `
            -Url "$resolvedApiUrl/api/polls" `
            -Method "POST" `
            -BodyFile $pollBody `
            -Headers @("Authorization: Bearer $token", "Content-Type: application/json") | Out-Null

        $activePollJson = Invoke-Check -Name "Get active poll" -Url "$resolvedApiUrl/api/polls/active"
        $pollId = ($activePollJson | ConvertFrom-Json).id
        if ([string]::IsNullOrWhiteSpace($pollId)) {
            throw "Active poll ID was empty."
        }

        Set-Content -Path $voteBody -Value "{`"pollId`":`"$pollId`",`"userId`":`"22222222-2222-2222-2222-222222222222`",`"choice`":`"Track A`"}" -Encoding ascii
        Invoke-Check -Name "Create vote" `
            -Url "$resolvedApiUrl/api/polls/votes" `
            -Method "POST" `
            -BodyFile $voteBody `
            -Headers @("Authorization: Bearer $token", "Content-Type: application/json") | Out-Null

        Set-Content -Path $shoutBody -Value '{"userId":"22222222-2222-2222-2222-222222222222","message":"Smoke shoutout"}' -Encoding ascii
        Invoke-Check -Name "Create shoutout" `
            -Url "$resolvedApiUrl/api/shoutouts" `
            -Method "POST" `
            -BodyFile $shoutBody `
            -Headers @("Authorization: Bearer $token", "Content-Type: application/json") | Out-Null

        Set-Content -Path $badBody -Value '{bad json' -Encoding ascii
        Invoke-Check -Name "Invalid payload returns 400" `
            -Url "$resolvedApiUrl/api/polls" `
            -Method "POST" `
            -BodyFile $badBody `
            -Headers @("Authorization: Bearer $token", "Content-Type: application/json") `
            -ExpectedStatus 400 | Out-Null
    }
    finally {
        Remove-Item $pollBody, $voteBody, $shoutBody, $badBody -ErrorAction SilentlyContinue
    }

    Write-Host "Smoke suite completed successfully."
}
finally {
    if ($webProcess -and -not $webProcess.HasExited) {
        Stop-Process -Id $webProcess.Id -Force
    }
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }

    if ($null -eq $previousUseInMemory) { Remove-Item Env:UseInMemoryDb -ErrorAction SilentlyContinue } else { $env:UseInMemoryDb = $previousUseInMemory }
    if ($null -eq $previousServiceApi) { Remove-Item Env:services__api__https__0 -ErrorAction SilentlyContinue } else { $env:services__api__https__0 = $previousServiceApi }
    if ($null -eq $previousServiceApiHttp) { Remove-Item Env:services__api__http__0 -ErrorAction SilentlyContinue } else { $env:services__api__http__0 = $previousServiceApiHttp }
    if ($null -eq $previousAspNetEnv) { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue } else { $env:ASPNETCORE_ENVIRONMENT = $previousAspNetEnv }
    if ($null -eq $previousDotnetEnv) { Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue } else { $env:DOTNET_ENVIRONMENT = $previousDotnetEnv }
    if ($null -eq $previousAspNetCoreUrls) { Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue } else { $env:ASPNETCORE_URLS = $previousAspNetCoreUrls }
    if ($null -eq $previousEnableHttpsRedirection) { Remove-Item Env:EnableHttpsRedirection -ErrorAction SilentlyContinue } else { $env:EnableHttpsRedirection = $previousEnableHttpsRedirection }
}
