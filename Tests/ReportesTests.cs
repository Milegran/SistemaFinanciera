using System;
using System.Data;
using MySqlConnector;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
public class ReportesTests : MarshalByRefObject {
    static int casos;
    static void Check(bool ok,string nombre){if(!ok)throw new Exception(nombre);casos++;Console.WriteLine("OK: "+nombre);}
    static void Rechaza(Action accion,string nombre){try{accion();}catch(ArgumentException){Check(true,nombre);return;}catch(UnauthorizedAccessException){Check(true,nombre);return;}throw new Exception("No rechazo: "+nombre);}
    public void Run(string conexion){try{Pruebas(conexion);}catch(Exception e){throw new Exception(e.ToString());}}
    static void Pruebas(string conexion){
        int usuario;
        using(var cn=new MySqlConnection(conexion)){cn.Open();using(var cmd=new MySqlCommand("SELECT MIN(u.Id) FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Activo=1 AND r.Codigo='ADMINISTRADOR'",cn))usuario=Convert.ToInt32(cmd.ExecuteScalar());}
        var repo=new FinanzasRepositorio(conexion);var servicio=new FinanzasServicio(repo);
        for(int i=1;i<=10;i++){
            var datos=repo.ConsultarReporte(i,usuario,null,null,0,0,0);
            Check(datos.Columns.Count>=3,"Consulta y columnas del reporte "+i);
            var vacio=repo.ConsultarReporte(i,usuario,new DateTime(2099,1,1),new DateTime(2099,1,2),int.MaxValue,i==1?0:int.MaxValue,0);
            Check(vacio.Rows.Count==0,"Filtros sin coincidencias del reporte "+i);
        }
        Rechaza(()=>servicio.ConsultarReporte(0,usuario,null,null,0,0,0),"Rechaza reporte fuera de catalogo");
        Rechaza(()=>servicio.ConsultarReporte(5,usuario,new DateTime(2026,10,2),new DateTime(2026,10,1),0,0,0),"Rechaza fechas invertidas");
        Rechaza(()=>servicio.ConsultarReporte(5,usuario,null,DateTime.MaxValue,0,0,0),"Rechaza fecha que desborda el limite");
        Rechaza(()=>servicio.ConsultarReporte(7,usuario,null,null,0,0,0),"Estado de cuenta exige prestamo");
        Rechaza(()=>servicio.ConsultarReporte(8,usuario,null,null,0,0,0),"Amortizacion exige prestamo");
        Rechaza(()=>repo.ConsultarReporte(1,int.MaxValue,null,null,0,0,0),"Rechaza usuario inexistente");
        Check(SistemaFinanciera.Reportes.CeldaCsv("=1+1")=="\"'=1+1\"","CSV neutraliza formulas");
        Check(SistemaFinanciera.Reportes.CeldaCsv("  @SUM(A1)").StartsWith("\"'"),"CSV neutraliza formula con espacios");
        Check(SistemaFinanciera.Reportes.CeldaCsv("A\"B")=="\"A\"\"B\"","CSV escapa comillas");
        Check(SistemaFinanciera.Reportes.CeldaCsv(-12.5m)=="\"-12.50\"","CSV conserva importes numericos negativos");
        Check(SistemaFinanciera.Reportes.CeldaCsv(DBNull.Value)=="\"\"","CSV admite valores nulos");
        Console.WriteLine("TOTAL: "+casos+" comprobaciones de solo lectura correctas.");
    }
}
