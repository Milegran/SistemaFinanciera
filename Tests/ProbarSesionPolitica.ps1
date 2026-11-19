param([Parameter(Mandatory=$true)][string]$Conexion)
$ErrorActionPreference='Stop'
if ($Conexion -notmatch 'Database=capitalwest_pruebas_sesion(?:;|$)') { throw 'Solo admite la base aislada capitalwest_pruebas_sesion.' }
$raizSesion=Split-Path -Parent $PSScriptRoot
$salidaSesion=Join-Path $PSScriptRoot 'obj/SesionPoliticaTests'
New-Item -ItemType Directory -Path $salidaSesion -Force | Out-Null
Get-ChildItem "$raizSesion/SistemaFinanciera/bin" -Filter '*.dll' | Copy-Item -Destination $salidaSesion -Force
$refsSesion=@(Get-ChildItem $salidaSesion -Filter '*.dll' | ForEach-Object {'/reference:'+ $_.FullName})
$refsSesion+='/reference:System.Data.dll','/reference:System.Core.dll','/reference:System.Web.dll','/reference:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\netstandard.dll'
$dllSesion=Join-Path $salidaSesion 'SesionPoliticaTests.dll'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:library "/out:$dllSesion" @refsSesion (Join-Path $PSScriptRoot 'SesionPoliticaTests.cs')
if($LASTEXITCODE -ne 0){throw 'No compilaron las pruebas'}
[xml]$configSesion=Get-Content "$raizSesion/SistemaFinanciera/Web.config" -Raw
[IO.File]::WriteAllText(($dllSesion+'.config'),'<configuration>'+$configSesion.configuration.runtime.OuterXml+'</configuration>')
$setupSesion=New-Object AppDomainSetup
$setupSesion.ApplicationBase=$salidaSesion
$setupSesion.ConfigurationFile=$dllSesion+'.config'
$dominioSesion=[AppDomain]::CreateDomain('SesionPoliticaPruebas',$null,$setupSesion)
try {
 $runnerSesion=$dominioSesion.CreateInstanceFromAndUnwrap($dllSesion,'SesionPoliticaTests')
 $tipoSesion=[Reflection.Assembly]::LoadFrom($dllSesion).GetType('SesionPoliticaTests')
 $tipoSesion.GetMethod('Run').Invoke($runnerSesion,[object[]]@($Conexion))
} finally { [AppDomain]::Unload($dominioSesion) }
