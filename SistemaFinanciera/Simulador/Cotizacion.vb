Option Strict On
Imports System.Globalization
Imports System.Text
Imports SistemaFinanciera.Entities

''' <summary>Datos de una cotización generada desde el simulador.</summary>
<Serializable>
Public Class CotizacionPrestamo
    Public Property Numero As String
    Public Property FechaEmision As DateTime
    Public Property ClienteNombre As String
    Public Property ClienteIdentificacion As String
    Public Property ClienteTelefono As String
    Public Property ClienteCorreo As String
    Public Property ClienteRegistrado As Boolean
    Public Property Producto As String
    Public Property Metodo As String
    Public Property NotaMetodo As String
    Public Property Periodicidad As String
    Public Property TasaAnual As Decimal
    Public Property TipoTasa As String
    Public Property Monto As Decimal
    Public Property Plazo As Integer
    Public Property FechaPrimerPago As Date
    Public Property Comision As Decimal
    Public Property Gastos As Decimal
    Public Property Asesor As String
    Public Property Plan As List(Of CuotaPlan)

    Public ReadOnly Property TotalIntereses As Decimal
        Get
            Return Plan.Sum(Function(q) q.Interes)
        End Get
    End Property
    Public ReadOnly Property TotalPagar As Decimal
        Get
            Return Plan.Sum(Function(q) q.Cuota)
        End Get
    End Property
    Public ReadOnly Property PrimeraCuota As Decimal
        Get
            Return If(Plan.Count = 0, 0D, Plan(0).Cuota)
        End Get
    End Property
    Public ReadOnly Property CuotaMaxima As Decimal
        Get
            Return If(Plan.Count = 0, 0D, Plan.Max(Function(q) q.Cuota))
        End Get
    End Property
    Public ReadOnly Property NetoRecibir As Decimal
        Get
            Return Monto - Comision - Gastos
        End Get
    End Property
    Public ReadOnly Property TasaTexto As String
        Get
            Return TasaAnual.ToString("0.######", Cultura) & "% " & If(TipoTasa = "EFECTIVA_ANUAL", "efectiva anual", "nominal anual")
        End Get
    End Property

    Public Shared ReadOnly Cultura As CultureInfo = CultureInfo.GetCultureInfo("es-PA")
    Public Const Aviso As String = "Cotización informativa basada en las condiciones vigentes del producto a la fecha de emisión. No constituye aprobación ni oferta vinculante de crédito; el calendario contractual, la tasa y los cargos definitivos se confirman al formalizar la solicitud."

    Public Shared Function Dinero(valor As Decimal) As String
        Return "B/. " & valor.ToString("N2", Cultura)
    End Function
    Public Shared Function Numero2(valor As Decimal) As String
        Return valor.ToString("N2", Cultura)
    End Function
    Public Shared Function Fecha(valor As Date) As String
        Return valor.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
    End Function
    Public Shared Function TextoONoIndicado(valor As String) As String
        Return If(String.IsNullOrWhiteSpace(valor), "No indicado", valor.Trim())
    End Function

    ''' <summary>Pares etiqueta/valor de la sección "Datos del cliente".</summary>
    Public Function DatosCliente() As List(Of KeyValuePair(Of String, String))
        Return New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)("Nombre", TextoONoIndicado(ClienteNombre)),
            New KeyValuePair(Of String, String)("Identificación", TextoONoIndicado(ClienteIdentificacion)),
            New KeyValuePair(Of String, String)("Teléfono", TextoONoIndicado(ClienteTelefono)),
            New KeyValuePair(Of String, String)("Correo electrónico", TextoONoIndicado(ClienteCorreo)),
            New KeyValuePair(Of String, String)("Condición", If(ClienteRegistrado, "Cliente registrado", "Prospecto (no registrado)"))}
    End Function

    ''' <summary>True si se indicaron datos del cliente (son opcionales).</summary>
    Public ReadOnly Property TieneCliente As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(ClienteNombre)
        End Get
    End Property

    ''' <summary>Líneas bajo el nombre del cliente en el encabezado sencillo (sin vacíos).</summary>
    Public Function LineasCliente() As List(Of String)
        Dim lineas As New List(Of String)()
        If Not String.IsNullOrWhiteSpace(ClienteIdentificacion) Then lineas.Add("Identificación: " & ClienteIdentificacion.Trim())
        If Not String.IsNullOrWhiteSpace(ClienteTelefono) Then lineas.Add("Teléfono: " & ClienteTelefono.Trim())
        If Not String.IsNullOrWhiteSpace(ClienteCorreo) Then lineas.Add(ClienteCorreo.Trim())
        lineas.Add(If(ClienteRegistrado, "Cliente registrado", "Prospecto (no registrado)"))
        Return lineas
    End Function

    Public Const NombreEmpresa As String = "CapitalWest"

    ''' <summary>Líneas bajo el nombre de la empresa en el encabezado sencillo.</summary>
    Public Function LineasEmpresa() As List(Of String)
        Return New List(Of String) From {
            "Soluciones financieras",
            "Asesor: " & TextoONoIndicado(Asesor),
            "Cotización N.º " & Numero,
            "Emitida: " & FechaEmision.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}
    End Function

    ''' <summary>Pares etiqueta/valor de la sección "Datos de la cotización".</summary>
    Public Function DatosCotizacion() As List(Of KeyValuePair(Of String, String))
        Return New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)("Producto financiero", Producto),
            New KeyValuePair(Of String, String)("Método de cálculo", Metodo),
            New KeyValuePair(Of String, String)("Tasa de interés", TasaTexto),
            New KeyValuePair(Of String, String)("Periodicidad de pago", Periodicidad),
            New KeyValuePair(Of String, String)("Monto solicitado", Dinero(Monto)),
            New KeyValuePair(Of String, String)("Plazo", Plazo.ToString(Cultura) & " cuotas"),
            New KeyValuePair(Of String, String)("Primer pago estimado", Fecha(FechaPrimerPago))}
    End Function

    ''' <summary>Pares etiqueta/valor del resumen económico.</summary>
    Public Function Resumen() As List(Of KeyValuePair(Of String, Decimal))
        Return New List(Of KeyValuePair(Of String, Decimal)) From {
            New KeyValuePair(Of String, Decimal)("Primera cuota estimada", PrimeraCuota),
            New KeyValuePair(Of String, Decimal)("Cuota máxima", CuotaMaxima),
            New KeyValuePair(Of String, Decimal)("Total de intereses", TotalIntereses),
            New KeyValuePair(Of String, Decimal)("Total a pagar", TotalPagar),
            New KeyValuePair(Of String, Decimal)("Comisión retenida", Comision),
            New KeyValuePair(Of String, Decimal)("Gastos retenidos", Gastos),
            New KeyValuePair(Of String, Decimal)("Neto estimado a recibir", NetoRecibir)}
    End Function

    ''' <summary>Documento HTML formal para imprimir (se muestra solo al imprimir).</summary>
    Public Function Html(logoUrl As String) As String
        Dim e = Function(t As String) HttpUtility.HtmlEncode(t)
        Dim sb As New StringBuilder()
        sb.Append("<article class=""cot-doc"">")
        ' Encabezado sencillo: logo y título, y debajo dos columnas (cliente / empresa) sin recuadros.
        sb.Append("<header class=""cot-head""><img src=""").Append(e(logoUrl)).Append(""" alt=""CapitalWest"" /><h1>Cotización de préstamo</h1></header>")
        sb.Append("<div class=""cot-parties""><div>")
        If TieneCliente Then
            sb.Append("<span class=""cot-label"">Cotización para</span><strong>").Append(e(ClienteNombre.Trim())).Append("</strong>")
            For Each linea In LineasCliente()
                sb.Append("<p>").Append(e(linea)).Append("</p>")
            Next
        End If
        sb.Append("</div><div class=""cot-right""><span class=""cot-label"">Empresa</span><strong>").Append(e(NombreEmpresa)).Append("</strong>")
        For Each linea In LineasEmpresa()
            sb.Append("<p>").Append(e(linea)).Append("</p>")
        Next
        sb.Append("</div></div>")
        AppendSeccion(sb, "Datos de la cotización", DatosCotizacion(), e)
        sb.Append("<section class=""cot-section""><h2>Resumen económico</h2><div class=""cot-summary"">")
        For Each par In Resumen()
            sb.Append("<div").Append(If(par.Key = "Total a pagar" OrElse par.Key = "Neto estimado a recibir", " class=""is-key""", "")).Append("><span>").Append(e(par.Key)).Append("</span><strong>").Append(e(Dinero(par.Value))).Append("</strong></div>")
        Next
        sb.Append("</div></section>")
        sb.Append("<section class=""cot-section cot-section-tabla""><h2>Tabla de amortización</h2><p class=""cot-note"">").Append(e(NotaMetodo)).Append("</p><table class=""cot-table""><thead><tr><th>N.º</th><th>Fecha</th><th>Capital (B/.)</th><th>Interés (B/.)</th><th>Cuota (B/.)</th><th>Saldo (B/.)</th></tr></thead><tbody>")
        For Each q In Plan
            sb.Append("<tr><td>").Append(q.Numero).Append("</td><td>").Append(Fecha(q.Fecha)).Append("</td><td>").Append(Numero2(q.Capital)).Append("</td><td>").Append(Numero2(q.Interes)).Append("</td><td>").Append(Numero2(q.Cuota)).Append("</td><td>").Append(Numero2(q.Saldo)).Append("</td></tr>")
        Next
        sb.Append("</tbody><tfoot><tr><td colspan=""2"">Totales</td><td>").Append(Numero2(Plan.Sum(Function(q) q.Capital))).Append("</td><td>").Append(Numero2(TotalIntereses)).Append("</td><td>").Append(Numero2(TotalPagar)).Append("</td><td></td></tr></tfoot></table></section>")
        sb.Append("<div class=""cot-signs""><div><span></span>Firma del cliente</div><div><span></span>Firma del asesor</div></div>")
        sb.Append("<footer class=""cot-foot"">").Append(e(Aviso)).Append("</footer></article>")
        Return sb.ToString()
    End Function

    Private Shared Sub AppendSeccion(sb As StringBuilder, titulo As String, datos As List(Of KeyValuePair(Of String, String)), e As Func(Of String, String))
        sb.Append("<section class=""cot-section""><h2>").Append(e(titulo)).Append("</h2><dl class=""cot-grid"">")
        For Each par In datos
            sb.Append("<div><dt>").Append(e(par.Key)).Append("</dt><dd>").Append(e(par.Value)).Append("</dd></div>")
        Next
        sb.Append("</dl></section>")
    End Sub
End Class
