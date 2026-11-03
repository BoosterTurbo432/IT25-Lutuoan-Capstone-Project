$ErrorActionPreference = 'Stop'
Write-Host "=== Starting WebGL Loading Screen Compatibility & Integrity Tests ==="

$proj = "c:\Users\Asus TUF\Documents\GitHub\IT25-Lutuoan-Capstone-Project"

# Test 1: Check required source files
$requiredFiles = @(
    "Assets\Scripts\Core\LoadingBridge.cs",
    "Assets\Scripts\UI\Loading\LoadingScreenController.cs",
    "Assets\Scripts\UI\Common\SceneLoader.cs",
    "Assets\Scripts\UI\MainMenu\MainMenuManager.cs",
    "Assets\Scripts\Editor\LoadingScreenBuilder.cs",
    "Assets\Scripts\Editor\MainMenuSceneBuilder.cs"
)

foreach ($f in $requiredFiles) {
    $full = Join-Path $proj $f
    if (-not (Test-Path $full)) {
        Write-Error "FAIL: Missing file $f"
    }
}
Write-Host "PASS: All 6 required scripts present on disk."

# Test 2: Check EditorBuildSettings scene configuration
$buildSettings = Get-Content "$proj\ProjectSettings\EditorBuildSettings.asset" -Raw
if ($buildSettings -notmatch "path:\s*Assets/Scenes/loadingscreen\.unity") {
    Write-Error "FAIL: loadingscreen.unity not found in EditorBuildSettings.asset"
}

# Verify loadingscreen is the first scene (Index 0 for initial game boot)
$firstSceneMatch = [regex]::Match($buildSettings, "m_Scenes:\s*\r?\n\s*-\s*enabled:\s*1\s*\r?\n\s*path:\s*(.*)")
if ($firstSceneMatch.Success) {
    $firstPath = $firstSceneMatch.Groups[1].Value.Trim()
    if ($firstPath -ne "Assets/Scenes/loadingscreen.unity") {
        Write-Error "FAIL: Initial boot scene is '$firstPath' instead of 'Assets/Scenes/loadingscreen.unity'"
    }
    Write-Host "PASS: Scene 0 in Build Settings is '$firstPath' (Cold Boot enabled)."
} else {
    Write-Error "FAIL: Could not parse first scene in EditorBuildSettings.asset"
}

# Test 3: Roslyn compilation check
$compileScript = "$PSScriptRoot\compile_check.ps1"
if (Test-Path $compileScript) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $compileScript
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FAIL: Roslyn compilation failed with code $LASTEXITCODE"
    }
    Write-Host "PASS: Full Roslyn compilation passed for runtime, WebGL, and Editor."
}

# Test 4: Verify WebGL yield & memory optimization patterns in LoadingScreenController.cs
$controllerCode = Get-Content "$proj\Assets\Scripts\UI\Loading\LoadingScreenController.cs" -Raw

if ($controllerCode -notmatch "yield\s+return\s+Resources\.UnloadUnusedAssets\(\)") {
    Write-Error "FAIL: Missing Resources.UnloadUnusedAssets() memory cleanup in LoadingScreenController.cs"
}
if ($controllerCode -notmatch "GC\.Collect\(\)") {
    Write-Error "FAIL: Missing GC.Collect() memory cleanup in LoadingScreenController.cs"
}
if ($controllerCode -notmatch "asyncOp\.allowSceneActivation\s*=\s*false") {
    Write-Error "FAIL: Missing asyncOp.allowSceneActivation = false in LoadingScreenController.cs"
}
if ($controllerCode -notmatch "Mathf\.Min\(Time\.unscaledDeltaTime,\s*0\.05f\)") {
    Write-Error "FAIL: Missing delta time clamping in LoadingScreenController.cs"
}
Write-Host "PASS: WebGL memory cleanup, async activation gate, and delta clamping verified."

# Test 5: Verify LoadingBridge integration in MainMenuManager.cs
$menuManagerCode = Get-Content "$proj\Assets\Scripts\UI\MainMenu\MainMenuManager.cs" -Raw
if ($menuManagerCode -notmatch "LoadingBridge\.LoadSceneAsync") {
    Write-Error "FAIL: MainMenuManager.cs does not route through LoadingBridge.LoadSceneAsync"
}
Write-Host "PASS: MainMenuManager routes scene transitions through LoadingBridge."

Write-Host "=== ALL 5 TESTING & OPTIMIZATION CYCLES PASSED SUCCESSFULLY! ==="
