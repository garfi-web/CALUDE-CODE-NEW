<#
.SYNOPSIS
    Sauvegarde locale de RED MET : copie ce qui n'est pas sur GitHub vers un disque externe ou OneDrive.

.DESCRIPTION
    Copie dans <Destination>\REDMET_backup :
      - C:\Users\Home\.claude.json       (configuration de Claude Code, dont les serveurs MCP)
      - C:\Users\Home\.claude            (historique des conversations, réglages, mémoire)
      - avec -WithModels : C:\RED MET\ai (TripoSG, ComfyUI, poids et environnements, environ 23 Go)

    Ne copie jamais les dossiers Library ni Temp, ni les fichiers .blend1 (copies de secours de Blender).
    Par défaut, .claude\.credentials.json (jeton de connexion) n'est PAS copié : il donnerait accès au
    compte à quiconque lit la sauvegarde, et il suffit de se reconnecter après une restauration.

    La copie est incrémentale (robocopy) : relancer le script ne recopie que ce qui a changé.
    Rien n'est supprimé dans la destination, sauf avec -Mirror.

.PARAMETER Destination
    Dossier de destination, par exemple E:\ (disque externe) ou "$env:OneDrive\Sauvegardes".

.PARAMETER WithModels
    Ajoute C:\RED MET\ai (environ 23 Go). Vérifiez l'espace libre, surtout sur OneDrive.

.PARAMETER IncludeCredentials
    Copie aussi .claude\.credentials.json. Déconseillé, surtout vers un dossier synchronisé dans le cloud.

.PARAMETER Mirror
    Fait de la destination un miroir exact : les fichiers supprimés à la source le sont aussi dans la sauvegarde.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\backup_local.ps1 -Destination E:\

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\backup_local.ps1 -Destination "$env:OneDrive\Sauvegardes" -WithModels
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [switch]$WithModels,
    [switch]$IncludeCredentials,
    [switch]$Mirror
)

$ErrorActionPreference = 'Stop'

$ClaudeJson = Join-Path $HOME '.claude.json'
$ClaudeDir = Join-Path $HOME '.claude'
$ModelsDir = 'C:\RED MET\ai'

if (-not (Test-Path $Destination)) {
    throw "Destination introuvable : $Destination (branchez le disque ou créez le dossier)."
}

$Root = Join-Path $Destination 'REDMET_backup'
New-Item -ItemType Directory -Force -Path $Root | Out-Null

# Exclusions communes : jamais Library, Temp ni les .blend1
$ExcludeDirs = @('Library', 'Temp')
$ExcludeFiles = @('*.blend1')

function Invoke-Copy {
    param([string]$Source, [string]$Target, [string[]]$ExtraExcludeFiles = @())

    $mode = if ($Mirror) { '/MIR' } else { '/E' }
    $arguments = @($Source, $Target, $mode, '/R:2', '/W:2', '/NP', '/NDL', '/NFL',
        '/XD') + $ExcludeDirs + @('/XF') + $ExcludeFiles + $ExtraExcludeFiles

    Write-Host "-> $Source" -ForegroundColor Cyan
    & robocopy @arguments | Select-Object -Last 12 | ForEach-Object { Write-Host "   $_" }

    # robocopy : 0 à 7 = succès (fichiers copiés, ignorés ou déjà à jour), 8 et plus = erreur
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy a échoué sur $Source (code $LASTEXITCODE)."
    }
}

# Estimation de l'espace nécessaire avant de commencer
$sources = @($ClaudeDir) + $(if ($WithModels) { @($ModelsDir) } else { @() })
$needed = (Get-ChildItem $sources -Recurse -Force -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -ne '.blend1' } | Measure-Object Length -Sum).Sum
$drive = (Get-Item $Root).PSDrive
if ($drive -and $drive.Free -lt $needed) {
    throw ("Espace insuffisant sur {0}: : {1:N1} Go nécessaires, {2:N1} Go libres." -f $drive.Name, ($needed / 1GB), ($drive.Free / 1GB))
}
Write-Host ("Sauvegarde vers {0} (environ {1:N1} Go à vérifier)" -f $Root, ($needed / 1GB)) -ForegroundColor Green

# 1. Configuration de Claude Code
Copy-Item $ClaudeJson (Join-Path $Root '.claude.json') -Force
Write-Host "-> $ClaudeJson" -ForegroundColor Cyan

# 2. Historique des conversations et réglages
$credentials = if ($IncludeCredentials) { @() } else { @('.credentials.json') }
if ($IncludeCredentials) {
    Write-Warning "Le jeton de connexion (.credentials.json) est inclus : protégez cette sauvegarde."
}
Invoke-Copy -Source $ClaudeDir -Target (Join-Path $Root '.claude') -ExtraExcludeFiles $credentials

# 3. Modèles et environnements (optionnel)
if ($WithModels) {
    if (Test-Path $ModelsDir) {
        Invoke-Copy -Source $ModelsDir -Target (Join-Path $Root 'RED MET_ai')
    }
    else {
        Write-Warning "$ModelsDir introuvable : modèles ignorés."
    }
}

# Trace de la sauvegarde, pour savoir ce qu'elle contient et quand elle a été faite
@(
    "Sauvegarde RED MET du $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    "Machine : $env:COMPUTERNAME"
    "Contenu : .claude.json, .claude$(if ($WithModels) { ', RED MET\ai' })"
    "Jeton de connexion inclus : $([bool]$IncludeCredentials)"
    "Mode : $(if ($Mirror) { 'miroir' } else { 'incrémental' })"
    ""
    "Restauration : copier .claude.json et .claude dans $HOME, et RED MET_ai vers C:\RED MET\ai."
    "Puis lancer Claude Code et se reconnecter si le jeton n'était pas inclus."
) | Set-Content -Path (Join-Path $Root 'SAUVEGARDE.txt') -Encoding utf8

Write-Host "Terminé : $Root" -ForegroundColor Green
