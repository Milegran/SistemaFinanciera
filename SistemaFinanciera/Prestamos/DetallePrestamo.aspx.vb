Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Linq
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL

Public Class DetallePrestamo
    Inherits FinanzasPagina

    Private ReadOnly Property ServicioClientes As ClienteServicio
        Get
            Dim config = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If config Is Nothing Then Throw New InvalidOperationException("No está configurada la conexión del sistema.")
            Return New ClienteServicio(New ClienteRepositorio(config.ConnectionString))
        End Get
    End Property

    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then Ejecutar(Sub() CargarDetalle())
    End Sub

    Private Function IdPrestamo() As Integer
        Dim id As Integer
        If Not Integer.TryParse(Request.QueryString("id"), id) OrElse id <= 0 Then Throw New ArgumentException("Seleccione un préstamo desde la cartera.")
        Return id
    End Function

    Private Sub CargarDetalle()
        Dim id = IdPrestamo()
        Dim r = Fila(ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id, id))
        CargarCliente(CInt(r("ClienteId")))

        litNumeroPrestamo.Text = TextoDato(r("NumeroPrestamo"))
        litEstadoPrestamo.Text = EstadoPrestamo(r("EstadoActual"))
        litCapitalAprobado.Text = Dinero(r("MontoAprobado"))
        litCapitalPendiente.Text = Dinero(r("SaldoCapital"))
        litInteresesPendientes.Text = Dinero(r("InteresesProgramadosPendientes"))
        litMoraCargos.Text = "B/. " & Dinero(CDec(r("MoraPendiente")) + CDec(r("CargosPendientes")))
        litProducto.Text = TextoDato(r("Producto"))
        litMetodo.Text = TextoDato(r("Metodo"))
        litPeriodicidad.Text = NombrePeriodicidad(CInt(r("PeriodicidadId")))
        litCapitalizacion.Text = NombrePeriodicidad(CInt(r("CapitalizacionId")))
        litTasa.Text = Convert.ToDecimal(r("TasaInteres"), CultureInfo.InvariantCulture).ToString("0.######", CultureInfo.GetCultureInfo("es-PA")) & "% · " & TipoTasa(r("TipoTasa"))
        litCantidadCuotas.Text = Convert.ToString(r("CantidadCuotas"), CultureInfo.GetCultureInfo("es-PA"))
        litPlazo.Text = Convert.ToDecimal(r("PlazoMeses"), CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.GetCultureInfo("es-PA")) & " meses"
        litFechaAprobacion.Text = FechaDetalle(r, "FechaAprobacion")
        litFechaDesembolso.Text = FechaDetalle(r, "FechaDesembolso")
        litPrimerPago.Text = FechaDetalle(r, "FechaPrimerPago")
        litComision.Text = Dinero(r("Comision"))
        litGastos.Text = Dinero(r("GastosAsociados"))

        Dim tablaCuotas = ServicioFinanzas.Consultar("CUOTAS", Actor.Id, id)
        litBadgeCuotas.Text = tablaCuotas.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        Vincular(cuotas, tablaCuotas)
        Vincular(movimientos, ServicioFinanzas.Consultar("MOVIMIENTOS", Actor.Id, id))
    End Sub

    Private Sub CargarCliente(clienteId As Integer)
        Dim cliente = ServicioClientes.Obtener(clienteId)
        Dim perfil = ServicioFinanzas.ObtenerPerfilCliente(Actor.Id, clienteId)
        Dim nombre = (If(cliente.Nombres, "") & " " & If(cliente.Apellidos, "")).Trim()
        litNombreCliente.Text = TextoDato(nombre)
        litInicialesCliente.Text = Iniciales(nombre)
        litTipoPersona.Text = If(cliente.TipoPersona = "NATURAL", "Natural", "Jurídica")
        litIdentificacion.Text = TextoDato(cliente.TipoIdentificacion) & " · " & TextoDato(cliente.Identificacion)
        litNacimiento.Text = If(cliente.FechaNacimiento.HasValue, cliente.FechaNacimiento.Value.ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-PA")), "No registrado")
        litTelefono.Text = TextoDato(cliente.Telefono)
        litCorreo.Text = TextoDato(cliente.Correo)
        litSexo.Text = TextoDato(cliente.Sexo)
        litEstadoCivil.Text = TextoDato(cliente.EstadoCivil)
        litOcupacion.Text = TextoDato(cliente.Ocupacion)
        litIngresos.Text = Dinero(cliente.Ingresos)
        litDireccion.Text = String.Join(", ", New String() {cliente.Direccion, cliente.Corregimiento, cliente.Distrito, cliente.Provincia}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
        If String.IsNullOrWhiteSpace(litDireccion.Text) Then litDireccion.Text = "No registrado"
        Dim referencias = perfil.Tables("Referencias").AsEnumerable().Select(Function(f) String.Join(" · ", New String() {Convert.ToString(f("Nombre")), Convert.ToString(f("Parentesco")), Convert.ToString(f("Telefono"))}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))).Where(Function(s) Not String.IsNullOrWhiteSpace(s)).ToArray()
        litReferencias.Text = If(referencias.Length > 0, String.Join(" | ", referencias), TextoDato(cliente.Referencias))
    End Sub

    Protected Sub CambiarPaginaCuotas(sender As Object, e As GridViewPageEventArgs)
        Ejecutar(Sub()
            Dim datos = ServicioFinanzas.Consultar("CUOTAS", Actor.Id, IdPrestamo())
            cuotas.PageIndex = e.NewPageIndex
            Vincular(cuotas, datos)
        End Sub)
    End Sub

    Protected Sub CambiarPaginaMovimientos(sender As Object, e As GridViewPageEventArgs)
        Ejecutar(Sub()
            Dim datos = ServicioFinanzas.Consultar("MOVIMIENTOS", Actor.Id, IdPrestamo())
            movimientos.PageIndex = e.NewPageIndex
            Vincular(movimientos, datos)
        End Sub)
    End Sub

    Protected Sub VolverPrestamos(sender As Object, e As EventArgs)
        Response.Redirect(ResolveUrl("~/Prestamos/Prestamos.aspx"), True)
    End Sub

    Private Function NombrePeriodicidad(id As Integer) As String
        Dim opciones = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id, id)
        Return If(opciones.Rows.Count = 1, TextoDato(opciones.Rows(0)("Nombre")), "Registrada")
    End Function

    Private Shared Function FechaDetalle(fila As DataRow, campo As String) As String
        If Not fila.Table.Columns.Contains(campo) OrElse fila.IsNull(campo) Then Return "No registrada"
        Return Convert.ToDateTime(fila(campo), CultureInfo.InvariantCulture).ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-PA"))
    End Function

    Private Shared Function Iniciales(nombre As String) As String
        Dim partes = If(nombre, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If partes.Length = 0 Then Return "CL"
        If partes.Length = 1 Then Return partes(0).Substring(0, Math.Min(2, partes(0).Length)).ToUpperInvariant()
        Return (partes(0).Substring(0, 1) & partes(partes.Length - 1).Substring(0, 1)).ToUpperInvariant()
    End Function

    Private Shared Function TextoDato(valor As Object) As String
        If valor Is Nothing OrElse Convert.IsDBNull(valor) OrElse String.IsNullOrWhiteSpace(Convert.ToString(valor)) Then Return "No registrado"
        Return Convert.ToString(valor)
    End Function

    Private Shared Function EstadoPrestamo(valor As Object) As String
        Select Case Convert.ToString(valor)
            Case "VIGENTE" : Return "Vigente"
            Case "EN_MORA" : Return "En mora"
            Case "DESEMBOLSADO" : Return "Desembolsado"
            Case "CANCELADO" : Return "Cancelado"
            Case "REFINANCIADO" : Return "Refinanciado"
            Case "APROBADO" : Return "Aprobado"
            Case Else : Return TextoDato(valor)
        End Select
    End Function

    Private Shared Function TipoTasa(valor As Object) As String
        Select Case Convert.ToString(valor)
            Case "NOMINAL_ANUAL" : Return "Nominal anual"
            Case "EFECTIVA_ANUAL" : Return "Efectiva anual"
            Case Else : Return TextoDato(valor)
        End Select
    End Function
End Class
