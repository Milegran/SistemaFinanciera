using System;
using System.Linq;
using System.Data;
using System.Collections.Generic;
using MySqlConnector;
using SistemaFinanciera.Entities;
using SistemaFinanciera.DAL;
using SistemaFinanciera.BL;

public class FinanzasTests : MarshalByRefObject {
    static int casos;
    static readonly Dictionary<string,string[]> Dorados=new Dictionary<string,string[]>{
        {"FRANCES",new[]{"cuota = if(i=0, P/n, P*i/(1-(1+i)^(-n)))","round(saldo*i)","if(k=n, saldo, min(saldo, round(cuota - interes)))",""}},
        {"ALEMAN",new[]{"","round(saldo*i)","if(k=n, saldo, min(saldo, round(P/n)))",""}},
        {"AMERICANO",new[]{"","round(P*i)","if(k=n, P, 0)",""}},
        {"SIMPLE",new[]{"total = round(P*r/100*n/f)","if(k=n, total - interes_acum, min(total - interes_acum, round(total/n)))","if(k=n, saldo, min(saldo, round(P/n)))",""}},
        {"COMPUESTO",new[]{"acumulado = round(P*pow(1+i,k))","if(k=n, acumulado - P, 0)","if(k=n, P, 0)","if(k=n, 0, acumulado)"}}
    };
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
        string fDefs="cuota = P/n", fInts="round(saldo*i)", fCaps="min(saldo, cuota)";
        foreach(var codigo in new[]{"F", "1PRE", "PRE-01", "PRE 01", "__X", new string('A',31)})
            Rechaza(()=>servicio.CrearMetodoAdministracion(1,codigo,"Metodo de pruebas","Descripcion de pruebas",fDefs,fInts,fCaps,"","","",""),"Rechaza codigo de metodo: "+codigo);
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"FRANCES","Reservado","Descripcion de pruebas",fDefs,fInts,fCaps,"","","",""),"Rechaza codigo nativo del motor");
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"METODO_OK","","Descripcion de pruebas",fDefs,fInts,fCaps,"","","",""),"Rechaza metodo sin nombre");
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"METODO_OK","Metodo","Descripcion de pruebas",fDefs,"",fCaps,"","","",""),"Rechaza sistema sin formula de interes");
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"METODO_OK","Metodo","Descripcion de pruebas",fDefs,fInts,"","","","",""),"Rechaza sistema sin formula de capital");
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"METODO_OK","Metodo","Descripcion de pruebas","interes = P/n",fInts,fCaps,"","","",""),"Rechaza definicion que sombra una variable reservada");
        Rechaza(()=>servicio.CrearMetodoAdministracion(1,"CAL_VACIO","Metodo","Descripcion de pruebas","",fInts,"0","","","",""),"La creacion exige un calendario que cierre con 1, 3 y 12 cuotas");
        var ctx=new Dictionary<string,decimal>(StringComparer.OrdinalIgnoreCase){
            {"p",1000m},{"i",0.01m},{"r",24m},{"n",12m},{"k",1m},{"saldo",1000m},{"f",12m},{"interes_acum",0m},{"capital_acum",0m}};
        Check(ExpresionFinanciera.Evaluar("2+3*4",ctx)==14m,"Aritmetica: multiplicacion antes que suma");
        Check(ExpresionFinanciera.Evaluar("0.1+0.2",ctx)==0.3m,"Decimal exacto sin error de coma flotante");
        Check(ExpresionFinanciera.Evaluar("2^3^2",ctx)==512m,"Potencia asociativa a la derecha");
        Check(ExpresionFinanciera.Evaluar("-2^2",ctx)==-4m,"Unario menos con precedencia menor que la potencia");
        Check(ExpresionFinanciera.Evaluar("2^-1",ctx)==0.5m,"Exponente negativo");
        Check(ExpresionFinanciera.Evaluar("(1<2)+(3<=3)+(4<>5)+(5>6)",ctx)==3m,"Comparaciones devuelven 1 o 0");
        Rechaza(()=>ExpresionFinanciera.Evaluar("1<2<3",ctx),"No admite comparaciones encadenadas");
        Check(ExpresionFinanciera.Evaluar("(1<2) and (3>2)",ctx)==1m,"and logico infijo");
        Check(ExpresionFinanciera.Evaluar("(1<2) or (3>4)",ctx)==1m,"or logico infijo");
        Check(ExpresionFinanciera.Evaluar("not(0)",ctx)==1m,"not logico");
        Check(ExpresionFinanciera.Evaluar("and(1,0)+or(0,1)",ctx)==1m,"and y or como funcion");
        Check(ExpresionFinanciera.Evaluar("if(1<2,10,20)+if(0,10,20)",ctx)==30m,"if perezoso con tres ramas");
        Check(ExpresionFinanciera.Evaluar("min(3,5)+max(3,5)+abs(-7)",ctx)==15m,"min max y abs");
        Check(ExpresionFinanciera.Evaluar("floor(2.7)+ceil(2.1)",ctx)==5m,"floor y ceil");
        Check(ExpresionFinanciera.Evaluar("pow(2,10)",ctx)==1024m,"pow");
        Check(ExpresionFinanciera.Evaluar("round(2.345)",ctx)==2.35m,"round aleja del cero");
        Check(ExpresionFinanciera.Evaluar("round(-2.345)",ctx)==-2.35m,"round negativo aleja del cero");
        Check(ExpresionFinanciera.Evaluar("10+5 # comentario",ctx)==15m,"Comentario con # hasta fin de linea");
        Check(ExpresionFinanciera.Evaluar("P*p",ctx)==1000000m,"Los identificadores no distinguen mayusculas");
        Check(ExpresionFinanciera.Evaluar("round(P*r/100/f)",ctx)==20m,"round con division exacta");
        Rechaza(()=>ExpresionFinanciera.Evaluar("1/(2-2)",ctx),"Division por cero rechazada en espanol");
        try { ExpresionFinanciera.Evaluar("1 + 0,5",ctx); throw new Exception("No rechazo la coma decimal"); }
        catch(ArgumentException ex) { Check(ex.Message.Contains("punto"),"La coma decimal explica usar el punto: "+ex.Message); }
        Rechaza(()=>ExpresionFinanciera.Evaluar("x+1",ctx),"Variable desconocida sin valor en el contexto");
        Rechaza(()=>ExpresionFinanciera.Evaluar("2+",ctx),"Expresion incompleta");
        Rechaza(()=>ExpresionFinanciera.Evaluar("(1+2",ctx),"Parentesis sin cerrar");
        Rechaza(()=>ExpresionFinanciera.Analizar("  "),"Formula vacia rechazada");
        Rechaza(()=>ExpresionFinanciera.Analizar("x+1",new HashSet<string>(StringComparer.OrdinalIgnoreCase){"p"}),"Analizar rechaza variable fuera del conjunto permitido");
        Rechaza(()=>ExpresionFinanciera.Evaluar(new string('1',2001),ctx),"Limite de 2000 caracteres por formula");
        Rechaza(()=>ExpresionFinanciera.Evaluar(new string('(',31)+"1"+new string(')',31),ctx),"Limite de profundidad 30");
        Rechaza(()=>ExpresionFinanciera.Evaluar(string.Join("+",Enumerable.Repeat("1",201)),ctx),"Limite de 200 elementos");
        Rechaza(()=>ExpresionFinanciera.Construir("","","",""),"Rechaza sistema sin formula de interes");
        Rechaza(()=>ExpresionFinanciera.Construir("","round(saldo*i)","",""),"Rechaza sistema sin formula de capital");
        var frD=Dorados["FRANCES"];
        var sistema=ExpresionFinanciera.Construir(frD[0],frD[1],frD[2],frD[3]);
        Check(sistema.VariablesDefinidas.Length==1 && sistema.VariablesDefinidas[0]=="cuota","El sistema expone sus variables definidas");
        var q1=sistema.CalcularCuota(Condicion("X"),0.01m,1,1000m,0m,0m);
        Check(q1.Interes==10m && q1.Capital>0 && q1.Saldo==1000m-q1.Capital,"CalcularCuota replica el interes frances: capital="+q1.Capital);
        var sinSaldo=ExpresionFinanciera.Construir("","round(saldo*i)","interes","");
        var q2=sinSaldo.CalcularCuota(Condicion("X"),0.01m,1,1000m,0m,0m);
        Check(q2.Saldo==990m,"Saldo vacio usa el predeterminado saldo - capital");
        Rechaza(()=>ExpresionFinanciera.Construir("","capital","saldo",""),"El interes no puede leer el capital");
        Check(ExpresionFinanciera.Construir("","round(saldo*i)","interes","")!=null,"El capital si puede leer el interes");
        Rechaza(()=>ExpresionFinanciera.Construir("a = b","round(a)","a",""),"Definicion que depende de otra posterior");
        Rechaza(()=>ExpresionFinanciera.Construir("a = a + 1","round(a)","a",""),"Definicion circular rechazada");
        Rechaza(()=>ExpresionFinanciera.Construir("interes = 5","round(saldo*i)","saldo",""),"No sombrea la variable interes");
        Rechaza(()=>ExpresionFinanciera.Construir("min = 5","round(saldo*i)","saldo",""),"No sombrea una funcion del sistema");
        Rechaza(()=>ExpresionFinanciera.Construir("9mala = 1","round(saldo*i)","saldo",""),"Nombre de variable invalido");
        Rechaza(()=>ExpresionFinanciera.Construir("cuota P/n","round(saldo*i)","saldo",""),"Definicion sin signo igual");
        Rechaza(()=>ExpresionFinanciera.Construir("a=1\na=2","round(a)","a",""),"Variable definida dos veces");
        var muchas=string.Join("\n",Enumerable.Range(1,31).Select(x=>"v"+x+" = 1"));
        Rechaza(()=>ExpresionFinanciera.Construir(muchas,"round(saldo*i)","saldo",""),"Limite de 30 definiciones");
        var negInt=ExpresionFinanciera.Construir("","-saldo","P","");
        Rechaza(()=>negInt.CalcularCuota(Condicion("X"),0.01m,1,1000m,0m,0m),"Interes negativo rechazado en la cuota");
        var negCap=ExpresionFinanciera.Construir("","round(saldo*i)","-P","");
        Rechaza(()=>negCap.CalcularCuota(Condicion("X"),0.01m,1,1000m,0m,0m),"Capital negativo rechazado en la cuota");
        var vista=servicio.ProbarSistemaExpresiones(frD[0],frD[1],frD[2],frD[3]);
        Check(vista.Rows.Count==12,"La vista previa entrega 12 cuotas");
        Check(vista.AsEnumerable().Sum(x=>Convert.ToDecimal(x["Capital"]))==1000m,"La vista previa amortiza el capital");
        Check(Convert.ToDecimal(vista.Rows[11]["Saldo"])==0m,"La vista previa cierra en cero");
        Rechaza(()=>servicio.ProbarSistemaExpresiones("","round(saldo*i)","0",""),"La vista previa rechaza un sistema que no amortiza");
        var motorD=new MotorPrestamos();
        var fixtures=new object[][]{new object[]{12m,3,1000m},new object[]{0m,12,1000m},new object[]{24m,12,5000m},new object[]{12m,120,0.01m},new object[]{12m,120,10000000m},new object[]{12m,1,1000m}};
        foreach(var par in Dorados) {
            foreach(var fx in fixtures) {
                decimal tasa=(decimal)fx[0]; int n=(int)fx[1]; decimal monto=(decimal)fx[2];
                var nativa=Condicion(par.Key); nativa.TasaAnual=tasa; nativa.CantidadCuotas=n; nativa.Monto=monto;
                var expresa=Condicion("EXPR_"+par.Key); expresa.TasaAnual=tasa; expresa.CantidadCuotas=n; expresa.Monto=monto;
                expresa.Definiciones=par.Value[0]; expresa.ExprInteres=par.Value[1]; expresa.ExprCapital=par.Value[2]; expresa.ExprSaldo=par.Value[3];
                var pn=motorD.Generar(nativa); var pe=motorD.Generar(expresa);
                string eti=par.Key+" tasa="+tasa+" n="+n+" monto="+monto;
                Check(pn.Count==pe.Count && pn.Count==n,"Diferencial "+eti+": misma cantidad de cuotas");
                bool igual=true; string donde="";
                for(int idx=0;idx<pn.Count;idx++) {
                    var a=pn[idx]; var b=pe[idx];
                    if(a.Numero!=b.Numero||a.Fecha!=b.Fecha||a.Capital!=b.Capital||a.Interes!=b.Interes||a.Cuota!=b.Cuota||a.Saldo!=b.Saldo) {
                        igual=false;
                        donde=" en la cuota "+(idx+1)+": nativo capital="+a.Capital+" interes="+a.Interes+" cuota="+a.Cuota+" saldo="+a.Saldo+" | expresion capital="+b.Capital+" interes="+b.Interes+" cuota="+b.Cuota+" saldo="+b.Saldo;
                        break;
                    }
                }
                Check(igual,"Diferencial "+eti+donde);
            }
            Check(true,"Diferencial "+par.Key+": identico al motor nativo en las seis fixtures");
        }
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
        var producto=new ProductoFinanciero {Codigo="TEST",Nombre="Producto de pruebas",Descripcion="Solo pruebas aisladas",MontoMinimo=100m,MontoMaximo=10000m,CuotasMinimas=1,CuotasMaximas=120,MesesMinimos=1m,MesesMaximos=120m,TasaAnual=12m,TipoTasa="NOMINAL_ANUAL",CapitalizacionId=1};
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
        var metodosAdmin=servicio.MetodosAdministracion(admin);
        Check(metodosAdmin.AsEnumerable().Count(x=>MotorPrestamos.CodigosNativos.Contains((string)x["Codigo"]))==5,"Los cinco metodos nativos permanecen en el catalogo");
        Check(metodosAdmin.AsEnumerable().Where(x=>MotorPrestamos.CodigosNativos.Contains((string)x["Codigo"])).All(x=>x.IsNull("ExprInteres")&&x.IsNull("ExprCapital")),"Los metodos nativos no declaran formulas propias");
        var frD=Dorados["FRANCES"];
        Rechaza(()=>servicio.CrearMetodoAdministracion(admin,"FRANCES","Duplicado","Descripcion de pruebas",frD[0],frD[1],frD[2],frD[3],"","",""),"Rechaza codigo de metodo ya registrado");
        Rechaza(()=>servicio.CrearMetodoAdministracion(admin,"NOCIERRA","No cierra","Sistema que no amortiza","","round(saldo*i)","0","","","",""),"La creacion exige un calendario que cierre con 1, 3 y 12 cuotas");
        int expresionId=servicio.CrearMetodoAdministracion(admin,"FRANCES_PROP","Frances con formulas propias","Sistema frances declarado con formulas propias",frD[0],frD[1],frD[2],frD[3],"C = P*i/(1-(1+i)^(-n))","Amortizacion frances","Igual al frances nativo");
        Check(expresionId>0,"Alta de metodo con formulas propias");
        var filaExpr=servicio.MetodosAdministracion(admin).AsEnumerable().First(x=>(string)x["Codigo"]=="FRANCES_PROP");
        Check((string)filaExpr["ExprInteres"]==frD[1] && (string)filaExpr["ExprCapital"]==frD[2] && Convert.ToBoolean(filaExpr["Activo"]),"El metodo con formulas guarda sus expresiones y nace activo");
        Rechaza(()=>servicio.CrearMetodoAdministracion(admin,"FRANCES_PROP","Otra vez","Descripcion de pruebas",frD[0],frD[1],frD[2],frD[3],"","",""),"Rechaza codigo duplicado en base de datos");
        producto.Metodos.Add(expresionId);servicio.GuardarProducto(producto,admin);
        Check(servicio.Consultar("PRODUCTO_METODOS",admin,productoId).Rows.Count==6,"Asociacion del metodo con formulas al producto");
        var solicitudExpr=new SolicitudFinanciera {Clave=Guid.NewGuid().ToString("N"),ClienteId=clienteId,ProductoId=productoId,MetodoId=expresionId,PeriodicidadId=1,Monto=1000m,CantidadCuotas=3,FechaPrimerPago=ClienteServicio.HoyPanama.AddMonths(1),Observaciones="Prueba formulas propias"};
        var solicitudBase=new SolicitudFinanciera {Clave=Guid.NewGuid().ToString("N"),ClienteId=clienteId,ProductoId=productoId,MetodoId=1,PeriodicidadId=1,Monto=1000m,CantidadCuotas=3,FechaPrimerPago=solicitudExpr.FechaPrimerPago,Observaciones="Prueba base"};
        var planExpr=servicio.Simular(solicitudExpr,admin);
        var planBase=servicio.Simular(solicitudBase,admin);
        Check(planExpr.Count==planBase.Count && planExpr.Zip(planBase,(a,b)=>a.Cuota==b.Cuota && a.Capital==b.Capital && a.Interes==b.Interes && a.Saldo==b.Saldo && a.Fecha==b.Fecha).All(x=>x),"Plan del sistema con formulas identico al frances nativo");
        int solicitudExprId=servicio.CrearSolicitud(solicitudExpr,admin);Check(solicitudExprId>0,"Alta de solicitud con metodo de formulas propias");
        servicio.Evaluar(solicitudExprId,1,"EN_EVALUACION",0m,"",admin);
        servicio.Evaluar(solicitudExprId,2,"APROBADO",1000m,"Sistema con formulas en evaluacion",admin);
        int prestamoExpr=servicio.Formalizar(solicitudExprId,3,solicitudExpr.FechaPrimerPago,admin);
        var cuotasExpr=servicio.Consultar("CUOTAS",admin,prestamoExpr);
        Check(cuotasExpr.Rows.Count==3 && cuotasExpr.AsEnumerable().Sum(x=>Convert.ToDecimal(x["Capital"]))==1000m,"Formalizacion con formulas propias calcula el calendario completo");
        Rechaza(()=>servicio.GuardarMetodoAdministracion(admin,expresionId,"Frances con formulas propias","Edicion que no cierra",frD[0],frD[1],"0",frD[3],"","",""),"La edicion exige formulas que cierren");
        servicio.GuardarMetodoAdministracion(admin,expresionId,"Frances con formulas propias","Sistema frances documentado",frD[0],frD[1],frD[2],frD[3],"C = P*i/(1-(1+i)^(-n))","Amortizacion frances","Igual al frances nativo");
        var editado=servicio.MetodosAdministracion(admin).AsEnumerable().First(x=>(string)x["Codigo"]=="FRANCES_PROP");
        Check((string)editado["Descripcion"]=="Sistema frances documentado" && (string)editado["ExprCapital"]==frD[2],"La edicion guarda ficha y formulas");
        servicio.GuardarMetodoAdministracion(admin,1,"Frances","Amortizacion frances con cuotas niveladas","borrar formulas","","","","C = P*i/(1-(1+i)^(-n))","Amortizacion nivelada","Igual al ejemplo manual");
        var nativoFila=servicio.MetodosAdministracion(admin).AsEnumerable().First(x=>(string)x["Codigo"]=="FRANCES");
        Check((string)nativoFila["Descripcion"]=="Amortizacion frances con cuotas niveladas" && nativoFila.IsNull("ExprInteres"),"La edicion del metodo nativo conserva sus formulas nulas");
        Console.WriteLine("TOTAL: "+casos+" pruebas financieras aprobadas en base aislada.");
    }
}
