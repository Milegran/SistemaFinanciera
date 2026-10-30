Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Text
Public Class Reportes
    Inherits FinanzasPagina
    Private Shared ReadOnly Nombres As String() = {"Clientes registrados", "Préstamos vigentes", "Préstamos cancelados", "Préstamos en mora", "Pagos por período", "Saldos pendientes", "Estado de cuenta", "Tabla de amortización", "Resumen de cartera", "Comparativo de métodos"}
    Private Shared ReadOnly Notas As String() = {
        "Directorio de clientes activos e inactivos. Las fechas corresponden al registro del cliente.",
        "Préstamos activos, incluidos los que tienen cuotas vencidas. Fechas por desembolso; saldos actuales.",
        "Préstamos cancelados. Las fechas corresponden a la cancelación.",
        "Una fila por cuota vencida. Las fechas corresponden al vencimiento; la mora ya forma parte del monto vencido.",
        "Pagos aplicados durante el período; excluye recibos anulados. Las fechas corresponden al pago.",
        "Saldos actuales de préstamos activos. Las fechas filtran el desembolso, no reconstruyen saldos históricos.",
        "Seleccione un préstamo. Las fechas filtran movimientos; el saldo acumulado incluye los movimientos anteriores al período.",
        "Seleccione un préstamo. Las fechas corresponden al vencimiento de cada cuota.",
        "Cartera desembolsada agrupada por producto y estado actual. Las fechas filtran el desembolso.",
        "Resultados de los métodos usados en la cartera desembolsada. Las fechas filtran el desembolso; los préstamos pueden tener montos y condiciones distintos."}
    Private Property Filtros As String()
        Get
            Return TryCast(ViewState("FiltrosReporte"), String())
        End Get
        Set(value As String())
            ViewState("FiltrosReporte") = value
        End Set
    End Property
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        mensaje.Text = ""
        If Not IsPostBack Then Ejecutar(Sub()
            For i=0 To Nombres.Length-1
                reporte.Items.Add(New ListItem((i+1).ToString("00") & " · " & Nombres(i),(i+1).ToString(CultureInfo.InvariantCulture)))
            Next
            Lista(cliente,ServicioFinanzas.Consultar("REPORTES_CLIENTES",Actor.Id)) : cliente.Items(0).Text="Todos los clientes"
            Lista(producto,ServicioFinanzas.Consultar("REPORTES_PRODUCTOS",Actor.Id)) : producto.Items(0).Text="Todos los productos"
            ActualizarOpciones()
            GenerarActual()
        End Sub)
    End Sub
    Private Function NumeroReporte() As Integer
        Dim n As Integer
        If Not Integer.TryParse(reporte.SelectedValue,n) OrElse n<1 OrElse n>10 Then Throw New ArgumentException("Seleccione un reporte.")
        Return n
    End Function
    Private Sub ActualizarOpciones()
        Dim n=NumeroReporte()
        descripcion.Text=Notas(n-1)
        producto.Enabled=n<>1 : prestamo.Enabled=n=7 OrElse n=8
        If n=1 Then producto.SelectedIndex=0
        prestamo.Items.Clear() : prestamo.Items.Add(New ListItem(If(prestamo.Enabled,"Seleccione un préstamo","No aplica"),"0"))
        If prestamo.Enabled Then
            Dim c=Integer.Parse(If(cliente.SelectedValue="","0",cliente.SelectedValue),CultureInfo.InvariantCulture)
            Dim p=Integer.Parse(If(producto.SelectedValue="","0",producto.SelectedValue),CultureInfo.InvariantCulture)
            For Each fila As DataRow In ServicioFinanzas.Consultar("PRESTAMOS",Actor.Id).Rows
                If (c=0 OrElse c=CInt(fila("ClienteId"))) AndAlso (p=0 OrElse p=CInt(fila("ProductoId"))) Then
                    prestamo.Items.Add(New ListItem(CStr(fila("NumeroPrestamo")) & " · " & CStr(fila("NombreCompleto")),CStr(fila("Id"))))
                End If
            Next
        End If
    End Sub
    Protected Sub TipoCambiado(sender As Object,e As EventArgs) Handles reporte.SelectedIndexChanged
        Ejecutar(Sub()
            desde.Text="" : hasta.Text=""
            If NumeroReporte()=5 Then
                Dim hoy=SistemaFinanciera.BL.ClienteServicio.HoyPanama
                desde.Text=New Date(hoy.Year,hoy.Month,1).ToString("yyyy-MM-dd") : hasta.Text=hoy.ToString("yyyy-MM-dd")
            End If
            resultados.Visible=False : Filtros=Nothing
            ActualizarOpciones()
        End Sub)
    End Sub
    Protected Sub FiltroCambiado(sender As Object,e As EventArgs) Handles cliente.SelectedIndexChanged,producto.SelectedIndexChanged
        Ejecutar(Sub()
            resultados.Visible=False : Filtros=Nothing
            ActualizarOpciones()
        End Sub)
    End Sub
    Private Shared Function FechaFiltro(texto As String) As Nullable(Of Date)
        If String.IsNullOrWhiteSpace(texto) Then Return Nothing
        Dim valor As Date
        If Not Date.TryParseExact(texto,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,valor) Then Throw New ArgumentException("Indique fechas válidas.")
        Return valor
    End Function
    Private Function Consultar(f As String()) As DataTable
        If f Is Nothing OrElse f.Length<>6 Then Throw New InvalidOperationException("Genere un reporte primero.")
        Return ServicioFinanzas.ConsultarReporte(Integer.Parse(f(0)),Actor.Id,FechaFiltro(f(1)),FechaFiltro(f(2)),Integer.Parse(f(3)),Integer.Parse(f(4)),Integer.Parse(f(5)))
    End Function
    Private Sub GenerarActual()
        resultados.Visible=False
        Dim f=New String() {NumeroReporte().ToString(),desde.Text,hasta.Text,If(cliente.SelectedValue="","0",cliente.SelectedValue),If(producto.Enabled AndAlso producto.SelectedValue<>"",producto.SelectedValue,"0"),If(prestamo.Enabled,prestamo.SelectedValue,"0")}
        Dim datos=Consultar(f)
        Filtros=f : tabla.PageIndex=0
        titulo.Text=Nombres(Integer.Parse(f(0))-1)
        filtrosAplicados.Text=cliente.SelectedItem.Text & " · " & If(producto.Enabled,producto.SelectedItem.Text,"Todos los tipos de persona") & " · " & If(f(1)="","Sin fecha inicial",f(1)) & " / " & If(f(2)="","Sin fecha final",f(2))
        If prestamo.Enabled Then filtrosAplicados.Text &= " · " & prestamo.SelectedItem.Text
        ViewState("DescripcionAplicada")=descripcion.Text
        Mostrar(datos)
    End Sub
    Private Sub Mostrar(datos As DataTable)
        tabla.Columns.Clear()
        For Each columna As DataColumn In datos.Columns
            Dim campo As New BoundField With {.DataField=columna.ColumnName,.HeaderText=columna.ColumnName,.HtmlEncode=True}
            If columna.DataType=GetType(DateTime) Then campo.DataFormatString="{0:dd/MM/yyyy}"
            If columna.DataType=GetType(Decimal) Then campo.DataFormatString=If(columna.ColumnName="Préstamos","{0:N0}","{0:N2}")
            tabla.Columns.Add(campo)
        Next
        tabla.PageIndex=Math.Min(tabla.PageIndex,Math.Max(0,(datos.Rows.Count-1) \ tabla.PageSize))
        Vincular(tabla,datos)
        cantidad.Text=datos.Rows.Count.ToString("N0")
        Dim n=Integer.Parse(Filtros(0))
        Dim columnaTotal=If(n=4,"Vencido",If(n=5,"Pagado",If(n=8,"Pendiente",If(n=3,"Capital","Saldo"))))
        etiquetaTotal.Text=If(n=1,"Clientes activos",If(n=7,"Saldo al último movimiento",If(n=4,"Monto vencido",If(n=5,"Pagos aplicados",If(n=8,"Cuotas pendientes",If(n=3,"Capital original","Saldo de capital"))))))
        If n=1 Then
            total.Text=datos.AsEnumerable().Count(Function(r) CStr(r("Estado"))="Activo").ToString("N0")
        Else
            Dim importe As Decimal=0D
            If n=7 Then
                If datos.Rows.Count>0 Then importe=CDec(datos.Rows(datos.Rows.Count-1)("Saldo"))
            Else
                importe=datos.AsEnumerable().Sum(Function(r) If(IsDBNull(r(columnaTotal)),0D,CDec(r(columnaTotal))))
            End If
            total.Text=If(n=7 AndAlso datos.Rows.Count=0,"Sin movimientos","B/. " & importe.ToString("N2",CultureInfo.GetCultureInfo("es-PA")))
        End If
        fechaGenerado.Text=DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm")
        Dim inicio=If(datos.Rows.Count=0,0,tabla.PageIndex*tabla.PageSize+1)
        conteo.Text="Mostrando " & inicio & "–" & Math.Min((tabla.PageIndex+1)*tabla.PageSize,datos.Rows.Count) & " de " & datos.Rows.Count & " registros"
        exportar.Enabled=datos.Rows.Count>0 : imprimir.Enabled=datos.Rows.Count>0
        resultados.Visible=True
    End Sub
    Protected Sub GenerarClick(sender As Object,e As EventArgs) Handles generar.Click
        Ejecutar(Sub() GenerarActual())
    End Sub
    Protected Sub LimpiarClick(sender As Object,e As EventArgs) Handles limpiar.Click
        Ejecutar(Sub()
            desde.Text="" : hasta.Text="" : cliente.SelectedIndex=0 : producto.SelectedIndex=0
            resultados.Visible=False : Filtros=Nothing
            ActualizarOpciones()
        End Sub)
    End Sub
    Protected Sub Paginar(sender As Object,e As GridViewPageEventArgs) Handles tabla.PageIndexChanging
        Ejecutar(Sub()
            Dim datos=Consultar(Filtros)
            tabla.PageIndex=Math.Max(0,e.NewPageIndex)
            Mostrar(datos)
        End Sub)
    End Sub
    Private Shared Function TextoCelda(valor As Object) As String
        If IsDBNull(valor) Then Return ""
        If TypeOf valor Is DateTime Then Return CDate(valor).ToString("dd/MM/yyyy")
        If TypeOf valor Is Decimal Then Return CDec(valor).ToString("0.00",CultureInfo.InvariantCulture)
        Return Convert.ToString(valor,CultureInfo.InvariantCulture)
    End Function
    Public Shared Function CeldaCsv(valor As Object) As String
        Dim texto=TextoCelda(valor)
        If TypeOf valor Is String Then
            Dim inicio=texto.TrimStart()
            If inicio.Length>0 AndAlso "=+-@".Contains(inicio.Substring(0,1)) Then texto="'" & texto
        End If
        Return ChrW(34) & texto.Replace(ChrW(34),ChrW(34) & ChrW(34)) & ChrW(34)
    End Function
    Protected Sub ExportarClick(sender As Object,e As EventArgs) Handles exportar.Click
        Ejecutar(Sub()
            Dim datos=Consultar(Filtros)
            Dim contenido As New StringBuilder()
            contenido.AppendLine(String.Join(";",datos.Columns.Cast(Of DataColumn)().Select(Function(c) CeldaCsv(c.ColumnName))))
            For Each fila As DataRow In datos.Rows
                contenido.AppendLine(String.Join(";",fila.ItemArray.Select(Function(v) CeldaCsv(v))))
            Next
            Response.Clear() : Response.ContentType="text/csv" : Response.ContentEncoding=Encoding.UTF8
            Response.AddHeader("Content-Disposition","attachment; filename=CapitalWest-reporte-" & Integer.Parse(Filtros(0)).ToString("00") & ".csv")
            Response.BinaryWrite(Encoding.UTF8.GetPreamble()) : Response.Write(contenido.ToString())
            Response.Flush() : Response.SuppressContent=True : Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub
    Protected Sub ImprimirClick(sender As Object,e As EventArgs) Handles imprimir.Click
        Ejecutar(Sub()
            Dim datos=Consultar(Filtros)
            Dim html As New StringBuilder("<!doctype html><html lang='es'><head><meta charset='utf-8'><title>CapitalWest · Reporte</title><style>body{font:12px Arial;color:#15382c;margin:25px}h1{font-size:22px}table{border-collapse:collapse;width:100%;font-size:10px}td,th{border-bottom:1px solid #ddd;padding:7px;text-align:left}th{background:#edf7f1}thead{display:table-header-group}tr{break-inside:avoid}button{padding:10px 20px;margin-bottom:15px}@media print{button{display:none}@page{size:landscape;margin:12mm}}</style></head><body><button onclick='window.print()'>Imprimir / Guardar PDF</button><h1>CapitalWest · ")
            html.Append(Server.HtmlEncode(Nombres(Integer.Parse(Filtros(0))-1)) & "</h1><p>" & Server.HtmlEncode(filtrosAplicados.Text) & "</p><p>" & Server.HtmlEncode(CStr(ViewState("DescripcionAplicada"))) & "</p><p>Generado: " & DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm") & " · Panamá · Importes en B/. · " & datos.Rows.Count & " registros</p><table><thead><tr>")
            For Each col As DataColumn In datos.Columns
                html.Append("<th>" & Server.HtmlEncode(col.ColumnName) & "</th>")
            Next
            html.Append("</tr></thead><tbody>")
            For Each fila As DataRow In datos.Rows
                html.Append("<tr>")
                For Each valor In fila.ItemArray
                    html.Append("<td>" & Server.HtmlEncode(TextoCelda(valor)) & "</td>")
                Next
                html.Append("</tr>")
            Next
            html.Append("</tbody></table></body></html>")
            Response.Clear() : Response.ContentType="text/html" : Response.ContentEncoding=Encoding.UTF8
            Response.Write(html.ToString()) : Response.Flush() : Response.SuppressContent=True : Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub
End Class
