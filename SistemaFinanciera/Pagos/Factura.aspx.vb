Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Linq
Imports SistemaFinanciera.Entities

Public Class FacturaPago
    Inherits FinanzasPagina

    Protected ReadOnly Property UrlRegreso As String
        Get
            If String.Equals(Request.QueryString("origen"), "reportes", StringComparison.OrdinalIgnoreCase) Then
                Return ResolveUrl("~/Reportes/Reportes.aspx")
            End If
            Return ResolveUrl("~/Pagos/Pagos.aspx")
        End Get
    End Property

    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then Ejecutar(Sub() CargarFactura())
    End Sub

    Private Sub CargarFactura()
        If Not Actor.Puede("ADMINISTRADOR", "CAJERO") Then Throw New UnauthorizedAccessException("Su rol no puede consultar comprobantes de pago.")

        Dim pagoId As Integer
        If Not Integer.TryParse(Request.QueryString("id"), NumberStyles.None, CultureInfo.InvariantCulture, pagoId) OrElse pagoId <= 0 Then
            Throw New ArgumentException("Seleccione un comprobante válido desde la tabla de pagos.")
        End If

        Dim pagos = ServicioFinanzas.Consultar("PAGOS", Actor.Id)
        Dim pago = pagos.AsEnumerable().FirstOrDefault(Function(fila) CInt(fila("Id")) = pagoId)
        If pago Is Nothing OrElse Not String.Equals(CStr(pago("Estado")), "APLICADO", StringComparison.OrdinalIgnoreCase) Then
            Throw New ArgumentException("El comprobante no existe o el pago no está aplicado.")
        End If

        reciboNumero.Text = Dato(pago("NumeroRecibo"))
        reciboCliente.Text = Dato(pago("Cliente"))
        reciboPrestamo.Text = Dato(pago("NumeroPrestamo"))
        reciboFecha.Text = CDate(pago("FechaPago")).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-PA"))
        Dim codigoForma = Dato(pago("FormaPagoCodigo"))
        reciboForma.Text = CultureInfo.GetCultureInfo("es-PA").TextInfo.ToTitleCase(codigoForma.ToLower(CultureInfo.GetCultureInfo("es-PA")))
        Dim referencia = If(Convert.IsDBNull(pago("ReferenciaExterna")), String.Empty, Dato(pago("ReferenciaExterna")))
        reciboReferencia.Text = If(String.IsNullOrWhiteSpace(referencia), "No aplica", referencia)
        reciboMonto.Text = Dinero(pago("MontoPagado"))
        reciboCapital.Text = Dinero(pago("Capital"))
        reciboInteres.Text = Dinero(pago("Interes"))
        reciboMora.Text = Dinero(pago("Mora"))
        reciboCargos.Text = Dinero(pago("OtrosCargos"))
        reciboSaldoCapital.Text = Dinero(pago("SaldoCapitalPosterior"))
        factura.Visible = True
        documentoFactura.Text = DocumentoImpresion()
    End Sub

    ''' <summary>
    ''' Comprobante formal para imprimir (mismo estilo que la cotización del simulador):
    ''' logo y título, "Recibido de" / "Empresa", detalle del pago, resumen, firmas y pie.
    ''' </summary>
    Private Function DocumentoImpresion() As String
        Dim e = Function(t As String) Server.HtmlEncode(If(t, ""))
        Dim rutaLogo = Server.MapPath("~/Content/Images/capitalwest-login.png")
        Dim logo = If(IO.File.Exists(rutaLogo), "data:image/png;base64," & Convert.ToBase64String(IO.File.ReadAllBytes(rutaLogo)), ResolveUrl("~/Content/Images/capitalwest-login.png"))
        Dim impreso = DateTime.UtcNow.AddHours(-5).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
        Dim sb As New Text.StringBuilder()
        sb.Append("<article class=""fac-doc"">")
        sb.Append("<header class=""fac-head""><img src=""").Append(e(logo)).Append(""" alt=""CapitalWest"" /><div><h1>Comprobante de pago</h1><p>✓ Pago registrado</p></div></header>")
        sb.Append("<div class=""fac-parties""><div><span class=""fac-label"">Recibido de</span><strong>").Append(e(reciboCliente.Text)).Append("</strong>")
        sb.Append("<p>Préstamo: ").Append(e(reciboPrestamo.Text)).Append("</p><p>Forma de pago: ").Append(e(reciboForma.Text)).Append("</p><p>Referencia: ").Append(e(reciboReferencia.Text)).Append("</p></div>")
        sb.Append("<div class=""fac-right""><span class=""fac-label"">Empresa</span><strong>CapitalWest</strong><p>Gestión financiera · Panamá</p><p>Recibo N.º ").Append(e(reciboNumero.Text)).Append("</p><p>Fecha del pago: ").Append(e(reciboFecha.Text)).Append("</p><p>Impreso: ").Append(e(impreso)).Append("</p></div></div>")
        sb.Append("<section class=""fac-section""><h2>Detalle del pago</h2><table class=""fac-table""><thead><tr><th>Concepto</th><th class=""num"">Importe (B/.)</th></tr></thead><tbody>")
        For Each concepto In {New String() {"Aplicado a capital", reciboCapital.Text}, New String() {"Aplicado a intereses", reciboInteres.Text}, New String() {"Mora", reciboMora.Text}, New String() {"Otros cargos", reciboCargos.Text}}
            sb.Append("<tr><td>").Append(e(concepto(0))).Append("</td><td class=""num"">").Append(e(concepto(1))).Append("</td></tr>")
        Next
        sb.Append("</tbody><tfoot><tr><td>Total recibido</td><td class=""num"">B/. ").Append(e(reciboMonto.Text)).Append("</td></tr></tfoot></table></section>")
        sb.Append("<section class=""fac-section""><h2>Resumen</h2><div class=""fac-summary""><div class=""is-key""><span>Total recibido</span><strong>B/. ").Append(e(reciboMonto.Text)).Append("</strong></div><div><span>Saldo de capital posterior</span><strong>B/. ").Append(e(reciboSaldoCapital.Text)).Append("</strong></div></div></section>")
        sb.Append("<div class=""fac-signs""><div><span></span>Firma del cliente</div><div><span></span>Firma del cajero</div></div>")
        sb.Append("<footer class=""fac-foot"">El pago se distribuyó según el calendario y las condiciones registradas para este préstamo. Conserve este comprobante como constancia de pago. Importes en balboas (B/.).</footer></article>")
        Return sb.ToString()
    End Function

    Private Shared Function Dato(valor As Object) As String
        Return If(valor Is Nothing OrElse Convert.IsDBNull(valor), String.Empty, Convert.ToString(valor, CultureInfo.InvariantCulture))
    End Function
End Class
