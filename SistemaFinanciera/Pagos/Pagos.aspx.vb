Option Strict On
Imports System.Data
Imports SistemaFinanciera.Entities
Imports SistemaFinanciera.BL
Public Class Pagos
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        registro.Visible = Actor.Puede("ADMINISTRADOR", "CAJERO")
        If Not IsPostBack Then Ejecutar(Sub()
            Dim datos = ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id)
            datos.Columns.Add("Etiqueta", GetType(String))
            For Each r As DataRow In datos.Rows
                r("Etiqueta") = CStr(r("NumeroPrestamo")) & " · " & CStr(r("NombreCompleto")) & " · " & CStr(r("EstadoActual"))
            Next
            Lista(prestamo, datos, "Id", "Etiqueta")
            Lista(forma, ServicioFinanzas.Consultar("FORMAS", Actor.Id), "Codigo")
            fechaPago.Text = ClienteServicio.HoyPanama.ToString("yyyy-MM-dd")
            ViewState("ClavePago") = Guid.NewGuid().ToString("N")
            guardar.Enabled = False
            Vincular(listado, ServicioFinanzas.Consultar("PAGOS", Actor.Id))
        End Sub)
    End Sub
    Protected Sub Cambiado(sender As Object, e As EventArgs) Handles prestamo.SelectedIndexChanged
        Ejecutar(Sub()
            ViewState("ClavePago") = Guid.NewGuid().ToString("N")
            guardar.Enabled = True
            MostrarPrestamo()
        End Sub)
    End Sub
    Private Sub MostrarPrestamo()
        Dim id = Seleccion(prestamo), plan = ServicioFinanzas.Consultar("CUOTAS", Actor.Id, id)
        Dim r = Fila(ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id, id))
        Dim exigible = plan.AsEnumerable().Where(Function(q) CDate(q("FechaVencimiento")).Date <= ClienteServicio.HoyPanama).Sum(Function(q) CDec(q("TotalPendiente")))
        guardar.Enabled = Actor.Puede("ADMINISTRADOR", "CAJERO") AndAlso exigible > 0 AndAlso {"VIGENTE", "EN_MORA", "DESEMBOLSADO"}.Contains(CStr(r("EstadoCodigo")))
        monto.Attributes("max") = exigible.ToString("0.00", Globalization.CultureInfo.InvariantCulture)
        saldo.Text = "Capital pendiente: B/. " & Dinero(r("SaldoCapital")) & "; cuotas exigibles hoy: B/. " & Dinero(exigible) & ". No se generan nuevos cargos moratorios automáticamente en este módulo."
        Vincular(cuotas, plan) : Vincular(listado, ServicioFinanzas.Consultar("PAGOS", Actor.Id, id))
    End Sub
    Protected Sub GuardarClick(sender As Object, e As EventArgs) Handles guardar.Click
        Ejecutar(Sub()
            Dim id = ServicioFinanzas.RegistrarPago(New PagoFinanciero With {.PrestamoId = Seleccion(prestamo), .Monto = Numero(monto, "Monto recibido"), .Fecha = Fecha(fechaPago), .Forma = forma.SelectedValue, .Referencia = referencia.Text.Trim(), .Clave = CStr(ViewState("ClavePago"))}, Actor.Id)
            MostrarPrestamo() : guardar.Enabled = False
            Notificar("Pago registrado. Recibo interno " & id & ". Para otro pago, vuelva a seleccionar el préstamo. Repetir este envío no duplica el recibo.")
        End Sub)
    End Sub
End Class
