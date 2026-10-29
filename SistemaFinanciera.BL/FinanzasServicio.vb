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
        Return repo.Consultar(tipo, usuarioId, id)
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
        If String.IsNullOrEmpty(clave) OrElse clave.Length < 8 OrElse clave.Length > 20 Then Throw New ArgumentException("La contraseña debe tener entre 8 y 20 caracteres.")
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
    Public Function GuardarProducto(p As ProductoFinanciero, usuarioId As Integer) As Integer
        If p Is Nothing Then Throw New ArgumentException("Producto requerido.")
        ValidarDatosProducto(p.Codigo, p.Nombre, p.Descripcion)
        p.Codigo = If(p.Codigo, "").Trim().ToUpperInvariant()
        Importe(p.MontoMinimo, "Monto mínimo") : Importe(p.MontoMaximo, "Monto máximo")
        Importe(p.Comision, "Comisión", True) : Importe(p.Gastos, "Gastos", True)
        If p.MontoMaximo < p.MontoMinimo OrElse p.CuotasMinimas < 1 OrElse p.CuotasMaximas < p.CuotasMinimas OrElse p.CuotasMaximas > 1200 OrElse p.MesesMinimos <= 0 OrElse p.MesesMaximos < p.MesesMinimos OrElse p.MesesMaximos > 1200 OrElse Decimal.Round(p.MesesMinimos, 2) <> p.MesesMinimos OrElse Decimal.Round(p.MesesMaximos, 2) <> p.MesesMaximos Then Throw New ArgumentException("Revise los rangos de monto, cuotas y meses (hasta 1200).")
        If p.TasaAnual < 0 OrElse p.TasaAnual > 100 OrElse Decimal.Round(p.TasaAnual, 6) <> p.TasaAnual OrElse Not {"NOMINAL_ANUAL", "EFECTIVA_ANUAL"}.Contains(p.TipoTasa) OrElse p.CapitalizacionId <= 0 Then Throw New ArgumentException("Revise tasa anual (0–100 %, hasta seis decimales) y capitalización.")
        If p.Comision + p.Gastos > p.MontoMinimo OrElse p.Comision > 99999999.99D OrElse p.Gastos > 99999999.99D Then Throw New ArgumentException("Los costos fijos no pueden superar el monto mínimo.")
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
        If s Is Nothing OrElse s.ProductoId <= 0 OrElse s.MetodoId <= 0 OrElse s.PeriodicidadId <= 0 OrElse s.CantidadCuotas < 1 OrElse s.CantidadCuotas > 1200 Then Throw New ArgumentException("Seleccione producto, método, periodicidad y una cantidad de cuotas válida.")
        Importe(s.Monto, "Monto solicitado")
        If s.FechaPrimerPago <= ClienteServicio.HoyPanama OrElse s.FechaPrimerPago > ClienteServicio.HoyPanama.AddYears(2) Then Throw New ArgumentException("El primer pago debe ser futuro y estar dentro de los próximos dos años.")
        Return repo.SimularProducto(s, usuarioId, AddressOf motor.Generar)
    End Function
    Private Shared Sub ValidarSolicitud(s As SolicitudFinanciera)
        If s Is Nothing OrElse s.ClienteId <= 0 OrElse s.ProductoId <= 0 OrElse s.MetodoId <= 0 OrElse s.PeriodicidadId <= 0 OrElse s.CantidadCuotas < 1 OrElse s.CantidadCuotas > 1200 Then Throw New ArgumentException("Seleccione cliente, producto, método, periodicidad y cantidad de cuotas.")
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
End Class
