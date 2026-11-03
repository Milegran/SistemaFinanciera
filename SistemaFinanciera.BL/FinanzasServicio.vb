Option Strict On
Imports System.Data
Imports System.Security.Cryptography
Imports System.Text.RegularExpressions
Imports SistemaFinanciera.Entities
Imports SistemaFinanciera.DAL

Public Class FinanzasServicio
    Public Const PatronCodigoProducto As String = "^[A-Z][A-Z0-9]*(?:[_-][A-Z0-9]+)*$"
    Public Const PatronNombreProducto As String = "^[\p{L}][\p{L}\p{M}0-9 '\u2019.,&()/+%\-]*$"
    Public Const PatronUsuarioNuevo As String = "^[A-Za-z][A-Za-z0-9._-]{2,19}$"
    Private ReadOnly repo As FinanzasRepositorio
    Private ReadOnly motor As New MotorPrestamos()
    Public Sub New(repositorio As FinanzasRepositorio)
        repo = repositorio
    End Sub
    Public Function Usuario(id As Integer) As UsuarioFinanzas
        Return repo.ObtenerUsuario(id)
    End Function
    Public Function Consultar(tipo As String, usuarioId As Integer, Optional id As Integer = 0) As DataTable
        Dim datos = repo.Consultar(tipo, usuarioId, id)
        If tipo = "SOLICITUDES" Then
            ' La clave persistida conserva la idempotencia; el folio visible usa el Id único.
            datos.Columns.Add("NumeroSolicitudInterno", GetType(String))
            For Each fila As DataRow In datos.Rows
                fila("NumeroSolicitudInterno") = fila("NumeroSolicitud")
                fila("NumeroSolicitud") = NumeroSolicitudVisible(CInt(fila("Id")))
            Next
        End If
        If tipo = "PRESTAMOS" Then
            For Each fila As DataRow In datos.Rows
                fila("NumeroPrestamo") = NumeroPrestamoVisible(CInt(fila("Id")))
            Next
        End If
        Return datos
    End Function
    Public Shared Function NumeroPrestamoVisible(id As Integer) As String
        If id <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(id))
        Return "PRE-" & id.ToString("D6", Globalization.CultureInfo.InvariantCulture)
    End Function
    Public Shared Function NumeroSolicitudVisible(id As Integer) As String
        If id <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(id))
        Return "SOL-" & id.ToString("D6", Globalization.CultureInfo.InvariantCulture)
    End Function
    Public Function ObtenerResumenDashboard(usuarioId As Integer) As DataSet
        Return repo.ObtenerResumenDashboard(usuarioId)
    End Function
    Public Function ObtenerPanelClientes(usuarioId As Integer) As DataSet
        Return repo.ObtenerPanelClientes(usuarioId)
    End Function
    Public Function ObtenerPerfilCliente(usuarioId As Integer, clienteId As Integer) As DataSet
        Return repo.ObtenerPerfilCliente(usuarioId, clienteId)
    End Function
    Public Function HayUsuarios() As Boolean
        Return repo.HayUsuarios()
    End Function
    Public Function RolesAdministracion(usuarioId As Integer) As DataTable
        Return repo.RolesAdministracion(usuarioId)
    End Function
    Public Function CatalogoAdministracion(usuarioId As Integer,codigo As String) As DataTable
        Return repo.CatalogoAdministracion(usuarioId,codigo)
    End Function
    Public Sub GuardarEstadosCatalogoAdministracion(usuarioId As Integer,codigo As String,estados As IDictionary(Of String,Boolean))
        If estados Is Nothing OrElse estados.Count=0 Then Throw New ArgumentException("Seleccione un catálogo editable.")
        repo.GuardarEstadosCatalogoAdministracion(usuarioId,If(codigo,""),estados)
    End Sub
    Public Function MetodosAdministracion(usuarioId As Integer) As DataTable
        Return repo.MetodosAdministracion(usuarioId)
    End Function
    Public Sub GuardarMetodoAdministracion(usuarioId As Integer,id As Integer,nombre As String,descripcion As String,formula As String,variables As String,algoritmo As String,ejemplo As String)
        If id<=0 Then Throw New ArgumentException("Seleccione un método de cálculo.")
        Texto(nombre,"Nombre del método",100)
        Texto(descripcion,"Descripción",500)
        Texto(formula,"Fórmula",8000,False)
        Texto(variables,"Variables",8000,False)
        Texto(algoritmo,"Algoritmo",8000,False)
        Texto(ejemplo,"Ejemplo manual",8000,False)
        repo.GuardarMetodoAdministracion(usuarioId,id,nombre.Trim(),descripcion.Trim(),If(formula,"").Trim(),If(variables,"").Trim(),If(algoritmo,"").Trim(),If(ejemplo,"").Trim())
    End Sub
    Public Sub CambiarEstadoMetodoAdministracion(usuarioId As Integer,id As Integer,activo As Boolean)
        repo.CambiarEstadoMetodoAdministracion(usuarioId,id,activo)
    End Sub
    Public Function ParametrosAdministracion(usuarioId As Integer) As DataTable
        Return repo.ParametrosAdministracion(usuarioId)
    End Function
    Public Sub GuardarParametrosAdministracion(usuarioId As Integer,valores As IDictionary(Of String,String))
        If valores Is Nothing OrElse valores.Count<>9 Then Throw New ArgumentException("No se recibió el conjunto completo de parámetros.")
        ValidarEnteroParametro(valores,"INTENTOS_MAXIMOS",1,20)
        ValidarEnteroParametro(valores,"BLOQUEO_MINUTOS",1,999)
        ValidarEnteroParametro(valores,"INACTIVIDAD_MINUTOS",1,999)
        ValidarEnteroParametro(valores,"CLAVE_LONGITUD_MINIMA",8,64)
        ValidarEnteroParametro(valores,"DIAS_GRACIA_MORA",0,365)
        ValidarDecimalParametro(valores,"PORCENTAJE_MAXIMO_CUOTA_INGRESOS",0.01D,100D)
        ValidarDecimalParametro(valores,"TASA_MORA_ANUAL",0D,100D)
        For Each codigo In {"CLAVE_MAYUSCULA","CLAVE_NUMERO"}
            If Not valores.ContainsKey(codigo) OrElse Not {"0","1"}.Contains(valores(codigo)) Then Throw New ArgumentException("Revise las reglas de contraseña.")
        Next
        repo.GuardarParametrosAdministracion(usuarioId,valores)
    End Sub
    Private Shared Sub ValidarEnteroParametro(valores As IDictionary(Of String,String),codigo As String,minimo As Integer,maximo As Integer)
        Dim numero As Integer
        If Not valores.ContainsKey(codigo) OrElse Regex.Replace(valores(codigo),"\D","").Length>3 OrElse Not Integer.TryParse(valores(codigo),Globalization.NumberStyles.None,Globalization.CultureInfo.InvariantCulture,numero) OrElse numero<minimo OrElse numero>maximo Then Throw New ArgumentException(codigo & ": indique hasta 3 dígitos y un valor entre " & minimo.ToString() & " y " & maximo.ToString() & ".")
    End Sub
    Private Shared Sub ValidarDecimalParametro(valores As IDictionary(Of String,String),codigo As String,minimo As Decimal,maximo As Decimal)
        Dim numero As Decimal
        If Not valores.ContainsKey(codigo) OrElse Regex.Replace(valores(codigo),"\D","").Length>3 OrElse Not Decimal.TryParse(valores(codigo),Globalization.NumberStyles.AllowDecimalPoint,Globalization.CultureInfo.InvariantCulture,numero) OrElse numero<minimo OrElse numero>maximo OrElse Decimal.Round(numero,4)<>numero Then Throw New ArgumentException(codigo & ": indique hasta 3 dígitos numéricos, entre " & minimo.ToString(Globalization.CultureInfo.InvariantCulture) & " y " & maximo.ToString(Globalization.CultureInfo.InvariantCulture) & ".")
    End Sub
    Public Function ResumenUsuariosAdministracion(usuarioId As Integer) As DataTable
        Return repo.ResumenUsuariosAdministracion(usuarioId)
    End Function
    Public Function ConsultarUsuariosAdministracion(usuarioId As Integer,busqueda As String,rolId As Integer,estado As String) As DataTable
        If rolId<0 OrElse Not {"TODOS","ACTIVO","INACTIVO"}.Contains(If(estado,"TODOS")) Then Throw New ArgumentException("Revise los filtros seleccionados.")
        Return repo.ConsultarUsuariosAdministracion(usuarioId,If(busqueda,""),rolId,If(estado,"TODOS"))
    End Function
    Public Sub GuardarUsuarioAdministracion(usuarioId As Integer,id As Integer,nombreUsuario As String,nombreCompleto As String,correo As String,rolId As Integer,activo As Boolean,clave As String)
        If id<0 Then Throw New ArgumentException("Usuario no válido.")
        If Not Regex.IsMatch(If(nombreUsuario,""),PatronUsuarioNuevo) Then Throw New ArgumentException("Usuario: use 3–20 caracteres, comience con letra y utilice letras, números, punto, guion o guion bajo.")
        Texto(nombreCompleto,"Nombre completo",160)
        If Not Regex.IsMatch(If(nombreCompleto,"" ).Trim().Normalize(),ClienteServicio.PatronNombre) OrElse Regex.Split(If(nombreCompleto,"" ).Trim(),"\s+").Length<2 Then Throw New ArgumentException("Escriba el nombre completo y al menos un apellido.")
        Try
            Dim mail As New System.Net.Mail.MailAddress(If(correo,""))
            If correo.Length>254 OrElse mail.Address<>correo OrElse Not mail.Host.Contains(".") Then Throw New ArgumentException("Correo inválido.")
        Catch ex As Exception When TypeOf ex Is FormatException OrElse TypeOf ex Is ArgumentException
            Throw New ArgumentException("Correo inválido.")
        End Try
        If id=0 AndAlso String.IsNullOrEmpty(clave) Then Throw New ArgumentException("La contraseña inicial es obligatoria.")
        Dim hash=""
        If Not String.IsNullOrEmpty(clave) Then
            ValidarClave(clave)
            hash=HashClave(clave)
        End If
        repo.GuardarUsuarioAdministracion(usuarioId,id,nombreUsuario.Trim(),nombreCompleto.Trim(),correo.Trim(),rolId,activo,hash)
    End Sub
    Public Sub CambiarEstadoUsuarioAdministracion(usuarioId As Integer,id As Integer,activo As Boolean)
        repo.CambiarEstadoUsuarioAdministracion(usuarioId,id,activo)
    End Sub
    Public Function RestablecerClaveUsuarioAdministracion(usuarioId As Integer,id As Integer) As String
        If id<=0 Then Throw New ArgumentException("Seleccione un usuario.")
        Dim bytes(47) As Byte
        Using rng=RandomNumberGenerator.Create()
            rng.GetBytes(bytes)
        End Using
        Dim temporal="Cw!7" & Convert.ToBase64String(bytes).Replace("+","A").Replace("/","b").Replace("=","9")
        repo.RestablecerClaveUsuarioAdministracion(usuarioId,id,HashClave(temporal))
        Return temporal
    End Function
    Public Sub RestablecerClaveManualUsuarioAdministracion(usuarioId As Integer,id As Integer,clave As String)
        If id<=0 Then Throw New ArgumentException("Seleccione un usuario.")
        ValidarClave(clave)
        repo.RestablecerClaveManualUsuarioAdministracion(usuarioId,id,HashClave(clave))
    End Sub
    Public Sub CambiarClavePropia(usuarioId As Integer,clave As String)
        ValidarClave(clave)
        repo.CambiarClavePropia(usuarioId,HashClave(clave))
    End Sub
    Public Function ConsultarAuditoria(usuarioId As Integer,usuarioFiltro As Integer,modulo As String,accion As String,desde As Nullable(Of Date),hasta As Nullable(Of Date)) As DataTable
        If usuarioFiltro<0 OrElse (desde.HasValue AndAlso hasta.HasValue AndAlso desde.Value.Date>hasta.Value.Date) Then Throw New ArgumentException("Revise los filtros y el rango de fechas.")
        Return repo.ConsultarAuditoria(usuarioId,usuarioFiltro,If(modulo,""),If(accion,""),desde,hasta)
    End Function
    Public Function DetalleAuditoria(usuarioId As Integer,id As Long) As DataTable
        If id<=0 Then Throw New ArgumentException("Seleccione un registro de auditoría.")
        Return repo.DetalleAuditoria(usuarioId,id)
    End Function
    Private Sub ValidarClave(clave As String,Optional maximo As Integer=128)
        Dim minimo=8,requiereMayuscula=True,requiereNumero=True
        Try
            For Each fila As DataRow In repo.PoliticaClave().Rows
                Select Case CStr(fila("Codigo"))
                    Case "CLAVE_LONGITUD_MINIMA" : Integer.TryParse(CStr(fila("Valor")),minimo)
                    Case "CLAVE_MAYUSCULA" : requiereMayuscula=CStr(fila("Valor"))="1"
                    Case "CLAVE_NUMERO" : requiereNumero=CStr(fila("Valor"))="1"
                End Select
            Next
        Catch ex As Exception
            Diagnostics.Trace.TraceWarning("No se pudo leer la política de contraseña; se aplican las reglas seguras predeterminadas: {0}",ex.Message)
        End Try
        If minimo<8 OrElse minimo>64 Then minimo=8
        If String.IsNullOrEmpty(clave) OrElse clave.Length<minimo OrElse clave.Length>maximo OrElse (requiereMayuscula AndAlso Not Regex.IsMatch(clave,"[A-Z]")) OrElse (requiereNumero AndAlso Not Regex.IsMatch(clave,"[0-9]")) Then Throw New ArgumentException("La contraseña debe tener " & minimo.ToString() & "–" & maximo.ToString() & " caracteres" & If(requiereMayuscula," e incluir mayúscula","") & If(requiereNumero," y número","") & ".")
    End Sub
    Public Shared Function HashClave(clave As String) As String
        Dim sal(15) As Byte
        Using rng = RandomNumberGenerator.Create()
            rng.GetBytes(sal)
        End Using
        Using pbkdf As New Rfc2898DeriveBytes(clave, sal, 210000, HashAlgorithmName.SHA256)
            Return "PBKDF2-SHA256$210000$" & Convert.ToBase64String(sal) & "$" & Convert.ToBase64String(pbkdf.GetBytes(32))
        End Using
    End Function
    Public Shared Function VerificarClave(clave As String, hash As String) As Boolean
        Try
            Dim partes = hash.Split("$"c)
            If partes.Length <> 4 OrElse partes(0) <> "PBKDF2-SHA256" Then Return False
            Dim iteraciones = Integer.Parse(partes(1), Globalization.CultureInfo.InvariantCulture)
            If iteraciones < 100000 OrElse iteraciones > 1000000 Then Return False
            Dim esperado = Convert.FromBase64String(partes(3))
            Using pbkdf As New Rfc2898DeriveBytes(clave, Convert.FromBase64String(partes(2)), iteraciones, HashAlgorithmName.SHA256)
                Dim actual = pbkdf.GetBytes(32), diferencia As Integer = esperado.Length Xor actual.Length
                For i = 0 To Math.Min(actual.Length, esperado.Length) - 1
                    diferencia = diferencia Or (actual(i) Xor esperado(i))
                Next
                Return diferencia = 0
            End Using
        Catch ex As FormatException
            Return False
        End Try
    End Function
    Public Function CrearAdministrador(nombre As String, completo As String, correo As String, clave As String) As Integer
        If Not Regex.IsMatch(If(nombre, ""), PatronUsuarioNuevo) Then Throw New ArgumentException("Usuario: 3–20 caracteres; empiece con una letra y use letras, números, puntos, guiones o guion bajo.")
        Texto(completo, "Nombre", 160)
        If Not Regex.IsMatch(If(completo, "").Trim().Normalize(), ClienteServicio.PatronNombre) Then Throw New ArgumentException("Nombre completo: use letras, acentos, espacios, apóstrofos, puntos o guiones; no números.")
        If Regex.Split(If(completo, "").Trim(), "\s+").Length < 2 Then Throw New ArgumentException("Nombre completo: escriba al menos un nombre y un apellido.")
        Dim mail As System.Net.Mail.MailAddress
        Try
            mail = New System.Net.Mail.MailAddress(correo)
        Catch ex As Exception When TypeOf ex Is FormatException OrElse TypeOf ex Is ArgumentException
            Throw New ArgumentException("Correo inválido.")
        End Try
        If correo.Length > 254 OrElse mail.Address <> correo OrElse Not mail.Host.Contains(".") Then Throw New ArgumentException("Correo inválido.")
        ValidarClave(clave,20)
        Return repo.CrearAdministrador(nombre, completo, correo, HashClave(clave))
    End Function
    Public Function IniciarSesion(nombre As String, clave As String) As Integer
        If String.IsNullOrWhiteSpace(nombre) OrElse nombre.Length > 254 OrElse String.IsNullOrEmpty(clave) OrElse clave.Length > 128 Then Throw New ArgumentException("Usuario o contraseña incorrectos.")
        Dim t = repo.Credencial(nombre.Trim())
        If t.Rows.Count = 0 Then
            ' Trabajo equivalente para no responder inmediatamente ante un usuario inexistente.
            HashClave(clave)
            Throw New ArgumentException("Usuario o contraseña incorrectos.")
        End If
        Dim r = t.Rows(0)
        Dim id = CInt(r("Id"))
        Dim claveValida = VerificarClave(clave, CStr(r("ContrasenaHash")))
        If Not IsDBNull(r("BloqueadoHastaUtc")) AndAlso CDate(r("BloqueadoHastaUtc")) > Date.UtcNow Then
            repo.RegistrarAccesoBloqueado(id)
            Throw New ArgumentException("Usuario o contraseña incorrectos.")
        End If
        Dim correcto = CBool(r("Activo")) AndAlso claveValida
        repo.ResultadoLogin(id, correcto)
        If Not correcto Then Throw New ArgumentException("Usuario o contraseña incorrectos.")
        repo.ObtenerUsuario(id)
        Return id
    End Function
    Public Shared Sub Texto(valor As String, nombre As String, maximo As Integer, Optional obligatorio As Boolean = True)
        If (obligatorio AndAlso String.IsNullOrWhiteSpace(valor)) OrElse If(valor, "").Length > maximo OrElse Regex.IsMatch(If(valor, ""), "[<>\x00-\x08\x0B\x0C\x0E-\x1F\x7F\p{Cf}]") Then Throw New ArgumentException(nombre & ": revise contenido y longitud (máximo " & maximo & ").")
    End Sub
    Public Shared Sub Importe(valor As Decimal, nombre As String, Optional permiteCero As Boolean = False)
        If valor < If(permiteCero, 0D, 0.01D) OrElse valor > 999999999999.99D OrElse MotorPrestamos.Dinero(valor) <> valor Then Throw New ArgumentException(nombre & ": use un importe válido con máximo dos decimales.")
    End Sub
    Public Sub CambiarEstadoProducto(id As Integer, activo As Boolean, usuarioId As Integer)
        If id <= 0 Then Throw New ArgumentException("Seleccione un producto.")
        repo.CambiarEstadoProducto(id, activo, usuarioId)
    End Sub
    Public Sub EliminarProducto(id As Integer, usuarioId As Integer)
        If id <= 0 Then Throw New ArgumentException("Seleccione un producto.")
        repo.EliminarProducto(id, usuarioId)
    End Sub
    Public Function GuardarProducto(p As ProductoFinanciero, usuarioId As Integer) As Integer
        If p Is Nothing Then Throw New ArgumentException("Producto requerido.")
        ValidarDatosProducto(p.Codigo, p.Nombre, p.Descripcion)
        p.Codigo = If(p.Codigo, "").Trim().ToUpperInvariant()
        Importe(p.MontoMinimo, "Monto mínimo") : Importe(p.MontoMaximo, "Monto máximo")
        Importe(p.Comision, "Comisión", True) : Importe(p.Gastos, "Gastos", True)
        If p.MontoMaximo < p.MontoMinimo Then Throw New ArgumentException("El monto máximo debe ser mayor o igual al mínimo.")
        If p.CuotasMinimas < 1 OrElse p.CuotasMaximas < p.CuotasMinimas OrElse p.CuotasMaximas > 120 OrElse p.MesesMinimos < 1 OrElse p.MesesMaximos < p.MesesMinimos OrElse p.MesesMaximos > 120 OrElse Decimal.Truncate(p.MesesMinimos) <> p.MesesMinimos OrElse Decimal.Truncate(p.MesesMaximos) <> p.MesesMaximos Then Throw New ArgumentException("Rango de plazo permitido: 1 a 120 meses, sin decimales. El máximo debe ser mayor o igual al mínimo.")
        If p.TasaAnual < 0 OrElse p.TasaAnual > 100 OrElse Decimal.Round(p.TasaAnual, 6) <> p.TasaAnual OrElse Not {"NOMINAL_ANUAL", "EFECTIVA_ANUAL"}.Contains(p.TipoTasa) OrElse p.CapitalizacionId <= 0 Then Throw New ArgumentException("Revise tasa anual (0–100 %, hasta seis decimales) y capitalización.")
        If FinanzasRepositorio.CalcularComision(p.TipoComision, p.Comision, p.MontoMinimo) + p.Gastos > p.MontoMinimo OrElse p.Comision > 99999999.99D OrElse p.Gastos > 99999999.99D Then Throw New ArgumentException("La comisión calculada y los gastos no pueden superar el monto mínimo.")
        If p.Metodos.Count = 0 OrElse p.Periodicidades.Count = 0 Then Throw New ArgumentException("Seleccione al menos un método y una periodicidad.")
        Return repo.GuardarProducto(p, usuarioId)
    End Function
    Public Function ListarTasas(usuarioId As Integer) As DataTable
        Return repo.Consultar("TASAS", usuarioId)
    End Function
    Public Function GuardarTasa(productoId As Integer, porcentaje As Decimal, tipoTasa As String, fechaDesde As Date, usuarioId As Integer) As Integer
        If productoId <= 0 Then Throw New ArgumentException("Seleccione un producto.")
        If porcentaje < 0 OrElse porcentaje > 100 OrElse Decimal.Round(porcentaje, 6) <> porcentaje Then Throw New ArgumentException("Revise la tasa (0–100 %, hasta seis decimales).")
        If Not {"NOMINAL_ANUAL", "EFECTIVA_ANUAL"}.Contains(tipoTasa) Then Throw New ArgumentException("Tipo de tasa inválido.")
        If fechaDesde = Date.MinValue Then Throw New ArgumentException("Indique la fecha de inicio de vigencia.")
        Return repo.GuardarTasa(productoId, porcentaje, tipoTasa, fechaDesde, usuarioId)
    End Function
    Public Shared Sub ValidarDatosProducto(codigo As String, nombre As String, descripcion As String)
        Dim normalizado = If(codigo, "").Trim().ToUpperInvariant()
        If normalizado.Length < 2 OrElse normalizado.Length > 20 OrElse Not Regex.IsMatch(normalizado, PatronCodigoProducto) Then Throw New ArgumentException("Código: 2–20 caracteres, empiece con una letra. Use letras, números y guiones entre componentes (ej.: PRE-01).")
        Texto(nombre, "Nombre de producto", 80)
        If nombre.Trim().Length < 3 OrElse Not Regex.IsMatch(nombre.Trim().Normalize(), PatronNombreProducto) Then Throw New ArgumentException("Nombre de producto: 3–80 caracteres, empiece con una letra. Se admiten números después y signos habituales como guiones, puntos o paréntesis.")
        TextoConLetras(descripcion, "Descripción", 500)
    End Sub
    Public Shared Sub TextoConLetras(valor As String, nombre As String, maximo As Integer, Optional obligatorio As Boolean = True)
        Texto(valor, nombre, maximo, obligatorio)
        If Not String.IsNullOrWhiteSpace(valor) AndAlso Not Regex.IsMatch(valor, "\p{L}") Then Throw New ArgumentException(nombre & ": incluya texto, no solo números o signos.")
    End Sub
    Public Function CrearSolicitud(s As SolicitudFinanciera, usuarioId As Integer) As Integer
        ValidarSolicitud(s)
        Return repo.CrearSolicitud(s, usuarioId, AddressOf motor.Generar)
    End Function
    Public Function Simular(s As SolicitudFinanciera, usuarioId As Integer) As List(Of CuotaPlan)
        ValidarSolicitud(s)
        Return repo.Simular(s, usuarioId, AddressOf motor.Generar)
    End Function
    Public Function SimularProducto(s As SolicitudFinanciera, usuarioId As Integer) As List(Of CuotaPlan)
        If s Is Nothing OrElse s.ProductoId <= 0 OrElse s.MetodoId <= 0 OrElse s.PeriodicidadId <= 0 OrElse s.CantidadCuotas < 1 OrElse s.CantidadCuotas > 120 Then Throw New ArgumentException("Seleccione producto, método, periodicidad y una cantidad de cuotas válida.")
        Importe(s.Monto, "Monto solicitado")
        If s.FechaPrimerPago <= ClienteServicio.HoyPanama OrElse s.FechaPrimerPago > ClienteServicio.HoyPanama.AddYears(2) Then Throw New ArgumentException("El primer pago debe ser futuro y estar dentro de los próximos dos años.")
        Return repo.SimularProducto(s, usuarioId, AddressOf motor.Generar)
    End Function
    Private Shared Sub ValidarSolicitud(s As SolicitudFinanciera)
        If s Is Nothing OrElse s.ClienteId <= 0 OrElse s.ProductoId <= 0 OrElse s.MetodoId <= 0 OrElse s.PeriodicidadId <= 0 OrElse s.CantidadCuotas < 1 OrElse s.CantidadCuotas > 120 Then Throw New ArgumentException("Seleccione cliente, producto, método, periodicidad y cantidad de cuotas.")
        Importe(s.Monto, "Monto solicitado")
        If Not String.IsNullOrEmpty(s.Clave) AndAlso Not Regex.IsMatch(s.Clave, "^[a-f0-9]{32}$") Then Throw New ArgumentException("Vuelva a abrir la solicitud.")
        TextoConLetras(s.Observaciones, "Observaciones", 1000, False)
        If s.FechaPrimerPago <= ClienteServicio.HoyPanama OrElse s.FechaPrimerPago > ClienteServicio.HoyPanama.AddYears(2) Then Throw New ArgumentException("El primer pago debe ser futuro y estar dentro de los próximos dos años.")
    End Sub
    Public Function Evaluar(id As Integer, version As ULong, accion As String, monto As Decimal, motivo As String, usuarioId As Integer) As Integer
        TextoConLetras(motivo, "Motivo", 1000, accion = "RECHAZADO")
        If accion = "APROBADO" Then Importe(monto, "Monto aprobado")
        Return repo.Evaluar(id, version, accion, monto, motivo, usuarioId)
    End Function
    Public Function Formalizar(id As Integer, version As ULong, fecha As Date, usuarioId As Integer) As Integer
        Return repo.Formalizar(id, version, fecha, usuarioId, AddressOf motor.Generar)
    End Function
    Public Function RegistrarPago(p As PagoFinanciero, usuarioId As Integer) As Integer
        If p Is Nothing OrElse p.PrestamoId <= 0 Then Throw New ArgumentException("Seleccione el préstamo.")
        Importe(p.Monto, "Monto pagado")
        Texto(p.Referencia, "Referencia", 100, False)
        If Not Regex.IsMatch(If(p.Clave, ""), "^[a-f0-9]{32}$") Then Throw New ArgumentException("Vuelva a abrir el formulario de pago.")
        If Not {"EFECTIVO", "TRANSFERENCIA", "CHEQUE", "TARJETA"}.Contains(p.Forma) Then Throw New ArgumentException("Forma de pago inválida.")
        If p.Forma <> "EFECTIVO" Then Texto(p.Referencia, "Referencia de la operación", 100)
        Return repo.RegistrarPago(p, usuarioId)
    End Function

    Public Function ConsultarReporte(numero As Integer, usuarioId As Integer, desde As Nullable(Of Date), hasta As Nullable(Of Date), clienteId As Integer, productoId As Integer, prestamoId As Integer) As DataTable
        If numero<1 OrElse numero>10 Then Throw New ArgumentException("Seleccione un reporte válido.")
        If clienteId<0 OrElse productoId<0 OrElse prestamoId<0 Then Throw New ArgumentException("Seleccione filtros válidos.")
        For Each fecha In New Nullable(Of Date)() {desde,hasta}
            If fecha.HasValue AndAlso (fecha.Value.Year<1900 OrElse fecha.Value.Year>2099) Then Throw New ArgumentException("Las fechas deben estar entre 1900 y 2099.")
        Next
        If desde.HasValue AndAlso hasta.HasValue AndAlso desde.Value.Date>hasta.Value.Date Then Throw New ArgumentException("La fecha inicial no puede ser posterior a la final.")
        If (numero=7 OrElse numero=8) AndAlso prestamoId=0 Then Throw New ArgumentException("Seleccione un préstamo para generar este reporte.")
        If numero=1 AndAlso (productoId<>0 OrElse prestamoId<>0) Then Throw New ArgumentException("El directorio de clientes se filtra por cliente y fecha de registro.")
        Return repo.ConsultarReporte(numero, usuarioId, desde, hasta, clienteId, productoId, prestamoId)
    End Function
End Class
