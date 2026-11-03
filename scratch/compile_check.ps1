$ErrorActionPreference = 'Stop'
$editor = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor"
$dotnet = "$editor\Data\DotNetSdk\dotnet.exe"
$csc = "$editor\Data\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll"
$proj = "c:\Users\Asus TUF\Documents\GitHub\IT25-Lutuoan-Capstone-Project"
$sa = "$proj\Library\ScriptAssemblies"

$common = @()
$common += Get-ChildItem "$editor\Data\Managed\UnityEngine\*.dll" | ForEach-Object { $_.FullName }
$common += "$editor\Data\NetStandard\ref\2.1.0\netstandard.dll"
$common += "$sa\UnityEngine.UI.dll", "$sa\Unity.InputSystem.dll", "$sa\Unity.TextMeshPro.dll"

$runtimeSrc = Get-ChildItem "$proj\Assets\Scripts\UI", "$proj\Assets\Scripts\Core" -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
$editorSrc = Get-ChildItem "$proj\Assets\Scripts\Editor" -Recurse -Filter *.cs | ForEach-Object { $_.FullName }

function Compile($name, $defines, $sources, $extraRefs) {
  $out = Join-Path $env:TEMP "$name.dll"
  $a = @($csc, "-nologo", "-target:library", "-nostdlib", "-langversion:9.0", "-warn:4", "-nowarn:0649",
         "-define:$defines", "-out:$out")
  $a += ($common + $extraRefs) | ForEach-Object { "-r:$_" }
  $a += $sources
  & $dotnet @a
  "$name EXIT: $LASTEXITCODE"
  return $out
}

$rt = Compile "rt_editor" "ENABLE_INPUT_SYSTEM;UNITY_EDITOR" $runtimeSrc @()
Compile "rt_player" "ENABLE_INPUT_SYSTEM" $runtimeSrc @() | Out-Null
Compile "rt_webgl_legacy" "UNITY_WEBGL" $runtimeSrc @() | Select-Object -Last 1
Compile "editor_tools" "ENABLE_INPUT_SYSTEM;UNITY_EDITOR" $editorSrc @($rt) | Select-Object -Last 1
