<#!
.SYNOPSIS
  自動版本遞增、打包並上傳 NuGet 套件的工具腳本。

.DESCRIPTION
  針對使用 .nuspec 手動管理版本的情境：
    1. 從指定 .nuspec 讀取目前版本
    2. 依參數 --Bump (Patch/Minor/Major) 或 --Version 指定新版本
    3. 更新 .nuspec 中的 <version>
    4. dotnet clean / build -c Release
    5. nuget pack 產生 .nupkg (必要時也可加 -Symbols)
    6. 讀取 apikey.txt (或 --ApiKey 覆寫)
    7. 若新版本尚未存在於 nuget.org 則推送

.PARAMETER Nuspec
  指定 .nuspec 檔案 (預設: EzLib.nuspec)

.PARAMETER Bump
  版本遞增類型：Patch (預設) | Minor | Major

.PARAMETER Version
  明確指定新版本 (指定此參數時忽略 Bump)

.PARAMETER PreRelease
  指定預發版標籤 (例如: beta / rc1)；會附加於版本號後 (e.g. 1.2.3-beta)

.PARAMETER ApiKey
  NuGet API Key，若未指定則讀取 apikey.txt

.PARAMETER Source
  NuGet 發布來源 (預設: https://api.nuget.org/v3/index.json)

.PARAMETER Output
  輸出資料夾 (預設: nupkgs)

.PARAMETER Symbols
  產生符號包 (.snupkg)

.EXAMPLE
  ./Publish-NuGet.ps1

.EXAMPLE
  ./Publish-NuGet.ps1 -Bump Minor -PreRelease beta

.EXAMPLE
  ./Publish-NuGet.ps1 -Version 1.2.0

.NOTES
  需安裝 nuget.exe 或已在 PATH；也可改用 dotnet nuget push。
!#>
[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [string]$Nuspec = 'EzLib.nuspec',
    [ValidateSet('Patch','Minor','Major')] [string]$Bump = 'Patch',
    [string]$Version,
    [string]$PreRelease,
    [string]$ApiKey,
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [string]$Output = 'nupkgs',
    [switch]$Symbols
)

function Write-Info($m){ Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Write-Step($m){ Write-Host "[STEP] $m" -ForegroundColor Green }
function Write-Warn($m){ Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Write-Err ($m){ Write-Host "[FAIL] $m" -ForegroundColor Red }

$whatIfPreference = $PSCmdlet.WhatIfPreference

if(-not (Test-Path $Nuspec)){ Write-Err "Nuspec 檔案不存在: $Nuspec"; exit 1 }

[xml]$xml = Get-Content $Nuspec -Raw -Encoding UTF8
$currentVersion = $xml.package.metadata.version
if([string]::IsNullOrWhiteSpace($currentVersion)){ Write-Err '無法讀取現有版本號'; exit 1 }
Write-Info "目前版本: $currentVersion"

$newVersion = $Version
if(-not $newVersion){
  $parts = $currentVersion -split '-'; $core = $parts[0]
  $seg = $core.Split('.')
  while($seg.Count -lt 3){ $seg += '0' }
  [int]$major = $seg[0]; [int]$minor = $seg[1]; [int]$patch = $seg[2]
  switch($Bump){
    'Major' { $major++; $minor=0; $patch=0 }
    'Minor' { $minor++; $patch=0 }
    'Patch' { $patch++ }
  }
  $newVersion = "$major.$minor.$patch"
}
if($PreRelease){ $newVersion = "$newVersion-$PreRelease" }

if($newVersion -eq $currentVersion){ Write-Warn "新舊版本相同: $newVersion" }
Write-Step "新版本: $newVersion"

# 檢查 nuget.org 是否已存在
$packageId = $xml.package.metadata.id
$indexUrl = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLower())/index.json"
$exists = $false
try {
  $resp = Invoke-RestMethod -Uri $indexUrl -Method GET -ErrorAction Stop
  if($resp.versions -contains $newVersion){ $exists = $true }
} catch { }
if($exists){ Write-Err "版本 $newVersion 已存在於 nuget.org，停止。"; exit 1 }

if($whatIfPreference){ Write-Warn 'WhatIf 模式：不進行實際修改'; }

# 更新 nuspec 版本
if(-not $whatIfPreference){
  $xml.package.metadata.version = $newVersion
  $resolvedPath = (Resolve-Path $Nuspec).Path
  $writer = [System.IO.StreamWriter]::new($resolvedPath, $false, [System.Text.UTF8Encoding]::new($false))
  $xml.Save($writer)
  $writer.Close()
  Write-Step "已更新 $Nuspec 版本為 $newVersion"
}

# 清理與建置
Write-Step 'dotnet clean'
if(-not $whatIfPreference){ dotnet clean | Out-Null }
Write-Step 'dotnet build -c Release'
if(-not $whatIfPreference){ dotnet build -c Release | Out-Null }

# 打包
if(-not (Test-Path $Output)){ New-Item -ItemType Directory -Path $Output | Out-Null }
$packArgs = @('pack', $Nuspec, '-OutputDirectory', $Output)
if($Symbols){ $packArgs += '-Symbols'; $packArgs += '-SymbolPackageFormat'; $packArgs += 'snupkg' }
Write-Step "nuget $($packArgs -join ' ')"
$pkgFile = "$Output/$packageId.$newVersion.nupkg"
$snupkgFile = "$Output/$packageId.$newVersion.snupkg"
if(-not $whatIfPreference){
  nuget @packArgs | Out-Null
  if(-not (Test-Path $pkgFile)){ Write-Err "打包失敗，找不到: $pkgFile"; exit 1 }
}

# 取得 API Key（優先順序：-ApiKey 參數 > NUGET_API_KEY 環境變數 > apikey.txt）
if(-not $ApiKey){
  if($env:NUGET_API_KEY)         { $ApiKey = $env:NUGET_API_KEY }
  elseif(Test-Path 'apikey.txt') { $ApiKey = (Get-Content 'apikey.txt' -Raw).Trim() }
}
if(-not $ApiKey){ Write-Err '缺少 ApiKey (參數、NUGET_API_KEY 環境變數或 apikey.txt)'; exit 1 }

# 推送
Write-Step "推送套件 -> $Source"
if(-not $whatIfPreference){
  nuget push $pkgFile -ApiKey $ApiKey -Source $Source -NonInteractive
  if($Symbols -and (Test-Path $snupkgFile)){
    nuget push $snupkgFile -ApiKey $ApiKey -Source $Source -NonInteractive
  }
}

Write-Info "完成。產生版本: $newVersion"
