# Development only: sends the same GET request several times and prints duration and response size.
# Run: powershell -ExecutionPolicy Bypass -File scripts/measure-get-flights.ps1
# The first run is "cold" (JIT, EF model, connection pool), so it is excluded from the median.
param(
    [string]$Url = "http://localhost:5177/api/flights",
    [int]$Count = 5
)

Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromMinutes(2)

$durations = @()

for ($i = 1; $i -le $Count; $i++) {
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = $client.GetAsync($Url).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
    }
    catch {
        Write-Host "Request to $Url failed. Is the API running?" -ForegroundColor Red
        exit 1
    }
    $watch.Stop()

    $ms = $watch.ElapsedMilliseconds
    $mb = [math]::Round($body.Length / 1MB, 2)
    $note = if ($i -eq 1) { "(cold, ignored)" } else { "" }
    Write-Host ("Run {0}: {1} {2} ms, {3} MB {4}" -f $i, [int]$response.StatusCode, $ms, $mb, $note)

    if ($i -gt 1) { $durations += $ms }
}

$client.Dispose()

if ($durations.Count -gt 0) {
    $sorted = $durations | Sort-Object
    $middle = [math]::Floor($sorted.Count / 2)
    $median = if ($sorted.Count % 2 -eq 0) { ($sorted[$middle - 1] + $sorted[$middle]) / 2 } else { $sorted[$middle] }
    Write-Host ("Median of runs 2..{0}: {1} ms" -f $Count, $median)
}
