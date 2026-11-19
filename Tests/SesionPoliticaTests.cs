using System;
using System.Collections.Generic;
using System.IO;
using MySqlConnector;
using SistemaFinanciera.BL;
using SistemaFinanciera.DAL;

public class SesionPoliticaTests : MarshalByRefObject {
    static int casos;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("OK: " + name); casos++; }
    static void Rechaza(Action action, string name) { try { action(); } catch (ArgumentException) { Check(true,name); return; } throw new Exception("No se rechazo: " + name); }
    public void Run(string conexion) {
        if (new MySqlConnectionStringBuilder(conexion).Database != "capitalwest_pruebas_sesion") throw new Exception("Solo base aislada capitalwest_pruebas_sesion");
        var servicio = new FinanzasServicio(new FinanzasRepositorio(conexion));
        int admin = servicio.CrearAdministrador("pruebas_admin", "Administrador Ficticio", "admin@example.test", "PruebaSegura123!");
        int rol;
        using (var cn = new MySqlConnection(conexion)) { cn.Open(); using (var cmd = new MySqlCommand("SELECT Id FROM Roles WHERE Codigo='CONSULTA'",cn)) rol=Convert.ToInt32(cmd.ExecuteScalar()); }
        servicio.GuardarUsuarioAdministracion(admin,0,"pruebas_clave","Usuario Ficticio","usuario@example.test",rol,true,"InicialSegura123!");
        int usuario = servicio.IniciarSesion("pruebas_clave","InicialSegura123!");
        long version = servicio.Usuario(usuario).VersionSesion;
        string temporal = servicio.RestablecerClaveUsuarioAdministracion(admin,usuario);
        var actor = servicio.Usuario(usuario);
        Check(actor.DebeCambiarClave && actor.VersionSesion > version,"Recuperacion exige cambio e incrementa version");
        Check(servicio.IniciarSesion("pruebas_clave",temporal)==usuario,"Clave temporal generada permite entrar");
        var reglas=servicio.PoliticaClaveActual();
        Check(reglas["minimo"]==8 && reglas["maximo"]==128 && reglas["mayuscula"]==1 && reglas["numero"]==1,"Politica de captura coincide con servidor");
        foreach(var invalida in new[]{"abc","mayuscula123","SINNUMERO",new string('A',129)+"1"})
            Rechaza(()=>servicio.CambiarClavePropia(usuario,invalida),"Rechaza clave que incumple requisitos");
        Check(servicio.Usuario(usuario).VersionSesion==actor.VersionSesion,"Errores de validacion no cambian la version");
        servicio.CambiarClavePropia(usuario,"Valida12");
        Check(!servicio.Usuario(usuario).DebeCambiarClave && servicio.Usuario(usuario).VersionSesion>actor.VersionSesion,"Cambiar clave limpia exigencia e invalida tickets previos");
        Check(servicio.IniciarSesion("pruebas_clave","Valida12")==usuario,"Clave valida de ocho caracteres admitida");
        using (var cn = new MySqlConnection(conexion)) { cn.Open(); using (var cmd = new MySqlCommand("UPDATE ParametrosSistema SET Valor='14' WHERE Codigo='CLAVE_LONGITUD_MINIMA'",cn)) cmd.ExecuteNonQuery(); }
        Check(servicio.PoliticaClaveActual()["minimo"]==14,"Politica configurable sin hardcodear doce caracteres");
        Rechaza(()=>servicio.CambiarClavePropia(usuario,"Valida12"),"Cambio respeta nuevo minimo configurado");
        using (var cn = new MySqlConnection(conexion)) { cn.Open(); using (var cmd = new MySqlCommand("UPDATE ParametrosSistema SET Valor='8' WHERE Codigo='CLAVE_LONGITUD_MINIMA'",cn)) cmd.ExecuteNonQuery(); }
        // Dejar la cuenta ficticia lista para verificar el cambio forzado por HTTP.
        temporal=servicio.RestablecerClaveUsuarioAdministracion(admin,usuario);
        File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"credenciales-prueba.txt"),new[]{"pruebas_clave",temporal});
        Console.WriteLine("TOTAL: "+casos+" pruebas de politica y version aprobadas");
    }
}
