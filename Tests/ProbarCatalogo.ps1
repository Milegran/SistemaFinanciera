$ErrorActionPreference='Stop'
$raiz=Split-Path -Parent $PSScriptRoot
$salida=Join-Path $PSScriptRoot 'obj/CatalogoTests'
New-Item -ItemType Directory -Path $salida -Force | Out-Null
Get-ChildItem "$raiz\SistemaFinanciera\bin" -Filter '*.dll' | Copy-Item -Destination $salida -Force
$refs=@(Get-ChildItem $salida -Filter '*.dll' | ForEach-Object {'/reference:'+ $_.FullName})
$refs+='/reference:System.Data.dll','/reference:System.Core.dll','/reference:System.Web.dll','/reference:C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\netstandard.dll'
$dll=Join-Path $salida 'CatalogoTests.dll'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:library "/out:$dll" @refs (Join-Path $PSScriptRoot 'CatalogoTests.cs')
if($LASTEXITCODE -ne 0){throw 'No compilaron las pruebas'}
[xml]$config=Get-Content "$raiz\SistemaFinanciera\Web.config" -Raw
[IO.File]::WriteAllText(($dll+'.config'),'<configuration>'+$config.configuration.runtime.OuterXml+'</configuration>')
$setup=[AppDomainSetup]::new();$setup.ApplicationBase=$salida;$setup.ConfigurationFile=$dll+'.config'
$dominio=[AppDomain]::CreateDomain('CatalogoPruebas',$null,$setup)
try {
 $runner=$dominio.CreateInstanceFromAndUnwrap($dll,'CatalogoTests')
 $tipo=[Reflection.Assembly]::LoadFrom($dll).GetType('CatalogoTests')
 $conexion=($config.configuration.connectionStrings.add | Where-Object name -eq 'SistemaFinanciera').connectionString
 $tipo.GetMethod('Run').Invoke($runner,[object[]]@($conexion))
} catch {Write-Output $_.Exception.ToString();exit 1} finally {[AppDomain]::Unload($dominio)}
