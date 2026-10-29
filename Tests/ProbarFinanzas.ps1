param([string]$Conexion, [switch]$SoloCaptura)
$ErrorActionPreference = 'Stop'
if (-not $SoloCaptura -and $Conexion -notmatch 'Database=capitalwest_pruebas_finanzas(?:;|$)') { throw 'Este script exige la base aislada capitalwest_pruebas_finanzas.' }
$raizFinanzas = Split-Path -Parent $PSScriptRoot
$dllFinanzas = Join-Path $raizFinanzas 'SistemaFinanciera/bin'
$salidaFinanzas = Join-Path $PSScriptRoot 'obj/FinanzasTests'
New-Item -ItemType Directory -Path $salidaFinanzas -Force | Out-Null
Get-ChildItem -LiteralPath $dllFinanzas -Filter '*.dll' | Copy-Item -Destination $salidaFinanzas -Force
$refsFinanzas = @(Get-ChildItem -LiteralPath $salidaFinanzas -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName })
$refsFinanzas += '/reference:System.Data.dll','/reference:System.Core.dll','/reference:System.Transactions.dll','/reference:System.Net.Http.dll'
$refsFinanzas += '/reference:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\netstandard.dll'
$ensambladoFinanzas = Join-Path $salidaFinanzas 'PruebasFinanzas.dll'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:library "/out:$ensambladoFinanzas" @refsFinanzas (Join-Path $PSScriptRoot 'FinanzasTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron compilar las pruebas financieras.' }
[xml]$webFinanzas = Get-Content -LiteralPath (Join-Path $raizFinanzas 'SistemaFinanciera/Web.config') -Raw
[IO.File]::WriteAllText(($ensambladoFinanzas+'.config'), ('<configuration>'+$webFinanzas.configuration.runtime.OuterXml+'</configuration>'))
$entornoFinanzas = [AppDomainSetup]::new()
$entornoFinanzas.ApplicationBase = $salidaFinanzas
$entornoFinanzas.ConfigurationFile = $ensambladoFinanzas+'.config'
$dominioFinanzas = [AppDomain]::CreateDomain('FinanzasPruebas', $null, $entornoFinanzas)
try {
    $runnerFinanzas = $dominioFinanzas.CreateInstanceFromAndUnwrap($ensambladoFinanzas,'FinanzasTests')
    $tipoFinanzas = [Reflection.Assembly]::LoadFrom($ensambladoFinanzas).GetType('FinanzasTests')
    if ($SoloCaptura) { $tipoFinanzas.GetMethod('RunCaptura').Invoke($runnerFinanzas, @()) }
    else { $tipoFinanzas.GetMethod('Run').Invoke($runnerFinanzas, [object[]]@($Conexion)) }
} finally { [AppDomain]::Unload($dominioFinanzas) }
