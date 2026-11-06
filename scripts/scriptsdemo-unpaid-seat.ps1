param(
    [string]$Api = "https://localhost:7179",
    [int]$WaitSeconds = 30
)

function Send($method, $path, $body) {
    $json = if ($body) { $body | ConvertTo-Json } else { $null }
    try {
        $response = Invoke-WebRequest -Method $method -Uri "$Api$path" -Body $json -ContentType "application/json" -UseBasicParsing
        [pscustomobject]@{ Code = [int]$response.StatusCode; Data = $response.Content | ConvertFrom-Json }
    }
    catch {
        [pscustomobject]@{ Code = [int]$_.Exception.Response.StatusCode; Data = $null }
    }
}

function NewPassenger($id) {
    (Send POST "/api/passengers" @{ firstName = "Demo"; lastName = $id; email = "$id@demo.com"; passportNumber = "U1234567" }).Data
}

$stamp = Get-Random
$departure = (Get-Date).ToUniversalTime().AddDays(60 + ($stamp % 200))
$flight = (Send POST "/api/flights" @{
    flightNumber = "DM$($stamp % 9999)"; origin = "IST"; destination = "AYT"
    departureTime = $departure.ToString("o"); arrivalTime = $departure.AddHours(1).ToString("o")
    totalSeats = 1; price = 900
}).Data
$first = NewPassenger "a$stamp"
$second = NewPassenger "b$stamp"

$booking = Send POST "/api/reservations" @{ flightId = $flight.id; passengerId = $first.id }
Write-Host "Passenger A books (never pays): $($booking.Code)"
Write-Host "Passenger B books:              $((Send POST '/api/reservations' @{ flightId = $flight.id; passengerId = $second.id }).Code)"

Write-Host "Waiting $WaitSeconds s..."
Start-Sleep -Seconds $WaitSeconds

Write-Host "Seats left:                     $((Send GET "/api/flights/$($flight.id)").Data.availableSeats)"
Write-Host "Passenger B books again:        $((Send POST '/api/reservations' @{ flightId = $flight.id; passengerId = $second.id }).Code)"