using System;
using System.Data;
using MySqlConnector;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
using SistemaFinanciera.Entities;
public class CatalogoTests:MarshalByRefObject {
 static int count;static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);count++;Console.WriteLine("OK: "+msg);}
 public void Run(string conexion){
  Check(FinanzasRepositorio.CalcularComision("PORCENTAJE",2m,3000m)==60m,"Personal: 2% de 3000 = 60");
  Check(FinanzasRepositorio.CalcularComision("PORCENTAJE",1.5m,10000m)==150m,"Auto: 1.5% de 10000 = 150");
  Check(FinanzasRepositorio.CalcularComision("PORCENTAJE",2.5m,10000m)==250m,"Puente: 2.5% de 10000 = 250");
  Check(FinanzasRepositorio.CalcularComision("FIJO",25m,10000m)==25m,"Capitalizable: comision fija 25");
  Check(FinanzasRepositorio.CalcularComision("FIJO",0m,1000m)==0m,"Microcredito sin comision");
  Check(FinanzasRepositorio.CalcularComision("PORCENTAJE",1.5m,1000.33m)==15m,"Redondeo de comision a centavos");
  int user;using(var cn=new MySqlConnection(conexion)){cn.Open();using(var cmd=new MySqlCommand("SELECT MIN(u.Id) FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Codigo='ADMINISTRADOR'",cn))user=Convert.ToInt32(cmd.ExecuteScalar());}
  var repo=new FinanzasRepositorio(conexion);var servicio=new FinanzasServicio(repo);
  foreach(DataRow p in servicio.Consultar("PRODUCTOS_ACTIVOS",user,0).Rows){
   int id=Convert.ToInt32(p["Id"]);var datos=servicio.Consultar("PRODUCTOS",user,id).Rows[0];
   int mensual=Convert.ToInt32(servicio.Consultar("PRODUCTO_PERIODICIDADES",user,id).Rows[0]["Id"]);
   foreach(DataRow metodo in servicio.Consultar("PRODUCTO_METODOS",user,id).Rows){
    int cuotas=Convert.ToInt32(datos["PlazoMinimo"]);
    var plan=servicio.SimularProducto(new SolicitudFinanciera{ProductoId=id,MetodoId=Convert.ToInt32(metodo["Id"]),PeriodicidadId=mensual,Monto=Convert.ToDecimal(datos["MontoMinimo"]),CantidadCuotas=cuotas,FechaPrimerPago=ClienteServicio.HoyPanama.AddMonths(1)},user);
    Check(plan.Count==cuotas,"Simulacion "+p["Nombre"]+" / "+metodo["Nombre"]);
   }
  }
  Console.WriteLine("TOTAL: "+count+" comprobaciones sin modificar datos.");
 }
}
