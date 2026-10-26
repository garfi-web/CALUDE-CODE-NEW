# reprendre.ps1 - Relance l'environnement de travail RED MET
$ProjectPath  = "C:\Users\Home\Unity user templates\REDMET"
$UnityVersion = "6000.6.6f1"
$UnityExe     = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$BlenderExe   = Get-ChildItem "C:\Program Files\Blender Foundation" -Recurse -Filter blender.exe -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName

Write-Host "=== RED MET : reprise du travail ===" -ForegroundColor Red

# 1. Dossier du projet
if (-not (Test-Path $ProjectPath)) {
    Write-Host "Projet introuvable : $ProjectPath" -ForegroundColor Yellow
    Read-Host "Appuie sur Entree pour quitter"; exit 1
}
Set-Location $ProjectPath

# 2. Derniere version depuis GitHub
Write-Host "`n[1/4] Mise a jour depuis GitHub..." -ForegroundColor Cyan
git fetch origin
if (git status --porcelain) {
    Write-Host "Des modifications locales ne sont pas encore sauvegardees : pas de mise a jour automatique." -ForegroundColor Yellow
} else {
    git pull --ff-only
}

# 3. Unity : un seul editeur, celui de REDMET
Write-Host "`n[2/4] Unity..." -ForegroundColor Cyan
$unityWindows = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle }
$others = $unityWindows | Where-Object { $_.MainWindowTitle -notmatch "REDMET" }
if ($others) {
    Write-Host "ATTENTION : un autre projet Unity est ouvert :" -ForegroundColor Yellow
    $others | ForEach-Object { Write-Host "  - $($_.MainWindowTitle)" }
    Read-Host "Ferme-le (File > Exit), puis appuie sur Entree"
}
if ($unityWindows | Where-Object { $_.MainWindowTitle -match "REDMET" }) {
    Write-Host "Unity REDMET est deja ouvert."
} elseif (Test-Path $UnityExe) {
    Start-Process $UnityExe -ArgumentList "-projectPath `"$ProjectPath`""
    Write-Host "Ouverture de Unity REDMET (attends qu'il soit completement charge)."
} else {
    Write-Host "Unity $UnityVersion introuvable : ouvre REDMET depuis Unity Hub." -ForegroundColor Yellow
}

# 4. Blender
Write-Host "`n[3/4] Blender..." -ForegroundColor Cyan
if (Get-Process blender -ErrorAction SilentlyContinue) {
    Write-Host "Blender est deja ouvert."
} elseif ($BlenderExe) {
    Start-Process $BlenderExe
    Write-Host "Ouverture de Blender."
} else {
    Write-Host "Blender introuvable : ouvre-le manuellement." -ForegroundColor Yellow
}
Write-Host "Dans Blender : vue 3D > touche N > onglet MCP for Blender > Connect to MCP server." -ForegroundColor Yellow

# 5. Phrase de reprise dans le presse-papiers
$prompt = "Lis docs/REPRISE.md et docs/PRODUCTION.md, verifie avec /mcp que Unity et Blender sont connectes, rappelle-moi ou on en est, puis reprends la phase en cours la ou on s'est arretes."
Set-Clipboard -Value $prompt
Write-Host "`n[4/4] La phrase de reprise est copiee : fais Ctrl+V dans Claude puis Entree." -ForegroundColor Green
Read-Host "Quand Unity est charge et Blender connecte, appuie sur Entree pour lancer Claude"

# 6. Claude, derniere conversation
claude --continue