using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;
public class SeguridadAdminTests:MarshalByRefObject {
 static object Q(MySqlConnection c,string s){using(var q=new MySqlCommand(s,c))return q.ExecuteScalar();}
 static void Check(bool v,string m){if(!v)throw new Exception(m);Console.WriteLine("OK: "+m);}
 public void Run(string connection){
 var cs=new MySqlConnectionStringBuilder(connection);string db="cw_test_seguridad_"+Guid.NewGuid().ToString("N");
 using(var cn=new MySqlConnection(connection)){cn.Open();Q(cn,"CREATE DATABASE `"+db+"`");try{
 foreach(string t in new[]{"Usuarios","Roles","UsuarioRoles","ParametrosSistema","Auditoria"}){Q(cn,"CREATE TABLE `"+db+"`.`"+t+"` LIKE `"+cs.Database+"`.`"+t+"`");if(t!="Auditoria")Q(cn,"INSERT INTO `"+db+"`.`"+t+"` SELECT * FROM `"+cs.Database+"`.`"+t+"`");}
 cs.Database=db;using(var c=new MySqlConnection(cs.ConnectionString)){c.Open();var repo=new FinanzasRepositorio(cs.ConnectionString);var service=new FinanzasServicio(repo);
 int id=Convert.ToInt32(Q(c,"SELECT MIN(u.Id) FROM Usuarios u JOIN UsuarioRoles ur ON ur.UsuarioId=u.Id JOIN Roles r ON r.Id=ur.RolId WHERE r.Codigo='ADMINISTRADOR' AND u.Activo=1"));
 string reset="UPDATE Usuarios SET Activo=1,IntentosFallidos=0,BloqueadoHastaUtc=NULL WHERE Id="+id;
 var values=new Dictionary<string,string>{{"INTENTOS_MAXIMOS","2"},{"BLOQUEO_MINUTOS","3"},{"INACTIVIDAD_MINUTOS","9"},{"CLAVE_LONGITUD_MINIMA","14"},{"CLAVE_MAYUSCULA","1"},{"CLAVE_NUMERO","1"},{"PORCENTAJE_MAXIMO_CUOTA_INGRESOS","35.1256"},{"TASA_MORA_ANUAL","12.3456"},{"DIAS_GRACIA_MORA","5"}};
 service.GuardarParametrosAdministracion(id,values);Check(service.PoliticaClaveActual()["minimo"]==14,"Politica actualizada desde panel");Check(service.Usuario(id).MinutosInactividad==9,"Inactividad aplicada");
 foreach(var key in new[]{"INTENTOS_MAXIMOS","BLOQUEO_MINUTOS","INACTIVIDAD_MINUTOS","CLAVE_LONGITUD_MINIMA","DIAS_GRACIA_MORA","PORCENTAJE_MAXIMO_CUOTA_INGRESOS","TASA_MORA_ANUAL"}){var bad=new Dictionary<string,string>(values);bad[key]="9999";bool rejected=false;try{service.GuardarParametrosAdministracion(id,bad);}catch(ArgumentException){rejected=true;}Check(rejected,"Rechazo fuera de rango: "+key);}
 Q(c,reset);repo.ResultadoLogin(id,false);Check(Q(c,"SELECT BloqueadoHastaUtc FROM Usuarios WHERE Id="+id)==DBNull.Value,"Primer fallo sin bloqueo");repo.ResultadoLogin(id,false);Check(Convert.ToInt32(Q(c,"SELECT TIMESTAMPDIFF(SECOND,UTC_TIMESTAMP(),BloqueadoHastaUtc) FROM Usuarios WHERE Id="+id))>170,"Segundo fallo bloquea 3 minutos");Check(!repo.ResultadoLogin(id,true),"Clave correcta no evita bloqueo");
 Q(c,"UPDATE ParametrosSistema SET Valor='1' WHERE Codigo='INTENTOS_MAXIMOS'");Q(c,"UPDATE Usuarios SET BloqueadoHastaUtc=DATE_SUB(UTC_TIMESTAMP(),INTERVAL 1 MINUTE) WHERE Id="+id);repo.ResultadoLogin(id,false);Check(!repo.ResultadoLogin(id,true),"Tras vencimiento limite de un intento vuelve a bloquear");
 Q(c,reset);Q(c,"UPDATE ParametrosSistema SET Valor='2' WHERE Codigo='INTENTOS_MAXIMOS'");Parallel.For(0,6,i=>repo.ResultadoLogin(id,false));Check(Convert.ToInt32(Q(c,"SELECT IntentosFallidos FROM Usuarios WHERE Id="+id))==2,"Intentos concurrentes respetan limite");
 Q(c,"UPDATE Usuarios SET BloqueadoHastaUtc=DATE_SUB(UTC_TIMESTAMP(),INTERVAL 1 MINUTE) WHERE Id="+id);Check(repo.ResultadoLogin(id,true),"Permite acceso al vencer bloqueo");Check(Convert.ToInt32(Q(c,"SELECT IntentosFallidos FROM Usuarios WHERE Id="+id))==0,"Exito limpia intentos");
 bool denied=false;try{service.RestablecerClaveManualUsuarioAdministracion(id,id,"abc");}catch(ArgumentException e){denied=e.Message.Contains("14")&&e.Message.Contains("mayúscula")&&e.Message.Contains("número");}Check(denied,"Mensaje enumera requisitos faltantes");
 string login=Convert.ToString(Q(c,"SELECT NombreUsuario FROM Usuarios WHERE Id="+id));
 using(var cmd=new MySqlCommand("UPDATE Usuarios SET ContrasenaHash=@hash WHERE Id="+id,c)){cmd.Parameters.AddWithValue("@hash",FinanzasServicio.HashClave("PruebaSegura123!"));cmd.ExecuteNonQuery();}
 Action<string> rejectLogin=password=>{bool blocked=false;try{service.IniciarSesion(login,password);}catch(ArgumentException){blocked=true;}Check(blocked,"Login rechaza acceso no permitido");};
 foreach(int limit in new[]{1,3,5}){
  Q(c,reset);values["INTENTOS_MAXIMOS"]=limit.ToString();values["BLOQUEO_MINUTOS"]="2";service.GuardarParametrosAdministracion(id,values);
  for(int attempt=1;attempt<=limit;attempt++){rejectLogin("ClaveErronea");Check(Convert.ToInt32(Q(c,"SELECT BloqueadoHastaUtc IS NOT NULL FROM Usuarios WHERE Id="+id))==(attempt==limit?1:0),"Bloqueo exacto en intento "+attempt+" de "+limit);}
  bool explicitBlock=false;
  try{service.IniciarSesion(login,"PruebaSegura123!");}catch(AccesoBloqueadoException e){explicitBlock=e.Message.Contains("bloqueada")&&e.Message.Contains("minuto");}
  Check(explicitBlock,"Login informa bloqueo y tiempo restante");
  Check(Convert.ToInt32(Q(c,"SELECT TIMESTAMPDIFF(SECOND,UTC_TIMESTAMP(),BloqueadoHastaUtc) FROM Usuarios WHERE Id="+id))>110,"Login aplica minutos guardados");
  Q(c,"UPDATE Usuarios SET BloqueadoHastaUtc=DATE_SUB(UTC_TIMESTAMP(),INTERVAL 1 SECOND) WHERE Id="+id);
  Check(service.IniciarSesion(login,"PruebaSegura123!")==id,"Login permite entrar tras expirar bloqueo");
 }
 Q(c,reset);rejectLogin("ClaveErronea");Check(service.IniciarSesion(login,"PruebaSegura123!")==id,"Clave correcta antes del limite permite entrar");
 Check(Convert.ToInt32(Q(c,"SELECT IntentosFallidos FROM Usuarios WHERE Id="+id))==0,"Login correcto reinicia contador");
 Console.WriteLine("Pruebas de seguridad terminadas sin modificar la base original.");
 }
 }finally{Q(cn,"DROP DATABASE `"+db+"`");}}
 }
}
