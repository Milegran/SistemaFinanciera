param([string]$Conexion = 'Server=127.0.0.1;Port=3306;Database=capitalwest;User ID=root;Password=;Character Set=utf8mb4')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $raiz 'SistemaFinanciera\bin'
# Se usa un dominio .NET Framework con binding redirects, igual que la aplicación.
$salida = Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Path $salida -Force | Out-Null
Get-ChildItem -LiteralPath $dll -Filter '*.dll' | Copy-Item -Destination $salida -Force
$refs = @((Join-Path $dll 'SistemaFinanciera.Entities.dll'), (Join-Path $dll 'SistemaFinanciera.BL.dll'), (Join-Path $dll 'SistemaFinanciera.DAL.dll'))
$codigo = @'
using System;
using System.Linq;
using System.Transactions;
using SistemaFinanciera.Entities;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
public class PruebasClientes : MarshalByRefObject {
    public void Run(string conexion) { try { Ejecutar(conexion); } catch(Exception ex) { throw new Exception(ex.ToString()); } }
    static int casos;
    static void Check(bool condicion, string nombre) {
        if (!condicion) throw new Exception("FALLO: " + nombre);
        casos++; Console.WriteLine("OK: " + nombre);
    }
    static Cliente ClienteValido() {
        return new Cliente {TipoPersona="NATURAL",TipoIdentificacion="CEDULA",Identificacion="8-123-456",Nombres="Ana",Apellidos="Prueba",FechaNacimiento=new DateTime(1990,5,1),Sexo="F",EstadoCivil="SOLTERO",Telefono="+507 6000-0000",Correo="ana@example.com",Direccion="Calle de prueba",CodigoProvincia="08",CodigoDistrito="0801",CodigoCorregimiento="080105",Provincia="PANAMA",Distrito="PANAMÁ",Corregimiento="BELLA VISTA",Ocupacion="Analista",Ingresos=1250.50m,Referencias="Persona de referencia, 6000-0001, familiar"};
    }
    static void Invalido(Action<Cliente> cambio, string nombre) {
        var c=ClienteValido(); cambio(c); Check(ClienteServicio.Validar(c).Count>0,nombre);
    }
    static bool Falla(Action accion) {
        try { accion(); return false; } catch(ArgumentException) { return true; } catch(InvalidOperationException) { return true; }
    }
    public static void Ejecutar(string conexion) {
        var c=ClienteValido(); Check(ClienteServicio.Validar(c).Count==0,"Cliente natural válido con centavos");
        Check(c.Telefono=="60000000","Normalización del teléfono +507");
        Invalido(x=>x.Nombres="123123123", "Rechaza nombres numéricos de la captura");
        Invalido(x=>x.Apellidos="123333333", "Rechaza apellidos numéricos de la captura");
        Invalido(x=>x.Nombres="Ana1", "Rechaza números mezclados en nombre natural");
        Invalido(x=>x.Nombres="Ana@@", "Rechaza símbolos en nombre natural");
        c=ClienteValido(); c.Nombres="María José"; c.Apellidos="O’Neill Pérez-Gómez";
        Check(ClienteServicio.Validar(c).Count==0,"Admite acentos, apóstrofos y apellidos compuestos");
        c=ClienteValido(); c.Nombres="Jose\u0301";
        Check(ClienteServicio.Validar(c).Count==0 && c.Nombres=="José", "Normaliza acentos Unicode");
        Invalido(x=>x.TipoPersona="OTRA", "Tipo de persona fuera de catálogo");
        Invalido(x=>x.TipoIdentificacion="OTRO", "Documento fuera de catálogo");
        Invalido(x=>{x.TipoIdentificacion="RUC";x.Identificacion="12345-1-123456";}, "Natural con RUC rechazado antes de SQL");
        Invalido(x=>x.Identificacion="ABC-0-0", "Prefijo arbitrario de cédula rechazado");
        Invalido(x=>x.Identificacion="asdasdaewrfawdasd", "Identificación de la captura rechazada");
        c=ClienteValido(); c.Identificacion="E-123-456";
        Check(ClienteServicio.Validar(c).Count==0,"Prefijo E admitido");
        Invalido(x=>{x.TipoPersona="JURIDICA";x.TipoIdentificacion="RUC";x.Identificacion="A";}, "RUC de una letra rechazado");
        Invalido(x=>{x.TipoPersona="JURIDICA";x.TipoIdentificacion="RUC";x.Identificacion="12345-1-123456";x.Nombres="12345";}, "Razón social solo numérica rechazada");
        c=ClienteValido(); c.TipoPersona="JURIDICA";c.TipoIdentificacion="RUC";c.Identificacion="12345-1-123456";c.Nombres="Comercial 123, S.A. & Hijos";
        Check(ClienteServicio.Validar(c).Count==0 && c.Apellidos=="", "Razón social admite números y signos; datos personales no aplican");
        Invalido(x=>x.Telefono="0000000", "Teléfono de ceros rechazado");
        Invalido(x=>x.Telefono="6000ABCD", "Teléfono con letras rechazado");
        Invalido(x=>x.Sexo="NO_APLICA", "No aplica rechazado para sexo de natural");
        Invalido(x=>x.EstadoCivil="NO_APLICA", "No aplica rechazado para estado civil de natural");
        Invalido(x=>x.FechaNacimiento=ClienteServicio.HoyPanama.AddYears(-120).AddDays(-1), "Nacimiento fuera del rango");
        Invalido(x=>x.Direccion="12345", "Dirección sin descripción rechazada");
        Invalido(x=>x.Ocupacion="12345", "Ocupación sin descripción rechazada");
        Invalido(x=>x.Referencias="x", "Referencia incompleta rechazada");
        Invalido(x=>x.Referencias="Ana, 123, familiar", "Teléfono de referencia inválido");
        Invalido(x=>x.Referencias="60000001", "Referencia sin nombre rechazada");
        Invalido(x=>x.Referencias="Ana, 00000000", "Referencia con teléfono de ceros rechazada");
        Invalido(x=>x.Referencias="Ana, 123456789", "No acepta fragmento de teléfono demasiado largo");
        foreach(var referencia in new[]{"Ana Pérez\n6718-9025\nVecina", "Teléfono: +507 6718-9025; Nombre: Ana Pérez (vecina)", "Ana Pérez 6718-9025", "Ana Pérez\r\n\r\n6718-9025\r\nVecina"}) {
            c=ClienteValido();c.Referencias=referencia;
            Check(ClienteServicio.Validar(c).Count==0,"Referencia en formato libre: " + referencia.Replace("\n"," / "));
        }
        c=ClienteValido();c.Referencias="Ana Pérez, +507 6000-0001, familiar\nJosé Gómez, 200-0001, compañero";
        Check(ClienteServicio.Validar(c).Count==0,"Varias referencias válidas por línea");
        Invalido(x=>x.Direccion="Calle <script>", "Símbolos de marcado rechazados");
        Invalido(x=>x.Ocupacion="Analista\u0000", "Caracteres de control rechazados");
        foreach(var propiedad in new[]{"Identificacion","Nombres","Apellidos","Correo","Direccion","Provincia","Distrito","Corregimiento","Ocupacion","Referencias"}) {
            var limites=new[]{30,120,120,254,300,80,150,150,200,1000};
            var nombres=new[]{"Identificacion","Nombres","Apellidos","Correo","Direccion","Provincia","Distrito","Corregimiento","Ocupacion","Referencias"};
            int limite=limites[Array.IndexOf(nombres,propiedad)];
            Invalido(x=>typeof(Cliente).GetProperty(propiedad).SetValue(x,new string('a',limite+1),null),"Límite de " + propiedad);
            Invalido(x=>typeof(Cliente).GetProperty(propiedad).SetValue(x,"",null),"Obligatoriedad de " + propiedad);
        }
        Invalido(x=>x.Ingresos=-1m,"Ingresos negativos");
        Invalido(x=>x.Ingresos=1.001m,"Más de dos decimales");
        Invalido(x=>x.Ingresos=1000000000000m,"Límite de ingresos");
        Invalido(x=>x.Correo="incorrecto","Correo inválido");
        Invalido(x=>x.FechaNacimiento=DateTime.Today.AddDays(1),"Nacimiento futuro");
        Invalido(x=>x.FechaNacimiento=null,"Nacimiento obligatorio");
        Invalido(x=>x.Nombres="","Nombre obligatorio");
        Invalido(x=>x.Provincia="","Provincia obligatoria");
        Invalido(x=>x.CodigoProvincia="8","Código conserva cero inicial");
        var ubicacionRepo = new ClienteRepositorio(conexion);
        Check(ubicacionRepo.Ubicaciones(1).Count==13,"13 provincias suministradas");
        Check(ubicacionRepo.Ubicaciones(2,"08").Any(x=>x.Key=="0801") && ubicacionRepo.Ubicaciones(2,"08").All(x=>x.Key.StartsWith("08")),"Distritos dependen de provincia");
        Check(ubicacionRepo.Ubicaciones(3,"08","0801").Any(x=>x.Key=="080105") && ubicacionRepo.Ubicaciones(3,"08","0801").All(x=>x.Key.StartsWith("0801")),"Corregimientos dependen de distrito");
        Check(ubicacionRepo.Ubicaciones(3,"13","1306").Count==0,"BRILLANTE sin distrito se conserva sin habilitar");
        var cruzado=ClienteValido(); cruzado.CodigoProvincia="01";
        Check(Falla(()=>ubicacionRepo.Guardar(cruzado)),"Rechaza jerarquía cruzada");
        Invalido(x=>x.Referencias="","Referencias obligatorias");
        Invalido(x=>x.Identificacion="123","Formato de cédula");
        Invalido(x=>x.Telefono="123","Teléfono inválido");
        c=ClienteValido(); c.TipoPersona="JURIDICA"; c.TipoIdentificacion="RUC"; c.Identificacion="12345-1-123456"; c.Apellidos=""; c.FechaNacimiento=null; c.Sexo=""; c.EstadoCivil="";
        Check(ClienteServicio.Validar(c).Count==0 && c.Sexo=="NO_APLICA","Persona jurídica sin datos personales");
        c=ClienteValido(); c.TipoIdentificacion="PASAPORTE"; c.Identificacion=" ab123456 ";
        Check(ClienteServicio.Validar(c).Count==0 && c.Identificacion=="AB123456","Pasaporte y normalización");
        using(var scope=new TransactionScope()) {
            var servicio=new ClienteServicio(new ClienteRepositorio(conexion));
            var token=Guid.NewGuid().ToString("N").Substring(0,20).ToUpperInvariant();
            c=ClienteValido(); c.TipoIdentificacion="PASAPORTE"; c.Identificacion=token; c.Nombres="Prueba Ana";
            int id=servicio.Guardar(c); Check(id>0,"Alta persistida");
            var editado=servicio.Obtener(id); Check(editado.Ingresos==1250.50m && editado.FechaNacimiento==c.FechaNacimiento,"Lectura conserva centavos y fecha");
            Check(editado.CodigoProvincia=="08" && editado.CodigoDistrito=="0801" && editado.CodigoCorregimiento=="080105" && editado.Provincia=="PANAMA" && editado.Corregimiento=="BELLA VISTA","Códigos y nombres originales persistidos");
            Check(servicio.Listar(token,true).Any(x=>x.Id==id),"Búsqueda y filtro activo");
            Check(servicio.Listar("60000000",true).Any(x=>x.Id==id),"Búsqueda por teléfono");
            Check(servicio.Listar(token,true,"panama","NATURAL").Any(x=>x.Id==id),"Provincia sin distinguir mayúsculas y persona natural");
            Check(!servicio.Listar(token,true,"Chiriquí","NATURAL").Any(x=>x.Id==id),"Filtro por otra provincia");
            Check(!servicio.Listar(token,true,"","JURIDICA").Any(x=>x.Id==id),"Filtro por tipo de persona");
            Check(editado.CreadoUtc > DateTime.UtcNow.AddMinutes(-5) && editado.CreadoUtc <= DateTime.UtcNow.AddMinutes(1),"Fecha de alta disponible para el gráfico");
            Check(Falla(()=>servicio.Guardar(c)),"Identificación duplicada rechazada");
            var antigua=servicio.Obtener(id); editado.Nombres="Editado Ana"; servicio.Guardar(editado);
            Check(servicio.Obtener(id).Nombres==editado.Nombres,"Edición persistida");
            Check(Falla(()=>servicio.Guardar(antigua)),"Edición concurrente rechazada");
            Check(!servicio.Obtener(id).Version.SequenceEqual(antigua.Version),"Versión numérica incrementada al editar");
            var juridico=ClienteValido(); juridico.TipoPersona="JURIDICA"; juridico.TipoIdentificacion="RUC"; juridico.Identificacion="J"+token;
            juridico.Nombres="Empresa " + token; juridico.Apellidos=""; juridico.FechaNacimiento=null; juridico.Sexo=""; juridico.EstadoCivil="";
            juridico.Identificacion="12345-1-"+DateTime.UtcNow.ToString("HHmmssfff");
            int empresa=servicio.Guardar(juridico);
            Check(servicio.Obtener(empresa).EstadoCivil=="NO_APLICA" && !servicio.Obtener(empresa).FechaNacimiento.HasValue,"Persona jurídica persistida en MariaDB");
            var actual=servicio.Obtener(id); servicio.CambiarEstado(id,false,actual.Version);
            Check(!servicio.Obtener(id).Activo,"Desactivación conserva cliente");
            Check(!servicio.Listar(token,true).Any(x=>x.Id==id) && servicio.Listar(token,false).Any(x=>x.Id==id),"Filtro inactivos");
            Check(Falla(()=>servicio.Guardar(c)),"Duplicado inactivo rechazado");
            Check(Falla(()=>servicio.CambiarEstado(id,true,actual.Version)),"Cambio de estado concurrente rechazado");
            actual=servicio.Obtener(id); servicio.CambiarEstado(id,true,actual.Version);
            Check(servicio.Obtener(id).Activo,"Reactivación");
            Check(servicio.Listar("' OR 1=1 --",null).Count==0,"Texto SQL tratado como búsqueda literal");
            Check(servicio.Listar("%",null).Count==0,"Búsqueda literal sin comodines");
            // No Complete(): la transacción revierte todos los datos de prueba.
        }
        Console.WriteLine("TOTAL: " + casos + " comprobaciones correctas; datos de prueba revertidos.");
    }
}
'@
$archivoCodigo = Join-Path $salida 'PruebasClientes.cs'
[System.IO.File]::WriteAllText($archivoCodigo, $codigo, [System.Text.UTF8Encoding]::new($false))
$ejecutable = Join-Path $salida 'PruebasClientes.dll'
$compilador = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$argumentos = @('/nologo','/target:library',('/out:' + $ejecutable),'/reference:System.Transactions.dll','/reference:System.Core.dll')
$argumentos += $refs | ForEach-Object { '/reference:' + $_ }
& $compilador @argumentos $archivoCodigo
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el ensamblado de pruebas.' }
[xml]$web = Get-Content -LiteralPath (Join-Path $raiz 'SistemaFinanciera\Web.config') -Raw
$config = '<configuration>' + $web.configuration.runtime.OuterXml + '</configuration>'
[System.IO.File]::WriteAllText(($ejecutable + '.config'), $config, [System.Text.UTF8Encoding]::new($false))
$ensamblado = [Reflection.Assembly]::LoadFrom($ejecutable)
$tipoPruebas = $ensamblado.GetType('PruebasClientes')
$entorno = [AppDomainSetup]::new()
$entorno.ApplicationBase = $salida
$entorno.ConfigurationFile = $ejecutable + '.config'
$dominio = [AppDomain]::CreateDomain('PruebasMariaDB', $null, $entorno)
try {
    $runner = $dominio.CreateInstanceFromAndUnwrap($ejecutable, 'PruebasClientes')
    $tipoPruebas.GetMethod('Run').Invoke($runner, [object[]]@($Conexion))
} catch { throw $_.Exception.ToString() }
finally { [AppDomain]::Unload($dominio) }
