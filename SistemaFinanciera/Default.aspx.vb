Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Text

Public Class _Default
    Inherits FinanzasPagina

    Private Shared ReadOnly CulturaPanama As CultureInfo = CultureInfo.GetCultureInfo("es-PA")

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If IsPostBack Then Return
        Try
            Dim datos = ServicioFinanzas.ObtenerResumenDashboard(Actor.Id)
            Dim indicadores = datos.Tables("Indicadores").Rows(0)
            Dim hoy = DateTime.UtcNow.AddHours(-5).Date

            litNombreUsuario.Text = PrimerNombre(Actor.Nombre)
            litFechaCorte.Text = hoy.ToString("dd MMM yyyy", CulturaPanama)
            ConfigurarAccionPrincipal()
            CargarIndicadores(indicadores)
            litChartMensual.Text = RenderGrafico(datos.Tables("PagosPorMes"), hoy, False)
            litChartAnual.Text = RenderGrafico(datos.Tables("PagosPorMes"), hoy, True)
            litPagosRecientes.Text = RenderPagosRecientes(datos.Tables("PagosRecientes"), hoy)
            litCuotasProximas.Text = RenderCuotasProximas(datos.Tables("CuotasProximas"))
            litAvataresCuotas.Text = RenderAvataresCuotas(datos.Tables("CuotasProximas"))
        Catch ex As Exception
            Diagnostics.Trace.TraceError("Panel financiero de inicio: {0}", ex.ToString())
            pnlResumenError.Visible = True
            MostrarValoresNoDisponibles()
        End Try
    End Sub

    Private Sub ConfigurarAccionPrincipal()
        If Actor.Puede("CAJERO") Then
            lnkAccionPrincipal.Text = "Registrar pago"
            lnkAccionPrincipal.NavigateUrl = ResolveUrl("~/Pagos/Pagos.aspx")
            lnkAccionPrincipal.Visible = True
        ElseIf Actor.Puede("ADMINISTRADOR", "OFICIAL_CREDITO") Then
            lnkAccionPrincipal.Text = "Nueva solicitud"
            lnkAccionPrincipal.NavigateUrl = ResolveUrl("~/Solicitudes/Solicitudes.aspx")
            lnkAccionPrincipal.Visible = True
        End If
    End Sub

    Private Sub CargarIndicadores(r As DataRow)
        Dim formatoNumerico As IFormatProvider = CulturaPanama
        Dim cartera = CDec(r("MontoDesembolsado"))
        Dim pagoSemana = CDec(r("PagosSemana"))
        Dim pagoSemanaAnterior = CDec(r("PagosSemanaAnterior"))
        Dim vencido = CDec(r("MontoVencido"))
        Dim capitalPendiente = CDec(r("CapitalPendiente"))
        Dim interesExigible = CDec(r("InteresExigible"))
        Dim totalCuotas72 = CDec(r("MontoCuotasProximas72Horas"))
        Dim totalCuotas10 = CDec(r("MontoCuotasProximas10Dias"))

        litCarteraTotal.Text = Moneda(cartera)
        litPrestamosVigentes.Text = CInt(r("PrestamosVigentes")).ToString("N0", formatoNumerico)
        litPagosSemana.Text = Moneda(pagoSemana)
        litTransaccionesSemana.Text = CInt(r("TransaccionesSemana")).ToString("N0", formatoNumerico)
        litVariacionPagos.Text = Variacion(pagoSemana, pagoSemanaAnterior)
        litPrestamosMora.Text = CInt(r("PrestamosEnMora")).ToString("N0", formatoNumerico)
        litMontoVencido.Text = Moneda(vencido)
        litIndiceMora.Text = "Índice de mora: " & If(cartera = 0D, 0D, vencido / cartera * 100D).ToString("N1", formatoNumerico) & "%"
        litSolicitudesPendientes.Text = CInt(r("SolicitudesPendientes")).ToString("N0", formatoNumerico)
        litMontoSolicitudesPendientes.Text = Moneda(CDec(r("MontoSolicitudesPendientes")))
        litInteresesMes.Text = Moneda(CDec(r("InteresesCobradosMes")))
        litCuotas72h.Text = CInt(r("CuotasProximas72Horas")).ToString("N0", formatoNumerico)
        litMontoCuotas72h.Text = Moneda(totalCuotas72)
        litTotalCuotas10Dias.Text = Moneda(totalCuotas10)

        Dim capitalDisplay = "B/. " & capitalPendiente.ToString("N2", formatoNumerico)
        Dim interesDisplay = "B/. " & interesExigible.ToString("N2", formatoNumerico)
        homeBalanceValue.InnerText = capitalDisplay
        homeBalanceValue.Attributes("data-capital") = capitalPendiente.ToString(CultureInfo.InvariantCulture)
        homeBalanceValue.Attributes("data-interest") = interesExigible.ToString(CultureInfo.InvariantCulture)
        homeBalanceValue.Attributes("data-capital-display") = capitalDisplay
        homeBalanceValue.Attributes("data-interest-display") = interesDisplay
        homeCapitalPart.Attributes("data-value") = capitalPendiente.ToString(CultureInfo.InvariantCulture)
        homeInterestPart.Attributes("data-value") = interesExigible.ToString(CultureInfo.InvariantCulture)
    End Sub

    Private Function Variacion(actual As Decimal, anterior As Decimal) As String
        If anterior = 0D Then
            Return "<span class='home-comparison is-neutral'>" & If(actual = 0D, "0.0%", "Sin base") & "</span> <span class='home-comparison-note'>vs. semana previa</span>"
        End If
        Dim porcentaje = (actual - anterior) / anterior * 100D
        Dim clase = If(porcentaje >= 0D, "is-positive", "is-negative")
        Dim signo = If(porcentaje > 0D, "+", "")
        Return "<span class='home-comparison " & clase & "'>" & signo & porcentaje.ToString("N1", CulturaPanama) & "%</span> <span class='home-comparison-note'>vs. semana previa</span>"
    End Function

    Private Function RenderGrafico(datos As DataTable, hoy As DateTime, anual As Boolean) As String
        Dim periodos As New List(Of DashboardPeriodo)()
        If anual Then
            For i = 4 To 0 Step -1
                Dim anio = hoy.Year - i
                periodos.Add(New DashboardPeriodo With {.Anio = anio, .Etiqueta = anio.ToString(CultureInfo.InvariantCulture)})
            Next
        Else
            Dim inicio = New Date(hoy.Year, hoy.Month, 1).AddMonths(-5)
            For i = 0 To 5
                Dim fecha = inicio.AddMonths(i)
                periodos.Add(New DashboardPeriodo With {.Anio = fecha.Year, .Mes = fecha.Month, .Etiqueta = fecha.ToString("MMM", CulturaPanama).ToUpperInvariant()})
            Next
        End If

        For Each fila As DataRow In datos.Rows
            For Each periodo In periodos
                If CInt(fila("Anio")) = periodo.Anio AndAlso (anual OrElse CInt(fila("Mes")) = periodo.Mes) Then
                    periodo.Importe += CDec(fila("Importe"))
                    Exit For
                End If
            Next
        Next

        Dim maximo = periodos.Max(Function(x) x.Importe)
        If maximo <= 0D Then Return "<p class='home-chart-empty'>Aún no hay pagos confirmados en este período.</p>"
        Dim pico = periodos.OrderByDescending(Function(x) x.Importe).First()
        Dim indicePico = periodos.IndexOf(pico)
        Dim variacionPico = ""
        If indicePico > 0 AndAlso periodos(indicePico - 1).Importe > 0D Then
            Dim cambio = (pico.Importe - periodos(indicePico - 1).Importe) / periodos(indicePico - 1).Importe * 100D
            variacionPico = " · " & If(cambio > 0D, "+", "") & cambio.ToString("N1", CulturaPanama) & "%"
        End If
        Dim html As New StringBuilder("<div class='home-bar-chart-grid' role='img' aria-label='Importe de pagos por período'>")
        For Each periodo In periodos
            Dim altura = Math.Max(3D, periodo.Importe / maximo * 100D)
            Dim clase = If(periodo Is pico, " is-peak", "")
            html.Append("<div class='home-bar-column" & clase & "' title='" & Global.System.Web.HttpUtility.HtmlAttributeEncode(periodo.Etiqueta & ": B/. " & periodo.Importe.ToString("N2", CulturaPanama)) & "'>")
            If periodo Is pico Then html.Append("<span class='home-bar-peak-badge'>Pico" & Server.HtmlEncode(variacionPico) & "</span>")
            html.Append("<span class='home-bar-value'>" & Server.HtmlEncode(periodo.Importe.ToString("N0", CulturaPanama)) & "</span>")
            html.Append("<div class='home-bar-track'><span class='home-bar-fill' style='height:" & altura.ToString("0.##", CultureInfo.InvariantCulture) & "%'></span></div>")
            html.Append("<span class='home-bar-label'>" & Server.HtmlEncode(periodo.Etiqueta) & "</span>")
            html.Append("</div>")
        Next
        html.Append("</div>")
        Return html.ToString()
    End Function

    Private Function RenderPagosRecientes(datos As DataTable, hoy As DateTime) As String
        If datos.Rows.Count = 0 Then Return "<p class='home-empty-state'>Todavía no hay pagos confirmados.</p>"
        Dim html As New StringBuilder()
        Dim colores = New String() {"is-rose", "is-blue", "is-amber", "is-violet", "is-teal"}
        Dim indice = 0
        For Each fila As DataRow In datos.Rows
            Dim nombre = Convert.ToString(fila("NombreCompleto"))
            Dim fecha = CDate(fila("FechaPago"))
            Dim fechaEtiqueta = If(fecha.Date = hoy, "Hoy · " & fecha.ToString("HH:mm", CulturaPanama), If(fecha.Date = hoy.AddDays(-1), "Ayer · " & fecha.ToString("HH:mm", CulturaPanama), fecha.ToString("dd MMM", CulturaPanama)))
            html.Append("<div class='home-payment-item' data-dashboard-search-row>")
            html.Append("<span class='home-payment-avatar " & colores(indice Mod colores.Length) & "'>" & Server.HtmlEncode(Iniciales(nombre)) & "</span>")
            html.Append("<span class='home-payment-person'><strong>" & Server.HtmlEncode(nombre) & "</strong><small>" & Server.HtmlEncode(Convert.ToString(fila("NumeroPrestamo"))) & " · " & Server.HtmlEncode(Convert.ToString(fila("Producto"))) & "</small></span>")
            html.Append("<span class='home-payment-amount'><strong>" & Moneda(CDec(fila("MontoPagado"))) & "</strong><small><i></i>" & Server.HtmlEncode(fechaEtiqueta) & " · Confirmado</small></span>")
            html.Append("</div>")
            indice += 1
        Next
        Return html.ToString()
    End Function

    Private Function RenderCuotasProximas(datos As DataTable) As String
        If datos.Rows.Count = 0 Then Return "<tr><td colspan='4' class='home-empty-table'>No hay cuotas pendientes en los próximos 10 días.</td></tr>"
        Dim html As New StringBuilder()
        For Each fila As DataRow In datos.Rows
            Dim dias = CInt(fila("DiasRestantes"))
            Dim estado = If(dias <= 2, "Urgente", If(dias <= 5, "Próxima", "Programada"))
            Dim clase = If(dias <= 2, "is-urgent", If(dias <= 5, "is-soon", "is-scheduled"))
            Dim fecha = CDate(fila("FechaVencimiento")).ToString("dd MMM", CulturaPanama)
            html.Append("<tr data-dashboard-search-row><td><strong>" & Server.HtmlEncode(Convert.ToString(fila("NombreCompleto"))) & "</strong><small>" & Server.HtmlEncode(Convert.ToString(fila("NumeroPrestamo"))) & " · Cuota " & CInt(fila("NumeroCuota")).ToString(CultureInfo.InvariantCulture) & "</small></td>")
            html.Append("<td>" & Server.HtmlEncode(If(dias = 0, "Hoy", If(dias = 1, "Mañana · " & fecha, dias.ToString(CultureInfo.InvariantCulture) & " días · " & fecha))) & "</td>")
            html.Append("<td class='home-due-amount'>" & Moneda(CDec(fila("TotalPendiente"))) & "</td><td><span class='home-due-status " & clase & "'>" & estado & "</span></td></tr>")
        Next
        Return html.ToString()
    End Function

    Private Function RenderAvataresCuotas(datos As DataTable) As String
        Dim html As New StringBuilder()
        Dim cantidad = 0
        Dim totalUrgentes = 0
        For Each fila As DataRow In datos.Rows
            If CInt(fila("DiasRestantes")) <= 2 Then totalUrgentes += 1
        Next
        For Each fila As DataRow In datos.Rows
            If CInt(fila("DiasRestantes")) > 2 Then Continue For
            If cantidad = 4 Then Exit For
            Dim nombre = Convert.ToString(fila("NombreCompleto"))
            Dim titulo = nombre & " · " & Moneda(CDec(fila("TotalPendiente")))
            html.Append("<span class='home-due-avatar home-due-avatar-" & (cantidad + 1).ToString(CultureInfo.InvariantCulture) & "' title='" & Global.System.Web.HttpUtility.HtmlAttributeEncode(titulo) & "'>" & Server.HtmlEncode(Iniciales(nombre)) & "</span>")
            cantidad += 1
        Next
        If cantidad = 0 Then Return "<span class='home-avatar-empty'>Sin cuotas en las próximas 72 horas</span>"
        If totalUrgentes > cantidad Then html.Append("<span class='home-due-avatar home-due-avatar-more'>+" & (totalUrgentes - cantidad).ToString(CultureInfo.InvariantCulture) & "</span>")
        Return html.ToString()
    End Function

    Private Function PrimerNombre(nombre As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return ""
        Return nombre.Trim().Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)(0)
    End Function

    Private Function Iniciales(nombre As String) As String
        Dim palabras = If(nombre, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If palabras.Length = 0 Then Return "CW"
        Return String.Concat(palabras.Take(2).Select(Function(x) x.Substring(0, 1))).ToUpperInvariant()
    End Function

    Private Function Moneda(valor As Decimal) As String
        Return "B/. " & valor.ToString("N2", CulturaPanama)
    End Function

    Private Sub MostrarValoresNoDisponibles()
        For Each control As Literal In New Literal() {litCarteraTotal, litPrestamosVigentes, litPagosSemana, litVariacionPagos, litTransaccionesSemana, litPrestamosMora, litMontoVencido, litIndiceMora, litSolicitudesPendientes, litMontoSolicitudesPendientes, litInteresesMes, litCuotas72h, litMontoCuotas72h, litTotalCuotas10Dias}
            control.Text = "—"
        Next
        litChartMensual.Text = "<p class='home-chart-empty'>Datos temporalmente no disponibles.</p>"
        litChartAnual.Text = "<p class='home-chart-empty'>Datos temporalmente no disponibles.</p>"
        litPagosRecientes.Text = "<p class='home-empty-state'>Datos temporalmente no disponibles.</p>"
        litCuotasProximas.Text = "<tr><td colspan='4' class='home-empty-table'>Datos temporalmente no disponibles.</td></tr>"
        litAvataresCuotas.Text = "<span class='home-avatar-empty'>Datos temporalmente no disponibles</span>"
    End Sub

    Private Class DashboardPeriodo
        Public Property Anio As Integer
        Public Property Mes As Integer
        Public Property Etiqueta As String
        Public Property Importe As Decimal
    End Class
End Class
