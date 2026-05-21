param (
    [string]$Email = "dr.smith@sympnet.com",
    [string]$Password = "Doctor123!",
    [string]$FullName = "Dr. Smith"
)

$body = @{
    Email = $Email
    Password = $Password
    Role = "Doctor"
    FullName = $FullName
} | ConvertTo-Json

Write-Host "Registering Doctor: $Email" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5057/api/auth/register" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Doctor account created successfully!" -ForegroundColor Green
    $response | Format-List
} catch {
    Write-Host "Error creating doctor account:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
