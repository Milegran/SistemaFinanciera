Option Strict On
Imports System.Data
Imports SistemaFinanciera.BL
Public Class Prestamos
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        formalizacion.Visible = Actor.Puede("ADMINISTRADOR")
        If Not IsPostBack Then Ejecutar(Sub()
                                           Listar()
                                           desembolsar.Enabled = False
                                       End Sub)
    End Sub
    Private Sub Listar()
        Dim prestamos = ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id)
        Vincular(listado, prestamos)
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
    End Sub
    Protected Sub Cambiada(sender As Object, e As EventArgs) Handles solicitud.SelectedIndexChanged
        Ejecutar(Sub()
            Dim r = Fila(ServicioFinanzas.Consultar("SOLICITUDES", Actor.Id, Seleccion(solicitud)))
            ViewState("Version") = CULng(r("Version"))
            desembolsar.Enabled = Actor.Puede("ADMINISTRADOR")
            Dim periodo = ServicioFinanzas.Consultar("PERIODICIDADES", Actor.Id).AsEnumerable().Single(Function(x) CInt(x("Id")) = CInt(r("PeriodicidadId")))
            Dim hoy = ClienteServicio.HoyPanama
            primerPago.Text = If(CStr(periodo("Unidad")) = "MESES", hoy.AddMonths(CInt(periodo("Intervalo"))), hoy.AddDays(CInt(periodo("Intervalo")))).ToString("yyyy-MM-dd")
            primerPago.Attributes("min") = primerPago.Text
            primerPago.Attributes("max") = primerPago.Text
            condiciones.Text = "Aprobado: B/. " & Dinero(r("MontoAprobado")) & "; tasa anual: " & Convert.ToString(r("TasaPactada")) & "% " & CStr(r("TipoTasa")) & "; cuotas: " & Convert.ToString(r("CantidadCuotas")) & "; costos retenidos: B/. " & Dinero(CDec(r("Comision")) + CDec(r("GastosAsociados")))
        End Sub)
    End Sub
    Protected Sub DesembolsarClick(sender As Object, e As EventArgs) Handles desembolsar.Click
        Ejecutar(Sub()
            If ViewState("Version") Is Nothing Then Throw New ArgumentException("Seleccione la solicitud aprobada.")
            Dim id = ServicioFinanzas.Formalizar(Seleccion(solicitud), CULng(ViewState("Version")), Fecha(primerPago), Actor.Id)
            Listar() : condiciones.Text = "" : ViewState.Remove("Version")
            desembolsar.Enabled = False
            Notificar("Préstamo formalizado y desembolsado. Número interno " & id & ". Selecciónelo para consultar el contrato y las cuotas.")
        End Sub)
    End Sub
    Protected Sub Ver(sender As Object, e As GridViewCommandEventArgs) Handles listado.RowCommand
        If e.CommandName = "Ver" Then Response.Redirect("DetallePrestamo.aspx?id=" & CInt(listado.DataKeys(CInt(e.CommandArgument)).Value).ToString(), True)
    End Sub
End Class
