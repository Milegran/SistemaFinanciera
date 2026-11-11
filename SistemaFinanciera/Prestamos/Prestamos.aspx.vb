Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Linq
Imports SistemaFinanciera.BL

Public Class Prestamos
    Inherits FinanzasPagina

    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        formalizacion.Visible = Actor.Puede("ADMINISTRADOR")
        If Not IsPostBack Then
            Ejecutar(Sub()
                Listar()
                desembolsar.Enabled = False
            End Sub)
        End If
    End Sub

    Private Sub Listar()
        Dim prestamos = ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id)
        ActualizarIndicadores(prestamos)

        Dim seleccionActual = solicitud.SelectedValue
        Dim solicitudes = ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id)
        Dim usadas = prestamos.AsEnumerable().Select(Function(r) CInt(r("SolicitudId"))).ToList()
        For Each r In solicitudes.AsEnumerable().Where(Function(x) CStr(x("EstadoCodigo")) <> "APROBADO" OrElse usadas.Contains(CInt(x("Id")))).ToList()
            solicitudes.Rows.Remove(r)
        Next
        solicitudes.Columns.Add("Etiqueta", GetType(String))
        For Each r As DataRow In solicitudes.Rows
            r("Etiqueta") = CStr(r("NumeroSolicitud")) & " · " & CStr(r("Cliente")) & " · B/. " & Dinero(r("MontoAprobado"))
        Next
        Lista(solicitud, solicitudes, "Id", "Etiqueta")
        If Not String.IsNullOrWhiteSpace(seleccionActual) AndAlso solicitud.Items.FindByValue(seleccionActual) IsNot Nothing Then
            solicitud.SelectedValue = seleccionActual
        End If

        Dim estadoFiltro = filtroEstado.SelectedValue
        Dim termino = filtroBusqueda.Text.Trim()
        Dim filtrados = prestamos.Clone()
        For Each prestamo As DataRow In prestamos.Rows
            Dim estado = Convert.ToString(prestamo("EstadoActual"), CultureInfo.InvariantCulture)
            Dim coincideEstado = estadoFiltro = "" OrElse
                (estadoFiltro = "VIGENTE" AndAlso EsVigente(estado)) OrElse
                (estadoFiltro = "EN_MORA" AndAlso estado = "EN_MORA") OrElse
                (estadoFiltro = "CERRADOS" AndAlso (estado = "CANCELADO" OrElse estado = "REFINANCIADO"))
            If Not coincideEstado Then Continue For

            If termino <> "" AndAlso
                Not Contiene(prestamo("NumeroPrestamo"), termino) AndAlso
                Not Contiene(prestamo("NombreCompleto"), termino) AndAlso
                Not Contiene(prestamo("Producto"), termino) AndAlso
                Not Contiene(prestamo("Metodo"), termino) Then Continue For
            filtrados.ImportRow(prestamo)
        Next

        Vincular(listado, filtrados)
        litConteo.Text = "Mostrando " & filtrados.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) &
                         " de " & prestamos.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) & " préstamos"
        MarcarKpiActiva()
    End Sub

    Private Sub ActualizarIndicadores(prestamos As DataTable)
        filtroTodos.Text = prestamos.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroVigentes.Text = prestamos.AsEnumerable().Count(Function(r) EsVigente(Convert.ToString(r("EstadoActual"), CultureInfo.InvariantCulture))).ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroMora.Text = prestamos.AsEnumerable().Count(Function(r) Convert.ToString(r("EstadoActual"), CultureInfo.InvariantCulture) = "EN_MORA").ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        filtroCancelados.Text = prestamos.AsEnumerable().Count(Function(r)
            Dim estado = Convert.ToString(r("EstadoActual"), CultureInfo.InvariantCulture)
            Return estado = "CANCELADO" OrElse estado = "REFINANCIADO"
        End Function).ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
    End Sub

    Private Sub MarcarKpiActiva()
        Dim seleccionado = Convert.ToString(ViewState("KpiPrestamo"), CultureInfo.InvariantCulture)
        MarcarKpi(filtroTodos, seleccionado = "TODOS")
        MarcarKpi(filtroVigentes, seleccionado = "VIGENTE")
        MarcarKpi(filtroMora, seleccionado = "EN_MORA")
        MarcarKpi(filtroCancelados, seleccionado = "CERRADOS")
    End Sub

    Private Shared Sub MarcarKpi(control As LinkButton, marcada As Boolean)
        control.CssClass = control.CssClass.Replace(" is-selected", "") & If(marcada, " is-selected", "")
        control.Attributes("aria-pressed") = If(marcada, "true", "false")
    End Sub

    Private Shared Function EsVigente(estado As String) As Boolean
        Return estado = "VIGENTE" OrElse estado = "DESEMBOLSADO"
    End Function

    Private Shared Function Contiene(valor As Object, termino As String) As Boolean
        Return valor IsNot Nothing AndAlso valor IsNot DBNull.Value AndAlso
               Convert.ToString(valor, CultureInfo.InvariantCulture).IndexOf(termino, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Protected Function ClaseEstadoPrestamo(valor As Object) As String
        Select Case Convert.ToString(valor, CultureInfo.InvariantCulture)
            Case "EN_MORA"
                Return "loan-state loan-state--late"
            Case "CANCELADO"
                Return "loan-state loan-state--closed"
            Case "REFINANCIADO"
                Return "loan-state loan-state--closed"
            Case Else
                Return "loan-state loan-state--active"
        End Select
    End Function

    Protected Function TextoEstadoPrestamo(valor As Object) As String
        Select Case Convert.ToString(valor, CultureInfo.InvariantCulture)
            Case "EN_MORA"
                Return "En mora"
            Case "CANCELADO"
                Return "Cancelado"
            Case "REFINANCIADO"
                Return "Refinanciado"
            Case "DESEMBOLSADO", "VIGENTE"
                Return "Vigente"
            Case Else
                Return Convert.ToString(valor, CultureInfo.InvariantCulture).Replace("_", " ")
        End Select
    End Function

    Protected Sub Cambiada(sender As Object, e As EventArgs) Handles solicitud.SelectedIndexChanged
        Ejecutar(Sub()
            Dim r = Fila(ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id, Seleccion(solicitud)))
            ViewState("Version") = CULng(r("Version"))
            desembolsar.Enabled = Actor.Puede("ADMINISTRADOR")
            Dim periodo = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id).AsEnumerable().Single(Function(x) CInt(x("Id")) = CInt(r("PeriodicidadId")))
            Dim capitalizacion = Fila(ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id, CInt(r("CapitalizacionId"))))
            Dim hoy = ClienteServicio.HoyPanama
            primerPago.Text = If(CStr(periodo("Unidad")) = "MESES", hoy.AddMonths(CInt(periodo("Intervalo"))), hoy.AddDays(CInt(periodo("Intervalo")))).ToString("yyyy-MM-dd")
            primerPago.Attributes("min") = primerPago.Text
            primerPago.Attributes("max") = primerPago.Text
            Dim montoAprobado = CDec(r("MontoAprobado"))
            Dim comision = CDec(r("Comision"))
            Dim gastos = CDec(r("GastosAsociados"))
            Dim tipoTasa = If(CStr(r("TipoTasa")) = "NOMINAL_ANUAL", "Nominal anual", "Efectiva anual")
            Dim plazoMeses = CDec(r("PlazoMeses")).ToString("0.##", CultureInfo.InvariantCulture)
            condiciones.Text = "<div class=""loan-condition-grid"">" &
                CampoCondicion("Producto", CStr(r("Producto"))) &
                CampoCondicion("Método de cálculo", CStr(r("Metodo"))) &
                CampoCondicion("Monto aprobado", "B/. " & Dinero(montoAprobado)) &
                CampoCondicion("Tasa contractual", CDec(r("TasaPactada")).ToString("0.######", CultureInfo.InvariantCulture) & "% · " & tipoTasa) &
                CampoCondicion("Capitalización", CStr(capitalizacion("Nombre"))) &
                CampoCondicion("Plazo", CStr(r("CantidadCuotas")) & " cuotas · " & plazoMeses & " meses") &
                CampoCondicion("Periodicidad", CStr(periodo("Nombre"))) &
                CampoCondicion("Primer pago", Convert.ToDateTime(primerPago.Text, CultureInfo.InvariantCulture).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-PA"))) &
                CampoCondicion("Comisión retenida", "B/. " & Dinero(comision)) &
                CampoCondicion("Gastos retenidos", "B/. " & Dinero(gastos)) &
                CampoCondicion("Neto a desembolsar", "B/. " & Dinero(montoAprobado - comision - gastos), "loan-condition-net") &
                "</div>"
        End Sub)
    End Sub

    Private Shared Function CampoCondicion(etiqueta As String, valor As String, Optional clase As String = "") As String
        Dim css = If(String.IsNullOrWhiteSpace(clase), "loan-condition-item", "loan-condition-item " & clase)
        Return "<div class=""" & css & """><small>" & System.Web.HttpUtility.HtmlEncode(etiqueta) & "</small><strong>" & System.Web.HttpUtility.HtmlEncode(valor) & "</strong></div>"
    End Function

    Protected Sub BuscarPrestamos(sender As Object, e As EventArgs) Handles btnBuscar.Click
        listado.PageIndex = 0
        If filtroEstado.SelectedValue = "" Then
            ViewState.Remove("KpiPrestamo")
        Else
            ViewState("KpiPrestamo") = filtroEstado.SelectedValue
        End If
        Ejecutar(Sub() Listar())
    End Sub

    Protected Sub LimpiarFiltros(sender As Object, e As EventArgs) Handles btnLimpiar.Click
        filtroBusqueda.Text = String.Empty
        filtroEstado.SelectedIndex = 0
        listado.PageIndex = 0
        ViewState.Remove("KpiPrestamo")
        Ejecutar(Sub() Listar())
    End Sub

    Protected Sub FiltrarEstado(sender As Object, e As CommandEventArgs)
        Dim estado = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture)
        filtroEstado.SelectedValue = If(estado = "TODOS", "", estado)
        ViewState("KpiPrestamo") = estado
        listado.PageIndex = 0
        Ejecutar(Sub() Listar())
    End Sub

    Protected Sub PaginarPrestamos(sender As Object, e As GridViewPageEventArgs)
        listado.PageIndex = e.NewPageIndex
        Ejecutar(Sub() Listar())
    End Sub

    Protected Sub DesembolsarClick(sender As Object, e As EventArgs) Handles desembolsar.Click
        Ejecutar(Sub()
            If ViewState("Version") Is Nothing Then Throw New ArgumentException("Seleccione la solicitud aprobada.")
            Dim id = ServicioFinanzas.Formalizar(Seleccion(solicitud), CULng(ViewState("Version")), Fecha(primerPago), Actor.Id)
            Listar() : condiciones.Text = "" : ViewState.Remove("Version")
            desembolsar.Enabled = False
            Notificar("Préstamo formalizado y desembolsado. Número " & FinanzasServicio.NumeroPrestamoVisible(id) & ". Selecciónelo para consultar el contrato y las cuotas.")
        End Sub)
    End Sub

    Protected Sub Ver(sender As Object, e As GridViewCommandEventArgs) Handles listado.RowCommand
        If e.CommandName = "Ver" Then Response.Redirect("DetallePrestamo.aspx?id=" & CInt(listado.DataKeys(CInt(e.CommandArgument)).Value).ToString(CultureInfo.InvariantCulture), True)
    End Sub
End Class
