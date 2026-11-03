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

    Private Shared Function NumeroCelda(fila As DataRow,columna As String) As Decimal
        If Not fila.Table.Columns.Contains(columna) OrElse IsDBNull(fila(columna)) Then Return 0D
        Return Convert.ToDecimal(fila(columna),CultureInfo.InvariantCulture)
    End Function

    Private Shared Function AbreviarImporte(valor As Decimal) As String
        If valor>=1000000D Then Return (valor/1000000D).ToString("0.#",CultureInfo.GetCultureInfo("es-PA")) & " M"
        If valor>=1000D Then Return (valor/1000D).ToString("0.#",CultureInfo.GetCultureInfo("es-PA")) & " k"
        Return valor.ToString("N0",CultureInfo.GetCultureInfo("es-PA"))
    End Function

    Private Shared Function EstadoLegible(codigo As String) As String
        Select Case If(codigo,"").Trim().ToUpperInvariant()
            Case "VIGENTE" : Return "Vigente"
            Case "EN_MORA" : Return "En mora"
            Case "DESEMBOLSADO" : Return "Desembolsado"
            Case "CANCELADO" : Return "Cancelado"
            Case Else : Return If(codigo,"Sin dato").Replace("_"," ")
        End Select
    End Function

    Private Shared Function TextoCeldaGrafica(fila As DataRow,columna As String) As String
        If Not fila.Table.Columns.Contains(columna) OrElse IsDBNull(fila(columna)) Then Return "Sin dato"
        Return Convert.ToString(fila(columna),CultureInfo.CurrentCulture)
    End Function

    Private Function DatosGrafica(numero As Integer,datos As DataTable) As List(Of KeyValuePair(Of String,Decimal))
        Dim grupos As New SortedDictionary(Of String,Decimal)(StringComparer.Ordinal)
        Dim etiquetas As New Dictionary(Of String,String)(StringComparer.Ordinal)
        Dim serie As New List(Of KeyValuePair(Of String,Decimal))()
        Dim agrupada=numero<>7
        For i=0 To datos.Rows.Count-1
            Dim fila=datos.Rows(i)
            Dim clave As String, etiqueta As String, valor As Decimal
            Select Case numero
                Case 1
                    etiqueta=TextoCeldaGrafica(fila,"Estado") : clave=etiqueta : valor=1D
                Case 2
                    etiqueta=TextoCeldaGrafica(fila,"Producto") : clave=etiqueta : valor=NumeroCelda(fila,"Saldo")
                Case 3
                    etiqueta=TextoCeldaGrafica(fila,"Producto") : clave=etiqueta : valor=NumeroCelda(fila,"Capital")
                Case 4
                    etiqueta=TextoCeldaGrafica(fila,"Préstamo") : clave=etiqueta : valor=NumeroCelda(fila,"Vencido")
                Case 5
                    Dim fecha=CDate(fila("Fecha"))
                    clave=fecha.ToString("yyyy-MM",CultureInfo.InvariantCulture)
                    etiqueta=fecha.ToString("MMM yyyy",CultureInfo.GetCultureInfo("es-PA"))
                    valor=NumeroCelda(fila,"Pagado")
                Case 6
                    etiqueta=TextoCeldaGrafica(fila,"Préstamo") : clave=etiqueta : valor=NumeroCelda(fila,"Saldo")
                Case 7
                    Dim fecha=CDate(fila("Fecha"))
                    etiqueta=fecha.ToString("dd/MM") & " · " & TextoCeldaGrafica(fila,"Tipo")
                    clave=i.ToString("D8",CultureInfo.InvariantCulture) : valor=NumeroCelda(fila,"Saldo")
                Case 8
                    Dim cuota=CInt(NumeroCelda(fila,"Cuota"))
                    clave=cuota.ToString("D4",CultureInfo.InvariantCulture) : etiqueta="Cuota " & cuota.ToString(CultureInfo.InvariantCulture)
                    valor=NumeroCelda(fila,"Programado")
                Case 9
                    etiqueta=TextoCeldaGrafica(fila,"Producto") : clave=etiqueta : valor=NumeroCelda(fila,"Saldo")
                Case 10
                    etiqueta=TextoCeldaGrafica(fila,"Método") : clave=etiqueta : valor=NumeroCelda(fila,"Capital")
                Case Else
                    Return serie
            End Select
            If agrupada Then
                If Not grupos.ContainsKey(clave) Then grupos(clave)=0D
                grupos(clave)+=valor
                etiquetas(clave)=etiqueta
            Else
                serie.Add(New KeyValuePair(Of String,Decimal)(etiqueta,valor))
            End If
        Next
        If Not agrupada Then
            If numero=7 AndAlso serie.Count>12 Then serie=serie.Skip(serie.Count-12).ToList()
            Return serie
        End If
        serie=grupos.Select(Function(par) New KeyValuePair(Of String,Decimal)(etiquetas(par.Key),par.Value)).ToList()
        If numero=5 Then
            If serie.Count>12 Then serie=serie.Skip(serie.Count-12).ToList()
        ElseIf numero=8 Then
            If serie.Count>12 Then serie=serie.Take(12).ToList()
        Else
            serie=serie.OrderByDescending(Function(par) par.Value).ThenBy(Function(par) par.Key,StringComparer.CurrentCulture).Take(10).ToList()
        End If
        Return serie
    End Function

    Private Function ConstruirGraficaReporte(numero As Integer,datos As DataTable) As String
        Dim serie=DatosGrafica(numero,datos)
        If serie.Count=0 Then Return "<div class='report-chart-empty'>No hay datos suficientes para graficar este resultado. La tabla conserva el resultado consultado.</div>"
        If serie.All(Function(p) p.Value=0D) Then Return "<div class='report-chart-empty'>Los registros encontrados no tienen valores para graficar. Revisa la tabla del reporte.</div>"
        If numero=9 OrElse numero=10 Then
            Dim pastel=ConstruirPastel(numero,datos)
            If numero=9 Then Return "<div class='report-chart-combo'><section class='report-pie-panel'><h4>Préstamos por estado</h4>" & pastel & "</section><section class='report-bars-panel'><h4>Saldo activo por producto</h4>" & ConstruirGraficaBarras(numero,serie) & "</section></div>"
            Return "<div class='report-chart-combo'><section class='report-pie-panel'><h4>Préstamos por método</h4>" & pastel & "</section><section class='report-bars-panel'><h4>Capital por método</h4>" & ConstruirGraficaBarras(numero,serie) & "</section></div>"
        End If
        If numero=5 OrElse numero=7 Then Return ConstruirGraficaLinea(numero,serie)
        Return ConstruirGraficaBarras(numero,serie)
    End Function

    Private Shared Function DatosPastel(numero As Integer,datos As DataTable) As List(Of KeyValuePair(Of String,Decimal))
        Dim valores As New SortedDictionary(Of String,Decimal)(StringComparer.Ordinal)
        For Each fila As DataRow In datos.Rows
            Dim etiqueta As String, valor As Decimal
            If numero=9 Then
                etiqueta=TextoCeldaGrafica(fila,"Estado")
                valor=NumeroCelda(fila,"Préstamos")
            Else
                etiqueta=TextoCeldaGrafica(fila,"Método")
                valor=NumeroCelda(fila,"Préstamos")
            End If
            If Not valores.ContainsKey(etiqueta) Then valores(etiqueta)=0D
            valores(etiqueta)+=valor
        Next
        Return valores.Select(Function(p) New KeyValuePair(Of String,Decimal)(p.Key,p.Value)).OrderByDescending(Function(p) p.Value).ToList()
    End Function

    Private Shared Function ColorPastel(numero As Integer,etiqueta As String,indice As Integer) As String
        If numero=9 Then
            Select Case etiqueta.Trim().ToUpperInvariant()
                Case "VIGENTE", "DESEMBOLSADO" : Return "#0d7a48"
                Case "EN_MORA" : Return "#d64545"
                Case "CANCELADO" : Return "#96a0ad"
                Case Else : Return "#e2a23a"
            End Select
        End If
        Dim colores As String()={"#0d7a48","#4b86d1","#e2a23a","#8364b8","#39a7a0"}
        Return colores(indice Mod colores.Length)
    End Function

    Private Function ConstruirPastel(numero As Integer,datos As DataTable) As String
        Dim serie=DatosPastel(numero,datos)
        Dim total=serie.Sum(Function(p) p.Value)
        If serie.Count=0 OrElse total<=0D Then Return "<div class='report-chart-empty'>No hay valores para mostrar en la gráfica circular.</div>"
        Const radio As Decimal=68D, cx As Decimal=90D, cy As Decimal=90D
        Dim circunferencia As Decimal=CDec(2*Math.PI)*radio
        Dim desplazamiento As Decimal=0D
        Dim sb As New StringBuilder("<div class='report-pie-layout'><svg class='report-pie' viewBox='0 0 180 180' role='img' aria-label='")
        Dim aria=If(numero=9,"Distribución de préstamos por estado","Distribución de préstamos por método de cálculo")
        sb.Append(Global.System.Web.HttpUtility.HtmlAttributeEncode(aria)).Append("'><circle cx='90' cy='90' r='68' fill='none' stroke='#edf1f4' stroke-width='25'/>")
        For i=0 To serie.Count-1
            Dim fraccion=serie(i).Value/total, segmento=fraccion*circunferencia
            Dim color=ColorPastel(numero,serie(i).Key,i)
            Dim title=serie(i).Key & " · " & serie(i).Value.ToString("N0",CultureInfo.GetCultureInfo("es-PA")) & " préstamos"
            sb.Append("<circle cx='").Append(cx.ToString(CultureInfo.InvariantCulture)).Append("' cy='").Append(cy.ToString(CultureInfo.InvariantCulture)).Append("' r='").Append(radio.ToString(CultureInfo.InvariantCulture)).Append("' fill='none' stroke='").Append(color).Append("' stroke-width='25' stroke-dasharray='").Append(segmento.ToString("0.##",CultureInfo.InvariantCulture)).Append(" ").Append((circunferencia-segmento).ToString("0.##",CultureInfo.InvariantCulture)).Append("' stroke-dashoffset='-").Append(desplazamiento.ToString("0.##",CultureInfo.InvariantCulture)).Append("' transform='rotate(-90 90 90)'><title>").Append(Server.HtmlEncode(title)).Append("</title></circle>")
            desplazamiento+=segmento
        Next
        Dim centro=total.ToString("N0",CultureInfo.GetCultureInfo("es-PA"))
        sb.Append("<text x='90' y='86' text-anchor='middle' class='report-pie-total'>").Append(Server.HtmlEncode(centro)).Append("</text><text x='90' y='104' text-anchor='middle' class='report-pie-caption'>préstamos</text></svg><div class='report-pie-legend' role='list'>")
        For i=0 To serie.Count-1
            Dim porcentaje=serie(i).Value*100D/total
            Dim color=ColorPastel(numero,serie(i).Key,i)
            Dim valor=serie(i).Value.ToString("N0",CultureInfo.GetCultureInfo("es-PA"))
            sb.Append("<div class='report-pie-item' role='listitem'><span class='report-pie-dot' style='background:").Append(color).Append("'></span><span class='report-pie-name' title='").Append(Global.System.Web.HttpUtility.HtmlAttributeEncode(serie(i).Key)).Append("'>").Append(Server.HtmlEncode(EstadoLegible(serie(i).Key))).Append("</span><strong>").Append(Server.HtmlEncode(valor)).Append("</strong><small>").Append(porcentaje.ToString("N1",CultureInfo.GetCultureInfo("es-PA"))).Append("%</small></div>")
        Next
        sb.Append("</div></div>")
        Return sb.ToString()
    End Function

    Private Function ConstruirGraficaBarras(numero As Integer,serie As List(Of KeyValuePair(Of String,Decimal))) As String
        Dim maximo=serie.Max(Function(p) p.Value)
        Dim formato=If(numero=1,"N0","N2")
        Dim sb As New StringBuilder("<div class='report-bars' role='list' aria-label='Valores agrupados del reporte'>")
        For Each punto In serie
            Dim ancho=If(maximo<=0D,0D,Math.Max(2D,Decimal.Round(punto.Value*100D/maximo,1)))
            Dim valor=If(numero=1,punto.Value.ToString(formato,CultureInfo.GetCultureInfo("es-PA")),"B/. " & punto.Value.ToString(formato,CultureInfo.GetCultureInfo("es-PA")))
            sb.Append("<div class='report-bar-row' role='listitem'><span class='report-bar-label' title='").Append(Global.System.Web.HttpUtility.HtmlAttributeEncode(punto.Key)).Append("'>").Append(Server.HtmlEncode(punto.Key)).Append("</span><div class='report-bar-track'><div class='report-bar-fill' style='width:").Append(ancho.ToString("0.##",CultureInfo.InvariantCulture)).Append("%'></div></div><strong class='report-bar-value'>").Append(Server.HtmlEncode(valor)).Append("</strong></div>")
        Next
        sb.Append("</div>")
        Return sb.ToString()
    End Function

    Private Function ConstruirGraficaLinea(numero As Integer,serie As List(Of KeyValuePair(Of String,Decimal))) As String
        Const left As Decimal=48D,stepX As Decimal=62D,baseY As Decimal=178D,height As Decimal=132D
        Dim maximo=Math.Max(1D,serie.Max(Function(p) p.Value))
        Dim puntos As New List(Of String)()
        For i=0 To serie.Count-1
            Dim x=left+stepX*i,y=baseY-(serie(i).Value/maximo)*height
            puntos.Add(x.ToString("0.##",CultureInfo.InvariantCulture) & "," & y.ToString("0.##",CultureInfo.InvariantCulture))
        Next
        Dim area="M " & left.ToString(CultureInfo.InvariantCulture) & " " & baseY.ToString(CultureInfo.InvariantCulture) & " L " & String.Join(" L ",puntos) & " L " & (left+stepX*(serie.Count-1)).ToString(CultureInfo.InvariantCulture) & " " & baseY.ToString(CultureInfo.InvariantCulture) & " Z"
        Dim label=If(numero=5,"Pagos aplicados por período","Saldo acumulado del préstamo")
        Dim sb As New StringBuilder("<svg class='report-line-chart' viewBox='0 0 800 220' role='img' aria-label='")
        sb.Append(Global.System.Web.HttpUtility.HtmlAttributeEncode(label)).Append("'><defs><linearGradient id='reportChartFill' x1='0' x2='0' y1='0' y2='1'><stop offset='0' stop-color='#0d7a48' stop-opacity='.2'/><stop offset='1' stop-color='#0d7a48' stop-opacity='.01'/></linearGradient></defs>")
        For i=0 To 3
            Dim y As Decimal = 42D + (44D * i)
            sb.Append("<line class='chart-grid' x1='42' y1='").Append(y.ToString(CultureInfo.InvariantCulture)).Append("' x2='780' y2='").Append(y.ToString(CultureInfo.InvariantCulture)).Append("'/>")
        Next
        sb.Append("<path class='chart-area' d='").Append(area).Append("'/><polyline class='chart-line' points='").Append(String.Join(" ",puntos)).Append("'/>")
        For i=0 To serie.Count-1
            Dim x=left+stepX*i,y=baseY-(serie(i).Value/maximo)*height
            Dim shownLabel=If(serie(i).Key.Length>14,serie(i).Key.Substring(0,12) & "…",serie(i).Key)
            sb.Append("<circle class='chart-point' cx='").Append(x.ToString("0.##",CultureInfo.InvariantCulture)).Append("' cy='").Append(y.ToString("0.##",CultureInfo.InvariantCulture)).Append("' r='3.5'><title>").Append(Server.HtmlEncode(serie(i).Key & " · B/. " & serie(i).Value.ToString("N2",CultureInfo.GetCultureInfo("es-PA")))).Append("</title></circle>")
            sb.Append("<text x='").Append(x.ToString("0.##",CultureInfo.InvariantCulture)).Append("' y='204' text-anchor='middle'>").Append(Server.HtmlEncode(shownLabel)).Append("</text>")
        Next
        sb.Append("<text x='8' y='28'>B/. ").Append(Server.HtmlEncode(maximo.ToString("N2",CultureInfo.GetCultureInfo("es-PA")))).Append("</text><text x='30' y='182'>0</text></svg>")
        Return sb.ToString()
    End Function

    Private Sub ConfigurarGrafica(numero As Integer,datos As DataTable)
        Select Case numero
            Case 1
                tituloGrafica.Text="Clientes por estado" : subtituloGrafica.Text="Cantidad de clientes registrados y su estado actual."
            Case 2
                tituloGrafica.Text="Saldo vigente por producto" : subtituloGrafica.Text="Los 10 productos con mayor saldo; la tabla conserva el detalle completo."
            Case 3
                tituloGrafica.Text="Capital de préstamos cancelados" : subtituloGrafica.Text="Capital original agrupado por producto; se muestran los 10 primeros."
            Case 4
                tituloGrafica.Text="Cuotas vencidas por préstamo" : subtituloGrafica.Text="Monto vencido agrupado por préstamo, sin duplicar cuotas; se muestran los 10 primeros."
            Case 5
                tituloGrafica.Text="Pagos recibidos en el tiempo" : subtituloGrafica.Text="Importe de pagos aplicados por mes; la tabla conserva todo el período filtrado."
            Case 6
                tituloGrafica.Text="Saldo de capital por préstamo" : subtituloGrafica.Text="Se muestran los préstamos con mayor saldo; la tabla incluye el resultado completo."
            Case 7
                tituloGrafica.Text="Evolución del saldo de cuenta" : subtituloGrafica.Text="Saldo acumulado en los últimos 12 movimientos del préstamo."
            Case 8
                tituloGrafica.Text="Pagos programados por cuota" : subtituloGrafica.Text="Primeras 12 cuotas del calendario seleccionado."
            Case 9
                tituloGrafica.Text="Distribución de la cartera" : subtituloGrafica.Text="Préstamos agrupados por estado y saldos pendientes por producto."
            Case 10
                tituloGrafica.Text="Comparativo por método de cálculo" : subtituloGrafica.Text="Participación de préstamos y capital agrupados por método."
        End Select
        graficaReporte.Text=ConstruirGraficaReporte(numero,datos)
    End Sub

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
        ConfigurarGrafica(n,datos)
        Dim columnaTotal=If(n=4,"Vencido",If(n=5,"Pagado",If(n=8,"Pendiente",If(n=3 OrElse n=10,"Capital","Saldo"))))
        etiquetaTotal.Text=If(n=1,"Clientes activos",If(n=7,"Saldo al último movimiento",If(n=4,"Monto vencido",If(n=5,"Pagos aplicados",If(n=8,"Cuotas pendientes",If(n=3,"Capital original",If(n=10,"Capital comparado","Saldo de capital")))))))
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
            ConfigurarGrafica(Integer.Parse(Filtros(0)),datos)
            Dim html As New StringBuilder("<!doctype html><html lang='es'><head><meta charset='utf-8'><title>CapitalWest · Reporte</title><style>body{font:12px Arial;color:#15382c;margin:25px}h1{font-size:22px}h2{font-size:16px;margin:18px 0 8px}table{border-collapse:collapse;width:100%;font-size:10px}td,th{border-bottom:1px solid #ddd;padding:7px;text-align:left}th{background:#edf7f1}thead{display:table-header-group}tr{break-inside:avoid}.report-bars{display:table;width:100%;margin:12px 0 22px}.report-bar-row{display:table-row}.report-bar-label,.report-bar-track,.report-bar-value{display:table-cell;padding:5px 8px;vertical-align:middle}.report-bar-label{width:24%}.report-bar-track{width:60%;background:#eef3f1}.report-bar-fill{height:9px;background:#0d7a48}.report-bar-value{text-align:right;color:#087c49;font-weight:bold}.report-line-chart{width:100%;height:220px}.report-line-chart .chart-grid{stroke:#ddd;stroke-width:1}.report-line-chart .chart-area{fill:#e5f4ec}.report-line-chart .chart-line{fill:none;stroke:#0d7a48;stroke-width:3}.report-line-chart .chart-point{fill:white;stroke:#0d7a48;stroke-width:2}.report-line-chart text{font:10px Arial;fill:#667}.button{padding:10px 20px;margin-bottom:15px}@media print{.button{display:none}@page{size:landscape;margin:12mm}}</style></head><body><button class='button' onclick='window.print()'>Imprimir / Guardar PDF</button><h1>CapitalWest · ")
            html.Append(Server.HtmlEncode(Nombres(Integer.Parse(Filtros(0))-1)) & "</h1><p>" & Server.HtmlEncode(filtrosAplicados.Text) & "</p><p>" & Server.HtmlEncode(CStr(ViewState("DescripcionAplicada"))) & "</p><p>Generado: " & DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm") & " · Panamá · Importes en B/. · " & datos.Rows.Count & " registros</p><h2>Resumen gráfico</h2>" & graficaReporte.Text & "<h2>Detalle del reporte</h2><table><thead><tr>")
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
