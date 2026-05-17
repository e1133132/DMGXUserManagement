#Requires -RunAsAdministrator
param(
    [string]$SiteName = $env:IIS_SITE_NAME,
    [string]$AppPoolName = $env:IIS_APP_POOL,
    [string]$PhysicalPath = $env:IIS_PHYSICAL_PATH,
    [string]$ConnectionString = $env:CONNECTION_STRING,
    [string]$PublishSource = (Join-Path $PSScriptRoot "..\publish")
)

$ErrorActionPreference = "Stop"

Import-Module WebAdministration

if ([string]::IsNullOrWhiteSpace($SiteName)) { $SiteName = "UserManagementApi" }
if ([string]::IsNullOrWhiteSpace($AppPoolName)) { $AppPoolName = "UserManagementApiPool" }
if ([string]::IsNullOrWhiteSpace($PhysicalPath)) { $PhysicalPath = "C:\inetpub\wwwroot\UserManagementApi" }

Write-Host "Deploying to IIS site '$SiteName' at '$PhysicalPath'..."

if (-not (Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
}

if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
}

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName -Port 8080 | Out-Null
}

Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Get-ChildItem -Path $PhysicalPath -Recurse -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $PublishSource "*") -Destination $PhysicalPath -Recurse -Force

$prodSettings = Join-Path $PhysicalPath "appsettings.Production.json"
if ($ConnectionString -and (Test-Path $prodSettings)) {
    $json = Get-Content $prodSettings -Raw | ConvertFrom-Json
    $json.ConnectionStrings.DefaultConnection = $ConnectionString
    $json | ConvertTo-Json -Depth 10 | Set-Content $prodSettings -Encoding UTF8
}

if (-not (Test-Path (Join-Path $PhysicalPath "web.config"))) {
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\UserManagement.Api.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
"@ | Set-Content (Join-Path $PhysicalPath "web.config") -Encoding UTF8
}

$logsDir = Join-Path $PhysicalPath "logs"
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }

Start-WebAppPool -Name $AppPoolName
Write-Host "Deployment completed. Site: http://localhost:8080 (default port if newly created)"
