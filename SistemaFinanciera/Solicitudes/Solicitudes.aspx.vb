Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Linq
Imports SistemaFinanciera.Entities
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Public Class Solicitudes
    Inherits FinanzasPagina
    Private ReadOnly Property ServicioClientes As ClienteServicio
        Get
            Dim config = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If config Is Nothing Then Throw New InvalidOperationException("No está configurada la conexión del sistema.")
            Return New ClienteServicio(New ClienteRepositorio(config.ConnectionString))
        End Get
    End Property
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        mensaje.Text = String.Empty
        mensajeEvaluacion.Text = String.Empty
        pnlMensajeEvaluacion.Visible = False
        pnlMensajeCreacion.Visible = False
        mensajeCreacion.Text = String.Empty
        creacion.Visible = Actor.Puede("ADMINISTRADOR", "OFICIAL_CREDITO")
        headerNueva.Visible = creacion.Visible
        If modalAbierto.Value = "1" AndAlso creacion.Visible Then
            modalNuevaSolicitud.Attributes.Remove("hidden")
        Else
            modalNuevaSolicitud.Attributes("hidden") = "hidden"
        End If
        If Not IsPostBack Then Ejecutar(Sub()
            ViewState("ClaveSolicitud") = Guid.NewGuid().ToString("N")
            Lista(cliente, ServicioFinanzas.Consultar("CLIENTES", Actor.Id))
            Lista(producto, ServicioFinanzas.Consultar("PRODUCTOS_ACTIVOS", Actor.Id))
            CargarMensual()
            ReiniciarPlazo()
            metodo.Items.Clear()
            metodo.Items.Add(New ListItem("Seleccione primero un producto", ""))
            primerPago.Text = SistemaFinanciera.BL.ClienteServicio.HoyPanama.AddMonths(1).ToString("yyyy-MM-dd")
            primerPago.Attributes("min") = SistemaFinanciera.BL.ClienteServicio.HoyPanama.AddDays(1).ToString("yyyy-MM-dd")
            primerPago.Attributes("max") = SistemaFinanciera.BL.ClienteServicio.HoyPanama.AddYears(2).ToString("yyyy-MM-dd")
            Listar()
        End Sub)
        If Not IsPostBack AndAlso Session("SolicitudRegistrada") IsNot Nothing Then
            Dim registrada = CInt(Session("SolicitudRegistrada"))
            Session.Remove("SolicitudRegistrada")
            Notificar("Solicitud " & FinanzasServicio.NumeroSolicitudVisible(registrada) & " registrada correctamente. Puede crear una nueva solicitud.")
        End If
    End Sub
    Protected Overrides Sub OnPreRender(e As EventArgs)
        If modalAbierto.Value = "1" AndAlso creacion.Visible AndAlso Not String.IsNullOrWhiteSpace(mensaje.Text) Then
            pnlMensajeCreacion.Visible = True
            pnlMensajeCreacion.Attributes("class") = pnlMensaje.Attributes("class")
            mensajeCreacion.Text = mensaje.Text
            mensaje.Text = String.Empty
        ElseIf evaluacion.Visible AndAlso Not String.IsNullOrWhiteSpace(mensaje.Text) Then
            pnlMensajeEvaluacion.Visible = True
            pnlMensajeEvaluacion.Attributes("class") = pnlMensaje.Attributes("class")
            mensajeEvaluacion.Text = mensaje.Text
            mensaje.Text = String.Empty
            ClientScript.RegisterStartupScript(Me.GetType(), "confirmacionEvaluacion",
                "document.addEventListener('DOMContentLoaded',function(){var aviso=document.getElementById('" & pnlMensajeEvaluacion.ClientID & "');if(aviso){aviso.focus({preventScroll:true});aviso.scrollIntoView({block:'center'});}});", True)
        End If
        If pnlListadoSolicitudes.Visible AndAlso Not String.IsNullOrWhiteSpace(mensaje.Text) Then
            pnlMensaje.Attributes("tabindex") = "-1"
            pnlMensaje.Style("scroll-margin-top") = "150px"
            ClientScript.RegisterStartupScript(Me.GetType(), "confirmacionRegistro",
                "document.addEventListener('DOMContentLoaded',function(){var aviso=document.getElementById('" & pnlMensaje.ClientID & "');if(aviso){aviso.focus({preventScroll:true});aviso.scrollIntoView({block:'center'});}});", True)
        End If
        MyBase.OnPreRender(e)
    End Sub
    Private Sub Listar()
        Dim todas = ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id)
        CargarFiltro(filtroCliente, todas, "ClienteId", "Cliente", "Todos los clientes")
        CargarFiltro(filtroProducto, todas, "ProductoId", "Producto", "Todos los productos")

        Dim total = todas.Rows.Count
        Dim enEvaluacion = todas.AsEnumerable().Count(Function(r) CStr(r("EstadoCodigo")) = "EN_EVALUACION")
        Dim aprobadas = todas.AsEnumerable().Count(Function(r) CStr(r("EstadoCodigo")) = "APROBADO")
        Dim rechazadas = todas.AsEnumerable().Count(Function(r) CStr(r("EstadoCodigo")) = "RECHAZADO")
        filtroTotal.Text = total.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroEnEvaluacion.Text = enEvaluacion.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroAprobadas.Text = aprobadas.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroRechazadas.Text = rechazadas.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))

        Dim desde As DateTime = DateTime.MinValue
        Dim hasta As DateTime = DateTime.MinValue
        If Not String.IsNullOrWhiteSpace(filtroDesde.Text) AndAlso Not DateTime.TryParseExact(filtroDesde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, desde) Then Throw New ArgumentException("La fecha inicial no es válida.")
        If Not String.IsNullOrWhiteSpace(filtroHasta.Text) AndAlso Not DateTime.TryParseExact(filtroHasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, hasta) Then Throw New ArgumentException("La fecha final no es válida.")
        If Not String.IsNullOrWhiteSpace(filtroDesde.Text) AndAlso Not String.IsNullOrWhiteSpace(filtroHasta.Text) AndAlso desde.Date > hasta.Date Then Throw New ArgumentException("La fecha inicial no puede ser posterior a la fecha final.")

        Dim termino = If(filtroBusqueda.Text, String.Empty).Trim()
        Dim idCliente As Integer
        Dim idProducto As Integer
        Integer.TryParse(filtroCliente.SelectedValue, idCliente)
        Integer.TryParse(filtroProducto.SelectedValue, idProducto)
        Dim filtradas = todas.Clone()
        For Each fila As DataRow In todas.Rows
            Dim fechaSolicitud = Convert.ToDateTime(fila("FechaSolicitud"), CultureInfo.InvariantCulture).Date
            If filtroEstado.SelectedValue <> "" AndAlso CStr(fila("EstadoCodigo")) <> filtroEstado.SelectedValue Then Continue For
            If idCliente > 0 AndAlso CInt(fila("ClienteId")) <> idCliente Then Continue For
            If idProducto > 0 AndAlso CInt(fila("ProductoId")) <> idProducto Then Continue For
            If Not String.IsNullOrWhiteSpace(filtroDesde.Text) AndAlso fechaSolicitud < desde.Date Then Continue For
            If Not String.IsNullOrWhiteSpace(filtroHasta.Text) AndAlso fechaSolicitud > hasta.Date Then Continue For
            If termino <> "" AndAlso Not Contiene(CStr(fila("NumeroSolicitud")), termino) AndAlso Not Contiene(CStr(fila("Cliente")), termino) AndAlso Not Contiene(CStr(fila("Producto")), termino) Then Continue For
            filtradas.ImportRow(fila)
        Next
        Vincular(listado, filtradas)
        litConteo.Text = "Mostrando " & filtradas.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) & " de " & total.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) & " solicitudes"
        MarcarKpiActiva()
    End Sub

    Private Shared Function Contiene(valor As String, termino As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(valor) AndAlso valor.IndexOf(termino, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Shared Sub CargarFiltro(lista As DropDownList, datos As DataTable, campoId As String, campoNombre As String, etiquetaTodos As String)
        Dim actual = lista.SelectedValue
        lista.Items.Clear()
        lista.Items.Add(New ListItem(etiquetaTodos, ""))
        Dim ids As New HashSet(Of Integer)()
        For Each fila As DataRow In datos.Rows
            Dim id = CInt(fila(campoId))
            If ids.Add(id) Then lista.Items.Add(New ListItem(CStr(fila(campoNombre)), id.ToString(CultureInfo.InvariantCulture)))
        Next
        If lista.Items.FindByValue(actual) IsNot Nothing Then lista.SelectedValue = actual
    End Sub

    Private Sub MarcarKpiActiva()
        filtroTotal.CssClass = "client-kpi customer-metric request-kpi" & If(filtroEstado.SelectedValue = "", " is-selected", "")
        filtroEnEvaluacion.CssClass = "client-kpi customer-metric request-kpi" & If(filtroEstado.SelectedValue = "EN_EVALUACION", " is-selected", "")
        filtroAprobadas.CssClass = "client-kpi customer-metric request-kpi" & If(filtroEstado.SelectedValue = "APROBADO", " is-selected", "")
        filtroRechazadas.CssClass = "client-kpi customer-metric request-kpi" & If(filtroEstado.SelectedValue = "RECHAZADO", " is-selected", "")
    End Sub

    Protected Sub FiltrarPorKpi(sender As Object, e As CommandEventArgs) Handles filtroTotal.Command, filtroEnEvaluacion.Command, filtroAprobadas.Command, filtroRechazadas.Command
        Ejecutar(Sub()
            filtroEstado.SelectedValue = CStr(e.CommandArgument)
            Listar()
        End Sub)
    End Sub

    Protected Sub BuscarSolicitudes(sender As Object, e As EventArgs) Handles btnBuscar.Click
        Ejecutar(Sub() Listar())
    End Sub

    Protected Sub LimpiarFiltros(sender As Object, e As EventArgs) Handles btnLimpiar.Click
        Ejecutar(Sub()
            filtroBusqueda.Text = String.Empty
            filtroEstado.SelectedIndex = 0
            filtroDesde.Text = String.Empty
            filtroHasta.Text = String.Empty
            filtroCliente.SelectedIndex = 0
            filtroProducto.SelectedIndex = 0
            Listar()
        End Sub)
    End Sub

    Public Function TextoEstadoSolicitud(valor As Object) As String
        Select Case If(valor Is Nothing OrElse IsDBNull(valor), "", CStr(valor))
            Case "SOLICITUD" : Return "Solicitud"
            Case "EN_EVALUACION" : Return "En evaluación"
            Case "APROBADO" : Return "Aprobada"
            Case "RECHAZADO" : Return "Rechazada"
            Case Else : Return "Sin estado"
        End Select
    End Function

    Public Function ClaseEstadoSolicitud(valor As Object) As String
        Select Case If(valor Is Nothing OrElse IsDBNull(valor), "", CStr(valor))
            Case "SOLICITUD" : Return "request-status request-status--pending"
            Case "EN_EVALUACION" : Return "request-status request-status--review"
            Case "APROBADO" : Return "request-status request-status--approved"
            Case "RECHAZADO" : Return "request-status request-status--rejected"
            Case Else : Return "request-status"
        End Select
    End Function
    Protected Sub ProductoCambiado(sender As Object, e As EventArgs) Handles producto.SelectedIndexChanged
        AbrirModalSolicitud()
        Ejecutar(Sub()
            ReiniciarPlazo()
            CargarMensual()
            metodo.Items.Clear()
            metodo.Items.Add(New ListItem("Seleccione primero un producto", ""))
            If String.IsNullOrEmpty(producto.SelectedValue) Then Return
            Dim id = Seleccion(producto)
            Lista(metodo, ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id))
            Dim permitidas = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id)
            If permitidas.Rows.Count = 0 Then Throw New ArgumentException("El producto debe tener habilitada la periodicidad mensual.")
            Dim condicionesProducto = Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))
            monto.Attributes("min") = Dinero(condicionesProducto("MontoMinimo"))
            monto.Attributes("max") = Dinero(condicionesProducto("MontoMaximo"))
            Dim minimo = Math.Max(CInt(condicionesProducto("PlazoMinimo")), CInt(Math.Ceiling(CDec(condicionesProducto("PlazoMinimoMeses")))))
            Dim maximo = Math.Min(CInt(condicionesProducto("PlazoMaximo")), CInt(Math.Floor(CDec(condicionesProducto("PlazoMaximoMeses")))))
            minimo = Math.Max(1, minimo)
            maximo = Math.Min(120, maximo)
            If minimo > maximo Then Throw New ArgumentException("El producto no tiene plazos mensuales válidos. Revise sus límites en Productos.")
            cuotas.Items.Clear()
            cuotas.Items.Add(New ListItem("Seleccione el plazo", ""))
            For meses As Integer = minimo To maximo
                cuotas.Items.Add(New ListItem(meses.ToString() & " meses · " & meses.ToString() & " cuotas mensuales", meses.ToString(CultureInfo.InvariantCulture)))
            Next
            cuotas.Enabled = True
            ayudaPlazo.Text = "Plazos permitidos: " & minimo.ToString() & "–" & maximo.ToString() & " meses. Una cuota por mes."
            simulacion.Visible = False
        End Sub)
    End Sub
    Private Sub CargarMensual()
        Dim mensual = ServicioFinanzas.Consultar("PERIODICIDADES_PAGO", Actor.Id)
        periodo.Items.Clear()
        If mensual.Rows.Count = 0 Then Throw New InvalidOperationException("No está configurada la periodicidad mensual.")
        periodo.Items.Add(New ListItem("Mensual", Convert.ToString(mensual.Rows(0)("Id"), CultureInfo.InvariantCulture)))
        periodo.SelectedIndex = 0
        periodo.Enabled = False
    End Sub
    Private Sub ReiniciarPlazo()
        cuotas.Items.Clear()
        cuotas.Items.Add(New ListItem("Seleccione primero un producto", ""))
        cuotas.Enabled = False
        ayudaPlazo.Text = "El plazo determina la cantidad de cuotas mensuales."
    End Sub
    Private Function Datos() As SolicitudFinanciera
        Return New SolicitudFinanciera With {.Clave = CStr(ViewState("ClaveSolicitud")), .ClienteId = Seleccion(cliente), .ProductoId = Seleccion(producto), .MetodoId = Seleccion(metodo), .PeriodicidadId = Seleccion(periodo), .Monto = Numero(monto, "Monto"), .CantidadCuotas = Seleccion(cuotas), .FechaPrimerPago = Fecha(primerPago), .Observaciones = observaciones.Text.Trim()}
    End Function
    Protected Sub SimularClick(sender As Object, e As EventArgs) Handles simular.Click
        AbrirModalSolicitud()
        Ejecutar(Sub()
            Dim plan = ServicioFinanzas.Simular(Datos(), Actor.Id)
            Vincular(tablaPlan, plan)
            totales.Text = "Cuota máxima: B/. " & Dinero(plan.Max(Function(q) q.Cuota)) & "; intereses: B/. " & Dinero(plan.Sum(Function(q) q.Interes)) & "; total: B/. " & Dinero(plan.Sum(Function(q) q.Cuota))
            simulacion.Visible = True
        End Sub)
    End Sub
    Protected Sub CrearClick(sender As Object, e As EventArgs) Handles crear.Click
        AbrirModalSolicitud()
        Ejecutar(Sub()
            Dim id = ServicioFinanzas.CrearSolicitud(Datos(), Actor.Id)
            ' Redirigir después del guardado evita reenviar el POST al actualizar.
            ' El GET inicializa un formulario vacío y una clave nueva de idempotencia.
            Session("SolicitudRegistrada") = id
            Response.Redirect(ResolveUrl("~/Solicitudes/Solicitudes.aspx"), False)
            Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub
    Private Sub AbrirModalSolicitud()
        modalAbierto.Value = "1"
        If creacion.Visible Then modalNuevaSolicitud.Attributes.Remove("hidden")
    End Sub
    Protected Sub Elegir(sender As Object, e As GridViewCommandEventArgs) Handles listado.RowCommand
        If e.CommandName <> "Elegir" Then Return
        Ejecutar(Sub()
            Dim id = CInt(listado.DataKeys(CInt(e.CommandArgument)).Value)
            MostrarDetalle(id)
        End Sub)
    End Sub
    Private Shared Function TextoDato(valor As Object, Optional predeterminado As String = "No registrado") As String
        If valor Is Nothing OrElse Convert.IsDBNull(valor) Then Return predeterminado
        Dim texto = Convert.ToString(valor, CultureInfo.GetCultureInfo("es-PA"))
        Return If(String.IsNullOrWhiteSpace(texto), predeterminado, texto.Trim())
    End Function
    Private Shared Function InicialesPersona(nombre As String) As String
        Dim partes = If(nombre, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If partes.Length = 0 Then Return "CL"
        If partes.Length = 1 Then Return partes(0).Substring(0, Math.Min(2, partes(0).Length)).ToUpperInvariant()
        Return (partes(0).Substring(0, 1) & partes(1).Substring(0, 1)).ToUpperInvariant()
    End Function
    Private Sub MostrarDetalle(id As Integer)
        tablaDetallePlan.PageIndex = 0
        Dim r = Fila(ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id, id))
        Dim clienteDetalle = ServicioClientes.Obtener(CInt(r("ClienteId")))
        Dim perfilCliente = ServicioFinanzas.ObtenerPerfilCliente(Actor.Id, clienteDetalle.Id)
        ViewState("SolicitudId") = id : ViewState("SolicitudVersion") = CULng(r("Version"))
        seleccionada.Text = CStr(r("NumeroSolicitud")) & " · " & CStr(r("Cliente")) & " · " & CStr(r("EstadoCodigo")) & " · Tasa pactada: " & Convert.ToString(r("TasaPactada"), CultureInfo.InvariantCulture) & "% · Ingresos evaluados: B/. " & Dinero(r("IngresosEvaluados")) & " · Cuota máxima estimada: B/. " & Dinero(r("CuotaEstimada"))
        litDetalleNumero.Text = TextoDato(r("NumeroSolicitud"))
        litDetalleEstado.Text = TextoEstadoSolicitud(r("EstadoCodigo"))
        litDetalleNombre.Text = (TextoDato(clienteDetalle.Nombres, "") & " " & TextoDato(clienteDetalle.Apellidos, "")).Trim()
        If String.IsNullOrWhiteSpace(litDetalleNombre.Text) Then litDetalleNombre.Text = TextoDato(r("Cliente"))
        litDetalleIdentificacion.Text = TextoDato(clienteDetalle.TipoIdentificacion) & " · " & TextoDato(clienteDetalle.Identificacion)
        litInicialesCliente.Text = InicialesPersona(litDetalleNombre.Text)
        litTipoPersonaCabecera.Text = TextoDato(clienteDetalle.TipoPersona)
        litNacimiento.Text = If(clienteDetalle.FechaNacimiento.HasValue, clienteDetalle.FechaNacimiento.Value.ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-PA")), "No registrado")
        litTelefono.Text = TextoDato(clienteDetalle.Telefono)
        litCorreo.Text = TextoDato(clienteDetalle.Correo)
        litSexo.Text = TextoDato(clienteDetalle.Sexo)
        litEstadoCivil.Text = TextoDato(clienteDetalle.EstadoCivil)
        litOcupacion.Text = TextoDato(clienteDetalle.Ocupacion)
        litIngresos.Text = Dinero(clienteDetalle.Ingresos)
        litDireccion.Text = String.Join(", ", New String() {clienteDetalle.Direccion, clienteDetalle.Corregimiento, clienteDetalle.Distrito, clienteDetalle.Provincia}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
        If String.IsNullOrWhiteSpace(litDireccion.Text) Then litDireccion.Text = "No registrado"
        Dim referenciasRegistradas = perfilCliente.Tables("Referencias").AsEnumerable().Select(Function(ref) String.Join(" · ", New String() {TextoDato(ref("Nombre"), ""), TextoDato(ref("Parentesco"), ""), TextoDato(ref("Telefono"), "")}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))).Where(Function(s) Not String.IsNullOrWhiteSpace(s)).ToArray()
        litReferencias.Text = If(referenciasRegistradas.Length > 0, String.Join(" | ", referenciasRegistradas), TextoDato(clienteDetalle.Referencias))
        litFechaSolicitud.Text = FechaDeFila(r, "FechaSolicitud")
        litMontoAprobado.Text = If(r.Table.Columns.Contains("MontoAprobado") AndAlso Not r.IsNull("MontoAprobado") AndAlso CDec(r("MontoAprobado")) > 0D, "B/. " & Dinero(r("MontoAprobado")), "Pendiente de aprobación")
        litIngresosEvaluados.Text = Dinero(r("IngresosEvaluados"))
        litFechaEvaluacion.Text = FechaDeFila(r, "FechaEvaluacion")
        litObservacionesSolicitud.Text = TextoDato(r("Observaciones"))
        litMotivoRechazo.Text = TextoDato(r("MotivoRechazo"))
        litMontoSolicitado.Text = Dinero(r("MontoSolicitado"))
        litComision.Text = Dinero(r("Comision"))
        litGastos.Text = Dinero(r("GastosAsociados"))
        litNeto.Text = Dinero(Math.Max(0D, CDec(r("MontoSolicitado")) - CDec(r("Comision")) - CDec(r("GastosAsociados"))))
        litCantidadCuotas.Text = Convert.ToString(r("CantidadCuotas"), CultureInfo.GetCultureInfo("es-PA"))
        litMetodoResumen.Text = TextoDato(r("Metodo"))
        Dim tasaTexto = Convert.ToString(r("TasaPactada"), CultureInfo.GetCultureInfo("es-PA")) & "% " & TextoDato(r("TipoTasa"), "")
        litDetalleCondiciones.Text = "Producto: " & TextoDato(r("Producto")) & " · Método: " & TextoDato(r("Metodo")) & " · Periodicidad: " & TextoDato(PeriodicidadSolicitud(r)) & " · Tasa pactada: " & tasaTexto & " · Primer pago: " & FechaSolicitudTexto(r)
        aprobado.Text = Dinero(r("MontoSolicitado"))
        Dim condicionesAprobacion = Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, CInt(r("ProductoId"))))
        aprobado.Attributes("min") = Dinero(Math.Max(CDec(condicionesAprobacion("MontoMinimo")), CDec(r("Comision")) + CDec(r("GastosAsociados"))))
        aprobado.Attributes("max") = Dinero(Math.Min(CDec(r("MontoSolicitado")), CDec(condicionesAprobacion("MontoMaximo"))))
        Dim administrador = Actor.Puede("ADMINISTRADOR")
        aprobado.Enabled = administrador : motivo.Enabled = administrador
        evaluar.Visible = administrador : aprobar.Visible = administrador : rechazar.Visible = administrador
        evaluar.Enabled = CStr(r("EstadoCodigo")) = "SOLICITUD"
        aprobar.Enabled = CStr(r("EstadoCodigo")) = "EN_EVALUACION"
        rechazar.Enabled = aprobar.Enabled
        evaluacion.Visible = True
        pnlListadoSolicitudes.Visible = False
        pnlDetalleSolicitud.Visible = True

        Dim plan As List(Of CuotaPlan) = Nothing
        Try
            plan = GenerarPlanSolicitud(r)
        Catch ex As Exception
            Diagnostics.Trace.TraceWarning("No se pudo proyectar el calendario de la solicitud {0}: {1}", id, ex.Message)
        End Try
        If plan IsNot Nothing AndAlso plan.Count > 0 Then
            litIntereses.Text = Dinero(plan.Sum(Function(q) q.Interes))
            litTotalProgramado.Text = Dinero(plan.Sum(Function(q) q.Cuota))
            litCuotaMaxima.Text = Dinero(plan.Max(Function(q) q.Cuota))
            Vincular(tablaDetallePlan, plan)
        Else
            litIntereses.Text = "0.00" : litTotalProgramado.Text = Dinero(r("MontoSolicitado"))
            litCuotaMaxima.Text = Dinero(r("CuotaEstimada"))
            Vincular(tablaDetallePlan, New List(Of CuotaPlan)())
        End If
    End Sub
    Protected Sub CambiarPaginaDetalle(sender As Object, e As GridViewPageEventArgs) Handles tablaDetallePlan.PageIndexChanging
        Ejecutar(Sub()
            If ViewState("SolicitudId") Is Nothing Then Throw New InvalidOperationException("Seleccione una solicitud para consultar sus cuotas.")
            Dim r = Fila(ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id, CInt(ViewState("SolicitudId"))))
            Dim plan = GenerarPlanSolicitud(r)
            Dim ultimaPagina = Math.Max(0, (plan.Count - 1) \ tablaDetallePlan.PageSize)
            tablaDetallePlan.PageIndex = Math.Max(0, Math.Min(e.NewPageIndex, ultimaPagina))
            ' Solo se vinculan las cuotas: no se reinicia el monto ni el motivo de evaluación.
            Vincular(tablaDetallePlan, plan)
        End Sub)
    End Sub
    Private Function PeriodicidadSolicitud(r As DataRow) As String
        If r.Table.Columns.Contains("Periodicidad") Then Return TextoDato(r("Periodicidad"))
        Dim opciones = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id, CInt(r("PeriodicidadId")))
        Return If(opciones.Rows.Count = 1, TextoDato(opciones.Rows(0)("Nombre")), "Registrada")
    End Function
    Private Function GenerarPlanSolicitud(r As DataRow) As List(Of CuotaPlan)
        Dim periodicidad = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id, CInt(r("PeriodicidadId")))
        Dim capitalizacion = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id, CInt(r("CapitalizacionId")))
        Dim metodos = ServicioFinanzas.Consultar("METODOS", Actor.Id, CInt(r("MetodoId")))
        If periodicidad.Rows.Count <> 1 OrElse capitalizacion.Rows.Count <> 1 OrElse metodos.Rows.Count <> 1 Then Throw New InvalidOperationException("No están disponibles las condiciones históricas para proyectar esta solicitud.")
        Dim fechaPrimerPago = If(r.IsNull("FechaPrimerPago"), ClienteServicio.HoyPanama.AddMonths(1), Convert.ToDateTime(r("FechaPrimerPago"), CultureInfo.InvariantCulture))
        Dim condiciones As New CondicionesPrestamo With {
            .Monto = CDec(r("MontoSolicitado")),
            .TasaAnual = CDec(r("TasaPactada")),
            .TipoTasa = CStr(r("TipoTasa")),
            .CapitalizacionesAnuales = CDec(capitalizacion.Rows(0)("PeriodosPorAnio")),
            .PeriodosAnuales = CDec(periodicidad.Rows(0)("PeriodosPorAnio")),
            .Unidad = CStr(periodicidad.Rows(0)("Unidad")),
            .Intervalo = CInt(periodicidad.Rows(0)("Intervalo")),
            .CantidadCuotas = CInt(r("CantidadCuotas")),
            .FechaPrimerPago = fechaPrimerPago,
            .Metodo = CStr(metodos.Rows(0)("Codigo"))
        }
        Return New MotorPrestamos().Generar(condiciones)
    End Function
    Private Shared Function FechaSolicitudTexto(r As DataRow) As String
        Return FechaDeFila(r, "FechaPrimerPago")
    End Function
    Private Shared Function FechaDeFila(r As DataRow, campo As String) As String
        If Not r.Table.Columns.Contains(campo) OrElse r.IsNull(campo) Then Return "No registrada"
        Return Convert.ToDateTime(r(campo), CultureInfo.InvariantCulture).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-PA"))
    End Function
    Protected Sub VolverSolicitudes(sender As Object, e As EventArgs)
        pnlDetalleSolicitud.Visible = False
        pnlListadoSolicitudes.Visible = True
        evaluacion.Visible = False
        modalAbierto.Value = "0"
        ViewState.Remove("SolicitudId")
        ViewState.Remove("SolicitudVersion")
        Ejecutar(Sub() Listar())
    End Sub
    Protected Sub NuevaClick(sender As Object, e As EventArgs) Handles nueva.Click
        Response.Redirect("Solicitudes.aspx", True)
    End Sub
    Private Sub Decision(estado As String)
        Ejecutar(Sub()
            Dim id = CInt(ViewState("SolicitudId"))
            ServicioFinanzas.Evaluar(id, CULng(ViewState("SolicitudVersion")), estado, If(estado = "APROBADO", Numero(aprobado, "Monto aprobado"), 0D), motivo.Text.Trim(), Actor.Id)
            Listar()
            MostrarDetalle(id)
            Dim folio = FinanzasServicio.NumeroSolicitudVisible(id)
            Select Case estado
                Case "APROBADO"
                    Notificar("Solicitud " & folio & " aprobada correctamente. Ya está disponible para formalizar el préstamo.")
                Case "RECHAZADO"
                    Notificar("Solicitud " & folio & " rechazada. La decisión quedó registrada.")
                Case Else
                    Notificar("Se inició la evaluación de la solicitud " & folio & ".")
            End Select
        End Sub)
    End Sub
    Protected Sub EvaluarClick(sender As Object, e As EventArgs) Handles evaluar.Click
        Decision("EN_EVALUACION")
    End Sub
    Protected Sub AprobarClick(sender As Object, e As EventArgs) Handles aprobar.Click
        Decision("APROBADO")
    End Sub
    Protected Sub RechazarClick(sender As Object, e As EventArgs) Handles rechazar.Click
        Decision("RECHAZADO")
    End Sub
End Class
