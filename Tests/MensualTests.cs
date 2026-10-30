using System;
using System.Data;
using MySqlConnector;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
using SistemaFinanciera.Entities;
public class MensualTests : MarshalByRefObject {
 public void Run(string conexion) {
  int usuario;
  using(var cn=new MySqlConnection(conexion)){cn.Open();using(var cmd=new MySqlCommand("SELECT MIN(u.Id) FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Codigo='ADMINISTRADOR'",cn)) usuario=Convert.ToInt32(cmd.ExecuteScalar());}
  var repo=new FinanzasRepositorio(conexion);
  var pagos=repo.Consultar("PERIODICIDADES_PAGO",usuario,0);
  if(pagos.Rows.Count==0)throw new Exception("Falta mensual");
  foreach(DataRow p in pagos.Rows) if((string)p["Unidad"]!="MESES"||Convert.ToInt32(p["Intervalo"])!=1||Convert.ToDecimal(p["PeriodosPorAnio"])!=12)throw new Exception("Periodicidad no mensual visible");
  int pruebas=1;
  foreach(DataRow producto in repo.Consultar("PRODUCTOS_ACTIVOS",usuario,0).Rows){
   int id=Convert.ToInt32(producto["Id"]);
   foreach(DataRow p in repo.Consultar("PRODUCTO_PERIODICIDADES",usuario,id).Rows){if(pagos.Select("Id="+p["Id"]).Length!=1)throw new Exception("Producto muestra otra periodicidad");pruebas++;}
   var metodos=repo.Consultar("PRODUCTO_METODOS",usuario,id);
   if(metodos.Rows.Count==0)continue;
   foreach(DataRow p in repo.Consultar("PERIODICIDADES",usuario,0).Rows){
    if((string)p["Unidad"]=="MESES"&&Convert.ToInt32(p["Intervalo"])==1)continue;
    var solicitud=new SolicitudFinanciera{ProductoId=id,MetodoId=Convert.ToInt32(metodos.Rows[0]["Id"]),PeriodicidadId=Convert.ToInt32(p["Id"]),Monto=3000m,CantidadCuotas=60,FechaPrimerPago=DateTime.Today.AddMonths(1)};
    bool rechazo=false;
    try {repo.SimularProducto(solicitud,usuario,new MotorPrestamos().Generar);}catch(ArgumentException){rechazo=true;}
    if(!rechazo)throw new Exception("Se acepto una simulacion no mensual");pruebas++;
   }
  }
  Console.WriteLine("OK: "+pruebas+" comprobaciones de periodicidad mensual, solo lectura.");
 }
}
