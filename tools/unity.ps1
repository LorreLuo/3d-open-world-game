param([Parameter(Mandatory=$true)][ValidateSet("Compile","TestPlay","Build","VerifyTags")][string]$Action)
$ErrorActionPreference = "Stop"
$proj = Split-Path -Parent $PSScriptRoot
$unity = $env:UNITY_PATH
if (-not $unity) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
        "C:\Program Files\Unity 6000.4.5f1\Editor\Unity.exe"
    )
    $unity = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $unity) {
        $found = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Filter "Unity.exe" -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { $unity = $found.FullName }
    }
}
if (-not $unity -or -not (Test-Path $unity)) { throw "未找到 Unity.exe，请设置 UNITY_PATH 环境变量" }
$log = Join-Path $env:TEMP ("unity-{0}-{1}.log" -f $Action, (Get-Date -Format "yyyyMMddHHmmss"))
$base = @("-batchmode","-nographics","-projectPath",$proj,"-logFile",$log,"-quit")
switch ($Action) {
    "Compile"    { & $unity @base "-executeMethod" "Game.Editor.ProjectVerifier.CompileCheck" | Out-Null }
    "VerifyTags" { & $unity @base "-executeMethod" "Game.Editor.TagManagerUpdater.Setup" | Out-Null }
    "TestPlay"   { & $unity "-batchmode","-nographics","-projectPath",$proj,"-logFile",$log,"-runTests","-testPlatform","PlayMode","-testResults",(Join-Path $proj "TestResults/playmode.xml") | Out-Null }
    "Build"      { & $unity @base "-executeMethod" "Game.Editor.ProjectVerifier.BuildWindows" | Out-Null }
}
$exit = $LASTEXITCODE
$content = if (Test-Path $log) { Get-Content $log -Raw } else { "" }
if ($Action -eq "Compile" -and $content -notmatch "COMPILE_CHECK_DONE") { throw "编译检查未完成（存在编译错误或方法未执行）。日志: $log" }
if ($content -match "error CS") { throw "存在编译错误。日志: $log" }
if ($Action -eq "Build") {
    if ($content -notmatch "BUILD_RESULT:") { throw "构建结果未输出。日志: $log" }
    if ($content -notmatch "BUILD_RESULT: Succeeded") { throw "构建失败。日志: $log" }
}
if ($exit -ne 0) { throw "Unity 退出码 $exit。日志: $log" }
Write-Host "OK: $Action 通过 ($log)"
