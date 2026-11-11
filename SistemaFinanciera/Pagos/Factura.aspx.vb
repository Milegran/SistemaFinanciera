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
    End Sub

    Private Shared Function Dato(valor As Object) As String
        Return If(valor Is Nothing OrElse Convert.IsDBNull(valor), String.Empty, Convert.ToString(valor, CultureInfo.InvariantCulture))
    End Function
End Class
