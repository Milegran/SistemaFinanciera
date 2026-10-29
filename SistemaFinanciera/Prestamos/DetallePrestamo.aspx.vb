Option Strict On
Public Class DetallePrestamo
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then Ejecutar(Sub()
            Dim id As Integer
            If Not Integer.TryParse(Request.QueryString("id"), id) OrElse id <= 0 Then Throw New ArgumentException("Seleccione un préstamo desde la cartera.")
            Dim r = Fila(ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id, id))
            resumen.Text = CStr(r("NumeroPrestamo")) & " · " & CStr(r("NombreCompleto")) & " · " & CStr(r("Producto")) & " · " & CStr(r("Metodo")) & " · Capital original: B/. " & Dinero(r("MontoAprobado")) & " · Capital pendiente: B/. " & Dinero(r("SaldoCapital")) & " · Estado: " & CStr(r("EstadoActual")) & " · Tasa anual pactada: " & Convert.ToString(r("TasaInteres")) & "% · Cuotas: " & Convert.ToString(r("CantidadCuotas"))
            Vincular(cuotas, ServicioFinanzas.Consultar("CUOTAS", Actor.Id, id))
            Vincular(movimientos, ServicioFinanzas.Consultar("MOVIMIENTOS", Actor.Id, id))
        End Sub)
    End Sub
End Class
