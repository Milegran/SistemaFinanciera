param([string]$Conexion = 'Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=SistemaFinanciera;Integrated Security=True;Connect Timeout=15')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $raiz 'SistemaFinanciera\bin'
$refs = @((Join-Path $dll 'SistemaFinanciera.Entities.dll'), (Join-Path $dll 'SistemaFinanciera.BL.dll'), (Join-Path $dll 'SistemaFinanciera.DAL.dll'), 'System.Data.dll', 'System.Transactions.dll', 'System.Core.dll')
foreach ($ruta in $refs[0..2]) { [void][Reflection.Assembly]::LoadFrom($ruta) }
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;
using System.Linq;
using System.Transactions;
using SistemaFinanciera.Entities;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
public static class PruebasClientes {
    static int casos;
    static void Check(bool condicion, string nombre) {
        if (!condicion) throw new Exception("FALLO: " + nombre);
        casos++; Console.WriteLine("OK: " + nombre);
    }
    static Cliente ClienteValido() {
        return new Cliente {TipoPersona="NATURAL",TipoIdentificacion="CEDULA",Identificacion="8-123-456",Nombres="Ana",Apellidos="Prueba",FechaNacimiento=new DateTime(1990,5,1),Sexo="F",EstadoCivil="SOLTERO",Telefono="+507 6000-0000",Correo="ana@example.com",Direccion="Calle de prueba",Provincia="Panamá",Distrito="Panamá",Corregimiento="Bella Vista",Ocupacion="Analista",Ingresos=1250.50m,Referencias="Persona de referencia, 6000-0001, familiar"};
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
        Invalido(x=>x.Ingresos=-1m,"Ingresos negativos");
        Invalido(x=>x.Ingresos=1.001m,"Más de dos decimales");
        Invalido(x=>x.Ingresos=1000000000000m,"Límite de ingresos");
        Invalido(x=>x.Correo="incorrecto","Correo inválido");
        Invalido(x=>x.FechaNacimiento=DateTime.Today.AddDays(1),"Nacimiento futuro");
        Invalido(x=>x.FechaNacimiento=null,"Nacimiento obligatorio");
        Invalido(x=>x.Nombres="","Nombre obligatorio");
        Invalido(x=>x.Provincia="","Provincia obligatoria");
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
            c=ClienteValido(); c.TipoIdentificacion="PASAPORTE"; c.Identificacion=token; c.Nombres="Prueba " + token;
            int id=servicio.Guardar(c); Check(id>0,"Alta persistida");
            var editado=servicio.Obtener(id); Check(editado.Ingresos==1250.50m && editado.FechaNacimiento==c.FechaNacimiento,"Lectura conserva centavos y fecha");
            Check(servicio.Listar(token,true).Any(x=>x.Id==id),"Búsqueda y filtro activo");
            Check(servicio.Listar("60000000",true).Any(x=>x.Id==id),"Búsqueda por teléfono");
            Check(servicio.Listar(token,true,"panamá","NATURAL").Any(x=>x.Id==id),"Provincia sin distinguir mayúsculas y persona natural");
            Check(!servicio.Listar(token,true,"Chiriquí","NATURAL").Any(x=>x.Id==id),"Filtro por otra provincia");
            Check(!servicio.Listar(token,true,"","JURIDICA").Any(x=>x.Id==id),"Filtro por tipo de persona");
            Check(editado.CreadoUtc.Year>=2026,"Fecha de alta disponible para el gráfico");
            Check(Falla(()=>servicio.Guardar(c)),"Identificación duplicada rechazada");
            var antigua=servicio.Obtener(id); editado.Nombres="Editado " + token; servicio.Guardar(editado);
            Check(servicio.Obtener(id).Nombres==editado.Nombres,"Edición persistida");
            Check(Falla(()=>servicio.Guardar(antigua)),"Edición concurrente rechazada");
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
[PruebasClientes]::Ejecutar($Conexion)

