using System;
using MySqlConnector;
using SistemaFinanciera.DAL;
public class AccionesProductoTests:MarshalByRefObject {
 static object Q(MySqlConnection cn,string sql){using(var c=new MySqlCommand(sql,cn))return c.ExecuteScalar();}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("OK: "+text);}
 public void Run(string conexion){
  var original=new MySqlConnectionStringBuilder(conexion);
  string db="cw_test_acciones_"+Guid.NewGuid().ToString("N");
  using(var cn=new MySqlConnection(conexion)){cn.Open();Q(cn,"CREATE DATABASE `"+db+"`");
   try {
    foreach(var t in new[]{"Usuarios","Roles","UsuarioRoles","ProductosFinancieros","ProductoMetodos","ProductoPeriodicidades","Tasas","Solicitudes","Prestamos","Auditoria"}){
     Q(cn,"CREATE TABLE `"+db+"`.`"+t+"` LIKE `"+original.Database+"`.`"+t+"`");
     if(t!="Auditoria")Q(cn,"INSERT INTO `"+db+"`.`"+t+"` SELECT * FROM `"+original.Database+"`.`"+t+"`");
    }
    original.Database=db;
    using(var test=new MySqlConnection(original.ConnectionString)){test.Open();
     int user=Convert.ToInt32(Q(test,"SELECT MIN(u.Id) FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE u.Activo=1 AND r.Codigo='ADMINISTRADOR'"));
     int id=Convert.ToInt32(Q(test,"SELECT MIN(Id) FROM ProductosFinancieros"));
     var repo=new FinanzasRepositorio(original.ConnectionString);
     long tasas=Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Tasas"));
     repo.CambiarEstadoProducto(id,false,user);
     Check(!Convert.ToBoolean(Q(test,"SELECT Activo FROM ProductosFinancieros WHERE Id="+id)),"Desactivar producto");
     repo.CambiarEstadoProducto(id,true,user);
     Check(Convert.ToBoolean(Q(test,"SELECT Activo FROM ProductosFinancieros WHERE Id="+id)),"Reactivar producto");
     Check(tasas==Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Tasas")),"Cambiar estado conserva tasas");
     bool denied=false;try{repo.EliminarProducto(id,int.MaxValue);}catch(UnauthorizedAccessException){denied=true;}
     Check(denied,"Eliminar exige usuario autorizado");
     if(Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Solicitudes WHERE ProductoId="+id))+Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Prestamos WHERE ProductoId="+id))>0){
      bool blocked=false;try{repo.EliminarProducto(id,user);}catch(ArgumentException){blocked=true;}
      Check(blocked,"Bloquea eliminar producto utilizado");
      Check(tasas==Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Tasas")),"El rechazo conserva tasas");
     }
     Q(test,"DELETE FROM Solicitudes WHERE ProductoId="+id);Q(test,"DELETE FROM Prestamos WHERE ProductoId="+id);
     repo.EliminarProducto(id,user);
     Check(Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM ProductosFinancieros WHERE Id="+id))==0,"Elimina producto sin operaciones");
     Check(Convert.ToInt64(Q(test,"SELECT COUNT(*) FROM Tasas WHERE ProductoId="+id))==0,"Elimina tasas del producto sin operaciones");
    }
   }finally{Q(cn,"DROP DATABASE `"+db+"`");}
  }
 }
}
