# Script de génération du diagramme Use Case SympNet via l'API Kroki
$pumlPath = "c:\Users\Yasmine\Documents\Symp\sympnet-web-service\usecase.puml"
$pngPath = "c:\Users\Yasmine\Documents\Symp\sympnet-web-service\usecase.png"

if (Test-Path $pumlPath) {
    Write-Host "Lecture du fichier $pumlPath..."
    $pumlContent = [System.IO.File]::ReadAllText($pumlPath)
    
    $bodyObj = @{
        diagram_source = $pumlContent
    }
    $body = $bodyObj | ConvertTo-Json -Compress -Depth 10
    
    Write-Host "Envoi de la requête de rendu à Kroki..."
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    try {
        Invoke-WebRequest -Uri "https://kroki.io/plantuml/png" -Method Post -Body $body -Headers $headers -OutFile $pngPath
        Write-Host "Image générée avec succès dans : $pngPath"
    } catch {
        Write-Error "Erreur lors de la requête de rendu : $_"
    }
} else {
    Write-Error "Le fichier d'entrée $pumlPath n'a pas été trouvé."
}
