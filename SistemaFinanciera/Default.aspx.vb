Option Strict On
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Imports System.Globalization
Public Class _Default
    Inherits Page
    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If IsPostBack Then Return
        Try
            Dim config = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If config Is Nothing Then Throw New InvalidOperationException("Conexión no configurada")
            Dim servicio As New ClienteServicio(New ClienteRepositorio(config.ConnectionString))
            Dim clientes = servicio.Listar("", Nothing)
            litTotal.Text = clientes.Count.ToString()
            litActivos.Text = System.Linq.Enumerable.Count(clientes, Function(c) c.Activo).ToString()
            litNaturales.Text = System.Linq.Enumerable.Count(clientes, Function(c) c.TipoPersona = "NATURAL").ToString()
            litJuridicos.Text = System.Linq.Enumerable.Count(clientes, Function(c) c.TipoPersona = "JURIDICA").ToString()
            Dim actividad As New System.Text.StringBuilder()
            For Each c In clientes.OrderByDescending(Function(v) v.CreadoUtc).ThenByDescending(Function(v) v.Id).Take(4)
                Dim iniciales = String.Concat(c.NombreCompleto.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(Function(v) v.Substring(0, 1)))
                actividad.Append("<div class='activity-row'><span class='person-avatar'>" & Server.HtmlEncode(iniciales) & "</span><span><strong>" & Server.HtmlEncode(c.NombreCompleto) & "</strong><small>" & c.CreadoUtc.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("es-PA")) & " · " & Server.HtmlEncode(c.Estado) & "</small></span></div>")
            Next
            If clientes.Count = 0 Then actividad.Append("<p class='activity-empty'>Aún no hay clientes registrados. Comienza desde el directorio.</p>")
            litActividad.Text = actividad.ToString()
            Dim inicio = New Date(Date.UtcNow.Year, Date.UtcNow.Month, 1).AddMonths(-5)
            Dim valores = Enumerable.Range(0, 6).Select(Function(i) System.Linq.Enumerable.Count(clientes, Function(c) c.CreadoUtc >= inicio.AddMonths(i) AndAlso c.CreadoUtc < inicio.AddMonths(i + 1))).ToArray()
            Dim maximo = Math.Max(1, valores.Max())
            Dim grafico As New System.Text.StringBuilder("<div class='monthly-chart' role='img' aria-label='Clientes registrados en cada uno de los últimos seis meses'>")
            For i = 0 To 5
                grafico.Append("<div class='month-col'><span class='month-value'>" & valores(i).ToString() & "</span><div class='month-bar " & If(i = 5, "current", "") & "' style='height:" & (valores(i) / maximo * 82).ToString("0.##", CultureInfo.InvariantCulture) & "%'></div><span class='month-label'>" & inicio.AddMonths(i).ToString("MMM", CultureInfo.GetCultureInfo("es-PA")).ToUpperInvariant() & "</span></div>")
            Next
            grafico.Append("</div>")
            litGrafico.Text = grafico.ToString()
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Resumen de clientes: {0}", ex.ToString())
            pnlResumenError.Visible = True
            For Each control As Literal In New Literal() {litTotal, litActivos, litNaturales, litJuridicos}
                control.Text = "—"
            Next
            litActividad.Text = "<p>Información no disponible.</p>"
            litGrafico.Text = "<p>Información no disponible.</p>"
        End Try
    End Sub
End Class
