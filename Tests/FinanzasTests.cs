using System;
using System.Linq;
using System.Data;
using MySqlConnector;
using SistemaFinanciera.Entities;
using SistemaFinanciera.DAL;
using SistemaFinanciera.BL;

public class FinanzasTests : MarshalByRefObject {
    static int casos;
    static void Check(bool ok, string nombre) { if(!ok) throw new Exception("FALLO: "+nombre); casos++; Console.WriteLine("OK: "+nombre); }
    static void Rechaza(Action accion,string nombre) { try { accion(); } catch(ArgumentException) { Check(true,nombre);return; } catch(InvalidOperationException) { Check(true,nombre);return; } catch(UnauthorizedAccessException) { Check(true,nombre);return; } throw new Exception("No rechazo: "+nombre); }
    static CondicionesPrestamo Condicion(string metodo) { return new CondicionesPrestamo {Monto=1000m,TasaAnual=12m,TipoTasa="NOMINAL_ANUAL",CapitalizacionesAnuales=12m,PeriodosAnuales=12m,Unidad="MESES",Intervalo=1,CantidadCuotas=3,FechaPrimerPago=new DateTime(2027,1,31),Metodo=metodo}; }
    static void Sql(string cadena,string sql) { using(var cn=new MySqlConnection(cadena)) { cn.Open();using(var cmd=new MySqlCommand(sql,cn))cmd.ExecuteNonQuery(); } }
    public void RunCaptura() {
        var servicio = new FinanzasServicio(null);
        foreach(var nombre in new[]{"123123", "Rafael123", "Rafael <Barrios>", "Rafael\u200b Barrios", ""})
            Rechaza(()=>servicio.CrearAdministrador("admin",nombre,"admin@example.test","Clave larga de prueba 2026!"),"Administrador rechaza nombre: "+nombre);
        Rechaza(()=>servicio.CrearAdministrador("admin","Rafael Barrios","1233333","Clave larga de prueba 2026!"),"Rechaza correo numérico");
        Rechaza(()=>servicio.CrearAdministrador("admin","Rafael Barrios","admin@example.test","corta"),"Rechaza clave corta");
        Rechaza(()=>servicio.CrearAdministrador("admin","Rafael Barrios","admin@example.test","1234567"),"Rechaza contraseña de siete caracteres");
        Rechaza(()=>servicio.CrearAdministrador("admin","Rafael Barrios","admin@example.test",new string('x',21)),"Rechaza contraseña de veintiún caracteres");
        Rechaza(()=>servicio.CrearAdministrador("ad min","Rafael Barrios","admin@example.test","Clave larga de prueba 2026!"),"Rechaza espacios en usuario");
        Rechaza(()=>servicio.GuardarProducto(new ProductoFinanciero {Codigo="PR",Nombre="12345",Descripcion="Producto de prueba"},1),"Producto rechaza nombre solo numérico");
        Rechaza(()=>FinanzasServicio.Texto("texto\u200boculto","Observación",100),"Rechaza caracteres invisibles");
        FinanzasServicio.Texto("Crédito comercial 2026, S.A.","Nombre",120);
        Check(true,"Producto permite letras, números y puntuación");
        FinanzasServicio.Texto("Primera línea\nSegunda línea: 123.","Observación",1000,false);
        Check(true,"Observaciones permiten texto libre y saltos de línea");
        foreach(var nombre in new[]{"María José", "O’Neill", "Jean-Luc", "José A. Pérez"})
            Check(System.Text.RegularExpressions.Regex.IsMatch(nombre,ClienteServicio.PatronNombre),"Admite nombre: "+nombre);
        foreach(var codigo in new[]{"123456", "1PRE", "PRE--01", "PRE_", "PRE.01", "P", new string('A',21)})
            Rechaza(()=>FinanzasServicio.ValidarDatosProducto(codigo,"Crédito personal 2026","Crédito para consumo."),"Rechaza código: "+codigo);
        foreach(var codigo in new[]{"PR", "pre-01", "CREDITO_2026", new string('A',20)}) {
            FinanzasServicio.ValidarDatosProducto(codigo,"Crédito personal 2026","Crédito para consumo.");
            Check(true,"Admite código: "+codigo);
        }
        foreach(var nombre in new[]{"123456", "9 Crédito", "AB", "Crédito <personal>", "Crédito 💰", new string('A',81)})
            Rechaza(()=>FinanzasServicio.ValidarDatosProducto("PR-01",nombre,"Crédito para consumo."),"Rechaza nombre de producto: "+nombre);
        foreach(var nombre in new[]{"Crédito personal 2026", "Plan 12 meses (B/.)", "Crédito Pérez & Hijos", "Préstamo + ahorro 10%", new string('A',80)}) {
            FinanzasServicio.ValidarDatosProducto("PR-01",nombre,"Crédito para consumo.");
            Check(true,"Admite nombre de producto: "+nombre);
        }
        foreach(var descripcion in new[]{"1234567890", "!!!", new string('A',501)})
            Rechaza(()=>FinanzasServicio.ValidarDatosProducto("PR-01","Crédito personal",descripcion),"Rechaza descripción sin texto o demasiado larga");
        FinanzasServicio.ValidarDatosProducto("PR-01","Crédito personal",new string('A',500));
        Check(true,"Admite descripción de 500 caracteres");
        Rechaza(()=>FinanzasServicio.TextoConLetras("123","Motivo",1000),"Motivo no admite solo números");
        FinanzasServicio.TextoConLetras("","Observaciones",1000,false);
        Check(true,"Observaciones siguen opcionales");
        FinanzasServicio.Texto("1234567890","Referencia de pago",100);
        Check(true,"Referencia de pago numérica sigue siendo válida");
        foreach(var usuario in new[]{"12345", "1admin", "ad min", new string('A',21)})
            Rechaza(()=>servicio.CrearAdministrador(usuario,"Rafael Barrios","admin@example.test","Prueba8!"),"Rechaza usuario inicial: "+usuario);
        Console.WriteLine("CAPTURA: "+casos+" comprobaciones aprobadas; sin acceso a base de datos.");
    }
    public void Run(string conexion) {
        var motor=new MotorPrestamos();
        foreach(var metodo in new[]{"FRANCES","ALEMAN","AMERICANO","SIMPLE","COMPUESTO"}) {
            var c=Condicion(metodo);var q=motor.Generar(c);
            Check(q.Count==3 && q.Last().Saldo==0,"Calendario termina en cero: "+metodo);
            Check(q.Sum(x=>x.Capital)==1000m,"Conserva capital: "+metodo);
            Check(q[1].Fecha==new DateTime(2027,2,28) && q[2].Fecha==new DateTime(2027,3,31),"Calendario conserva ancla de fin de mes: "+metodo);
            c.TasaAnual=0;c.CantidadCuotas=12;q=motor.Generar(c);
            Check(q.Sum(x=>x.Interes)==0 && q.Sum(x=>x.Capital)==1000m,"Tasa cero: "+metodo);
            foreach(var monto in new[]{0.01m,1m,10000000m}) {
                c.Monto=monto;c.CantidadCuotas=120;c.TasaAnual=12;
                q=motor.Generar(c);Check(q.Sum(x=>x.Capital)==monto && q.Last().Saldo==0,"Monto/plazo y redondeo: "+metodo+" / "+monto);
            }
        }
        var baseFrances=Condicion("FRANCES");baseFrances.CantidadCuotas=2;
        var frances=motor.Generar(baseFrances);Check(frances[0].Cuota==507.51m && frances[1].Cuota==507.51m && frances.Sum(x=>x.Interes)==15.02m,"Frances: ejemplo manual 1000, 1% mensual, 2 cuotas");
        var aleman=motor.Generar(Condicion("ALEMAN"));Check(aleman[0].Capital==333.33m && aleman[2].Capital==333.34m && aleman.Sum(x=>x.Interes)==20m,"Aleman: capital e intereses manuales");
        Check(motor.Generar(Condicion("AMERICANO")).Last().Cuota==1010m,"Americano: pago global manual");
        Check(motor.Generar(Condicion("SIMPLE")).Sum(x=>x.Interes)==30m,"Simple: P*r*n/f = 30");
        Check(motor.Generar(Condicion("COMPUESTO")).Last().Cuota==1030.30m,"Compuesto: 1000*(1.01)^3 = 1030.30");
        var efectivo=Condicion("FRANCES");efectivo.TipoTasa="EFECTIVA_ANUAL";efectivo.TasaAnual=12.682503m;
        Check(Math.Abs(motor.Generar(efectivo)[0].Interes-10m)<=0.01m,"Conversion de tasa efectiva anual");
        var invalido=Condicion("FRANCES");invalido.CantidadCuotas=0;Rechaza(()=>motor.Generar(invalido),"Cuotas cero");
        var quincenal=Condicion("ALEMAN");quincenal.Unidad="DIAS";quincenal.Intervalo=15;quincenal.PeriodosAnuales=24m;
        Check(motor.Generar(quincenal)[1].Fecha==quincenal.FechaPrimerPago.AddDays(15),"Periodicidad de 15 dias no deriva por meses");
        invalido=Condicion("SIMPLE");invalido.TipoTasa="EFECTIVA_ANUAL";Rechaza(()=>motor.Generar(invalido),"Simple no confunde tasa efectiva y nominal");
        var hash=FinanzasServicio.HashClave("Prueba8!");Check(FinanzasServicio.VerificarClave("Prueba8!",hash) && !FinanzasServicio.VerificarClave("incorrecta",hash),"Hash PBKDF2 verifica y rechaza clave incorrecta");
        var repo=new FinanzasRepositorio(conexion);var servicio=new FinanzasServicio(repo);
        int admin=servicio.CrearAdministrador("pruebas_admin","Administrador de pruebas","admin@example.test","Prueba8!");
        Check(servicio.IniciarSesion("pruebas_admin","Prueba8!")==admin,"Inicio de sesion real");
        Rechaza(()=>servicio.CrearAdministrador("otro","Otro","otro@example.test","Prueba8!"),"Alta inicial unica");
        Sql(conexion,"INSERT INTO Usuarios(NombreUsuario,NombreCompleto,Correo,ContrasenaHash) VALUES('consulta','Consulta','consulta@example.test','sin-acceso'); INSERT INTO UsuarioRoles SELECT LAST_INSERT_ID(),Id FROM Roles WHERE Codigo='CONSULTA';");
        int consulta=admin+1;
        Sql(conexion,"INSERT INTO Usuarios(NombreUsuario,NombreCompleto,Correo,ContrasenaHash) VALUES('oficial','Oficial','oficial@example.test','sin-acceso'); INSERT INTO UsuarioRoles SELECT LAST_INSERT_ID(),Id FROM Roles WHERE Codigo='OFICIAL_CREDITO'; INSERT INTO Usuarios(NombreUsuario,NombreCompleto,Correo,ContrasenaHash) VALUES('cajero','Cajero','cajero@example.test','sin-acceso'); INSERT INTO UsuarioRoles SELECT LAST_INSERT_ID(),Id FROM Roles WHERE Codigo='CAJERO';");
        int oficial=admin+2,cajero=admin+3;
        var producto=new ProductoFinanciero {Codigo="TEST",Nombre="Producto de pruebas",Descripcion="Solo pruebas aisladas",MontoMinimo=100m,MontoMaximo=10000m,CuotasMinimas=1,CuotasMaximas=120,MesesMinimos=0.01m,MesesMaximos=120m,TasaAnual=12m,TipoTasa="NOMINAL_ANUAL",CapitalizacionId=1};
        producto.Metodos.AddRange(new[]{1,2,3,4,5});producto.Periodicidades.Add(1);
        Rechaza(()=>servicio.GuardarProducto(producto,consulta),"Consulta no puede crear productos");
        Rechaza(()=>servicio.GuardarProducto(producto,cajero),"Cajero no puede configurar productos");
        int productoId=servicio.GuardarProducto(producto,admin);Check(productoId>0,"Alta de producto y tasa");
        producto.Id=productoId;producto.Metodos.Remove(5);servicio.GuardarProducto(producto,admin);
        Check(servicio.Consultar("PRODUCTO_METODOS",admin,productoId).Rows.Count==4,"Retira una asociacion aun no utilizada");
        producto.Metodos.Add(5);servicio.GuardarProducto(producto,admin);
        producto.CuotasMinimas=0;Rechaza(()=>servicio.GuardarProducto(producto,admin),"Producto con cuotas invalidas");producto.CuotasMinimas=1;
        var cliente=new Cliente {TipoPersona="NATURAL",TipoIdentificacion="CEDULA",Identificacion="8-123-456",Nombres="Ana",Apellidos="Prueba",FechaNacimiento=new DateTime(1990,5,1),Sexo="F",EstadoCivil="SOLTERO",Telefono="60000000",Correo="ana@example.test",Direccion="Calle de prueba",CodigoProvincia="08",CodigoDistrito="0801",CodigoCorregimiento="080105",Provincia="PANAMA",Distrito="PANAMA",Corregimiento="BELLA VISTA",Ocupacion="Analista",Ingresos=1500m,Referencias="Ana Perez 60000001"};
        int clienteId=new ClienteServicio(new ClienteRepositorio(conexion)).Guardar(cliente);
        var solicitud=new SolicitudFinanciera {Clave=Guid.NewGuid().ToString("N"),ClienteId=clienteId,ProductoId=productoId,MetodoId=1,PeriodicidadId=1,Monto=1000m,CantidadCuotas=3,FechaPrimerPago=ClienteServicio.HoyPanama.AddMonths(1),Observaciones="Prueba"};
        Check(servicio.Simular(solicitud,admin).Count==3,"Simulacion con condiciones de producto");
        int solicitudId=servicio.CrearSolicitud(solicitud,admin);Check(solicitudId>0,"Alta de solicitud");
        Check(servicio.CrearSolicitud(solicitud,admin)==solicitudId,"Reenviar solicitud no duplica el registro");
        solicitud.Monto=1100m;Rechaza(()=>servicio.CrearSolicitud(solicitud,admin),"Clave de solicitud con contenido diferente rechazada");solicitud.Monto=1000m;
        producto.Metodos.Remove(1);Rechaza(()=>servicio.GuardarProducto(producto,admin),"No retirar metodo con solicitud historica");producto.Metodos.Add(1);
        Rechaza(()=>servicio.Evaluar(solicitudId,1,"APROBADO",1000m,"",admin),"No aprobar sin evaluar");
        servicio.Evaluar(solicitudId,1,"EN_EVALUACION",0m,"",admin);
        Rechaza(()=>servicio.Evaluar(solicitudId,1,"APROBADO",1000m,"",admin),"Version de solicitud obsoleta");
        Rechaza(()=>servicio.Evaluar(solicitudId,2,"APROBADO",1001m,"",admin),"No aprobar mas del monto solicitado");
        Rechaza(()=>servicio.Evaluar(solicitudId,2,"APROBADO",99m,"",admin),"Monto aprobado respeta minimo del producto");
        Rechaza(()=>servicio.Evaluar(solicitudId,2,"RECHAZADO",0m,"",admin),"Rechazo requiere motivo");
        servicio.Evaluar(solicitudId,2,"APROBADO",1000m,"Evaluacion documentada",admin);
        producto.Id=productoId;producto.TasaAnual=24m;servicio.GuardarProducto(producto,admin);
        int prestamoId=servicio.Formalizar(solicitudId,3,solicitud.FechaPrimerPago,admin);
        var prestamo=servicio.Consultar("PRESTAMOS",admin,prestamoId).Rows[0];
        Check((decimal)prestamo["TasaInteres"]==12m,"Contrato conserva tasa pactada aunque cambie el producto");
        Check(servicio.Consultar("CUOTAS",admin,prestamoId).Rows.Count==3,"Cuotas persistidas");
        Rechaza(()=>servicio.Formalizar(solicitudId,3,solicitud.FechaPrimerPago,admin),"Formalizacion duplicada rechazada");
        solicitud.Clave=Guid.NewGuid().ToString("N");int rechazada=servicio.CrearSolicitud(solicitud,admin);servicio.Evaluar(rechazada,1,"EN_EVALUACION",0m,"",admin);servicio.Evaluar(rechazada,2,"RECHAZADO",0m,"No cumple la evaluacion",admin);
        Rechaza(()=>servicio.Formalizar(rechazada,3,solicitud.FechaPrimerPago,admin),"No formalizar solicitud rechazada");
        solicitud.Clave=Guid.NewGuid().ToString("N");int solicitudOficial=servicio.CrearSolicitud(solicitud,oficial);Check(solicitudOficial>0,"Oficial registra solicitud");
        Rechaza(()=>servicio.Evaluar(solicitudOficial,1,"EN_EVALUACION",0m,"",oficial),"Oficial no puede aprobar ni evaluar como administrador");
        Rechaza(()=>servicio.Formalizar(solicitudId,3,solicitud.FechaPrimerPago,oficial),"Oficial no puede desembolsar");
        var pago=new PagoFinanciero {PrestamoId=prestamoId,Monto=100m,Fecha=ClienteServicio.HoyPanama,Forma="EFECTIVO",Referencia="",Clave=Guid.NewGuid().ToString("N")};
        Rechaza(()=>servicio.RegistrarPago(pago,admin),"No cobrar cuotas futuras");
        // Fixture temporal: vencer todas las cuotas para probar cobranza, sin esperar meses.
        Sql(conexion,"UPDATE Cuotas SET FechaVencimiento=DATE(UTC_TIMESTAMP()-INTERVAL 5 HOUR)-INTERVAL 1 DAY WHERE PrestamoId="+prestamoId);
        Rechaza(()=>servicio.RegistrarPago(pago,consulta),"Consulta no puede registrar pagos");
        var paralelo1=System.Threading.Tasks.Task.Run(()=>servicio.RegistrarPago(pago,admin));
        var paralelo2=System.Threading.Tasks.Task.Run(()=>servicio.RegistrarPago(pago,admin));
        System.Threading.Tasks.Task.WaitAll(paralelo1,paralelo2);
        int recibo=paralelo1.Result;Check(recibo>0 && recibo==paralelo2.Result,"Dos envios concurrentes crean un unico pago parcial");
        var primerRecibo=servicio.Consultar("PAGOS",admin,prestamoId).Rows[0];
        Check(Convert.ToDecimal(primerRecibo["Interes"])==10m && Convert.ToDecimal(primerRecibo["Capital"])==90m,"Pago parcial aplica interes antes de capital");
        Check(servicio.RegistrarPago(pago,admin)==recibo,"Idempotencia: envio repetido no duplica recibo");
        pago.Monto=101m;Rechaza(()=>servicio.RegistrarPago(pago,admin),"Misma clave con importe distinto rechazada");
        var pagoCaja=new PagoFinanciero {PrestamoId=prestamoId,Monto=1m,Fecha=ClienteServicio.HoyPanama,Forma="EFECTIVO",Referencia="",Clave=Guid.NewGuid().ToString("N")};
        Check(servicio.RegistrarPago(pagoCaja,cajero)>0,"Cajero registra pago con su usuario");
        pago.Monto=999999m;pago.Clave=Guid.NewGuid().ToString("N");Rechaza(()=>servicio.RegistrarPago(pago,admin),"Sobrepago rechazado");
        pago.Monto=-1m;Rechaza(()=>servicio.RegistrarPago(pago,admin),"Pago negativo rechazado");
        pago.Monto=1.001m;Rechaza(()=>servicio.RegistrarPago(pago,admin),"Pago con fracciones de centavo rechazado");
        pago.Monto=10m;pago.Fecha=ClienteServicio.HoyPanama.AddDays(-1);Rechaza(()=>servicio.RegistrarPago(pago,admin),"Pago retroactivo rechazado");pago.Fecha=ClienteServicio.HoyPanama;
        pago.Forma="TRANSFERENCIA";Rechaza(()=>servicio.RegistrarPago(pago,admin),"Transferencia requiere referencia");pago.Forma="EFECTIVO";
        var filas=servicio.Consultar("CUOTAS",admin,prestamoId).AsEnumerable();
        pago.Monto=filas.Sum(x=>Convert.ToDecimal(x["TotalPendiente"]));pago.Clave=Guid.NewGuid().ToString("N");
        servicio.RegistrarPago(pago,admin);
        prestamo=servicio.Consultar("PRESTAMOS",admin,prestamoId).Rows[0];
        Check((decimal)prestamo["SaldoCapital"]==0m && (string)prestamo["EstadoCodigo"]=="CANCELADO","Cancelacion al completar las cuotas");
        Check(servicio.Consultar("PAGOS",admin,prestamoId).AsEnumerable().All(x=>Convert.ToDecimal(x["DiferenciaAplicacion"])==0m),"Recibos coinciden con desglose");
        var movimientos=servicio.Consultar("MOVIMIENTOS",admin,prestamoId);
        Check((string)movimientos.Rows[0]["Tipo"]=="DESEMBOLSO" && movimientos.AsEnumerable().All(x=>Convert.ToDecimal(x["SaldoAcumulado"])>=0m),"Movimientos ordenados desde desembolso sin saldos negativos");
        Check(Convert.ToDecimal(movimientos.Rows[movimientos.Rows.Count-1]["SaldoAcumulado"])==0m,"Estado de cuenta queda en cero");
        Console.WriteLine("TOTAL: "+casos+" pruebas financieras aprobadas en base aislada.");
    }
}
