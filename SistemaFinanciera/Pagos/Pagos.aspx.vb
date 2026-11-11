Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Linq
Imports System.Web.UI.WebControls
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
            Lista(clienteFactura, ServicioFinanzas.Consultar("CLIENTES", Actor.Id), "Id", "Nombre")
            If clienteFactura.Items.Count > 0 Then clienteFactura.Items(0).Text = "Todos los clientes"
            fechaPago.Text = ClienteServicio.HoyPanama.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            ViewState("ClavePago") = Guid.NewGuid().ToString("N")
            montoVencidoSugerido.Value = "0.00"
            montoTotalSugerido.Value = "0.00"
            guardar.Enabled = False
            If prestamo.Items.Count > 0 AndAlso Not String.IsNullOrWhiteSpace(prestamo.SelectedValue) Then MostrarPrestamo()
            If prestamo.Items.Count <= 1 Then
                MostrarAvisoPago("No hay préstamos disponibles para registrar un pago.")
            ElseIf String.IsNullOrWhiteSpace(prestamo.SelectedValue) Then
                MostrarAvisoPago("Selecciona un préstamo para comprobar si tiene cuotas cobrables.")
            End If
            Dim facturas = ServicioFinanzas.Consultar("PAGOS", Actor.Id)
            CargarFormasFiltroFactura(facturas)
            CargarCuotasCliente()
            AplicarFiltrosFactura(facturas)
        End Sub)
    End Sub

    Protected Sub Cambiado(sender As Object, e As EventArgs) Handles prestamo.SelectedIndexChanged
        Ejecutar(Sub()
            ViewState("ClavePago") = Guid.NewGuid().ToString("N")
            monto.Text = String.Empty
            montoErrorServidor.Visible = False
            monto.CssClass = "form-control"
            comprobante.Visible = False
            If String.IsNullOrWhiteSpace(prestamo.SelectedValue) Then
                cuotasCliente.PageIndex = 0
                guardar.Enabled = False
                pagarVencido.Disabled = True
                pagarTodo.Disabled = True
                montoVencidoSugerido.Value = "0.00"
                montoTotalSugerido.Value = "0.00"
                textoMontoVencido.Text = "0.00"
                textoMontoTotal.Text = "0.00"
                MostrarAvisoPago("Selecciona un préstamo para comprobar si tiene cuotas cobrables.")
                CargarCuotasCliente()
                Return
            End If
            cuotasCliente.PageIndex = 0
            MostrarPrestamo()
            CargarCuotasCliente()
        End Sub)
    End Sub

    Private Sub MostrarPrestamo()
        Dim id = Seleccion(prestamo)
        Dim plan = ServicioFinanzas.Consultar("CUOTAS", Actor.Id, id)
        Dim prestamoActual = Fila(ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id, id))
        Dim hoy = ClienteServicio.HoyPanama.Date
        Dim finMes = New Date(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month))
        Dim cuotasPendientes = plan.AsEnumerable().Where(Function(q) CDec(q("TotalPendiente")) > 0D)
        Dim vencido = cuotasPendientes.Where(Function(q) CDate(q("FechaVencimiento")).Date < hoy).Sum(Function(q) CDec(q("TotalPendiente")))
        Dim totalMes = cuotasPendientes.Where(Function(q) CDate(q("FechaVencimiento")).Date <= finMes).Sum(Function(q) CDec(q("TotalPendiente")))
        Dim cuotaMesNoVencida = totalMes - vencido

        monto.Attributes("min") = "0.01"
        monto.Attributes("max") = totalMes.ToString("0.00", CultureInfo.InvariantCulture)
        montoVencidoSugerido.Value = vencido.ToString("0.00", CultureInfo.InvariantCulture)
        montoTotalSugerido.Value = totalMes.ToString("0.00", CultureInfo.InvariantCulture)
        textoMontoVencido.Text = Dinero(vencido)
        textoMontoTotal.Text = Dinero(totalMes)

        Dim estadoPrestamo = CStr(prestamoActual("EstadoCodigo"))
        Dim admitePagos = {"VIGENTE", "EN_MORA", "DESEMBOLSADO"}.Contains(estadoPrestamo)
        pagarVencido.Disabled = Not admitePagos OrElse vencido <= 0D
        pagarTodo.Disabled = Not admitePagos OrElse totalMes <= 0D
        guardar.Enabled = Actor.Puede("ADMINISTRADOR", "CAJERO") AndAlso totalMes > 0D AndAlso admitePagos
        If Not admitePagos Then
            MostrarAvisoPago("No se puede registrar el pago porque el préstamo está " & estadoPrestamo.Replace("_", " ").ToLowerInvariant() & " y no admite nuevos cobros.")
        ElseIf totalMes <= 0D Then
            Dim proximaCuota = cuotasPendientes.Where(Function(q) CDate(q("FechaVencimiento")).Date > finMes).OrderBy(Function(q) CDate(q("FechaVencimiento"))).FirstOrDefault()
            Dim detalleProximaCuota = If(proximaCuota Is Nothing, String.Empty, " La próxima cuota vence el " & CDate(proximaCuota("FechaVencimiento")).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-PA")) & "; no se pueden adelantar pagos de períodos futuros.")
            MostrarAvisoPago("No se puede registrar un pago ahora: no hay cuotas vencidas ni programadas para este mes con saldo pendiente." & detalleProximaCuota)
        Else
            MostrarAvisoPago(String.Empty)
        End If
        saldo.Text = "Capital pendiente: B/. " & Dinero(prestamoActual("SaldoCapital")) & "; cuotas vencidas: B/. " & Dinero(vencido) & "; cuota programada del mes: B/. " & Dinero(cuotaMesNoVencida) & "; total sugerido: B/. " & Dinero(totalMes) & ". No se generan cargos moratorios nuevos automáticamente en este módulo."
    End Sub

    Private Sub MostrarAvisoPago(texto As String)
        avisoPagoNoDisponible.Visible = Not String.IsNullOrWhiteSpace(texto)
        razonPagoNoDisponible.Text = If(avisoPagoNoDisponible.Visible, texto, String.Empty)
    End Sub

    Protected Sub FiltrarFacturasClick(sender As Object, e As EventArgs)
        Ejecutar(Sub()
            listado.PageIndex = 0
            CargarCuotasCliente()
            AplicarFiltrosFactura(ServicioFinanzas.Consultar("PAGOS", Actor.Id))
        End Sub)
    End Sub

    Protected Sub CambiarClienteFiltroClick(sender As Object, e As EventArgs)
        Ejecutar(Sub()
            listado.PageIndex = 0
            cuotasCliente.PageIndex = 0
            CargarCuotasCliente()
            AplicarFiltrosFactura(ServicioFinanzas.Consultar("PAGOS", Actor.Id))
        End Sub)
    End Sub

    Protected Sub LimpiarFacturasClick(sender As Object, e As EventArgs)
        Ejecutar(Sub()
            buscarFactura.Text = String.Empty
            fechaDesdeFactura.Text = String.Empty
            fechaHastaFactura.Text = String.Empty
            clienteFactura.SelectedIndex = 0
            formaFactura.SelectedIndex = 0
            estadoFactura.SelectedIndex = 0
            listado.PageIndex = 0
            cuotasCliente.PageIndex = 0
            CargarCuotasCliente()
            AplicarFiltrosFactura(ServicioFinanzas.Consultar("PAGOS", Actor.Id))
        End Sub)
    End Sub

    Protected Sub PaginarFacturas(sender As Object, e As GridViewPageEventArgs)
        Ejecutar(Sub()
            listado.PageIndex = e.NewPageIndex
            AplicarFiltrosFactura(ServicioFinanzas.Consultar("PAGOS", Actor.Id))
        End Sub)
    End Sub

    Protected Sub PaginarCuotasCliente(sender As Object, e As GridViewPageEventArgs)
        Ejecutar(Sub()
            cuotasCliente.PageIndex = e.NewPageIndex
            CargarCuotasCliente()
        End Sub)
    End Sub

    Private Sub CargarCuotasCliente()
        Dim clienteId As Integer
        Dim clienteSeleccionado As Boolean = Integer.TryParse(clienteFactura.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, clienteId) AndAlso clienteId > 0

        ' En el formulario de pago se selecciona el préstamo, no el cliente.
        ' Si el filtro de facturas sigue en "Todos los clientes", usamos el
        ' titular del préstamo para mostrar todas sus cuotas pendientes.
        If Not clienteSeleccionado AndAlso Integer.TryParse(prestamo.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, clienteId) AndAlso clienteId > 0 Then
            Dim prestamoSeleccionado = Fila(ServicioFinanzas.Consultar("PRESTAMOS", Actor.Id, clienteId))
            If prestamoSeleccionado IsNot Nothing AndAlso prestamoSeleccionado.Table.Columns.Contains("ClienteId") Then
                clienteId = Convert.ToInt32(prestamoSeleccionado("ClienteId"), CultureInfo.InvariantCulture)
                clienteSeleccionado = clienteId > 0
            End If
        End If

        If Not clienteSeleccionado Then
            panelCuotasCliente.Visible = False
            conteoCuotasCliente.Text = String.Empty
            Vincular(cuotasCliente, Nothing)
            Return
        End If

        Dim cuotas = ServicioFinanzas.Consultar("CUOTAS_CLIENTE", Actor.Id, clienteId)
        panelCuotasCliente.Visible = True
        conteoCuotasCliente.Text = cuotas.Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) & If(cuotas.Rows.Count = 1, " cuota pendiente", " cuotas pendientes")
        Vincular(cuotasCliente, cuotas)
    End Sub

    Private Sub CargarFormasFiltroFactura(facturas As DataTable)
        formaFactura.Items.Clear()
        formaFactura.Items.Add(New ListItem("Todas las formas", String.Empty))
        For Each fila As DataRow In ServicioFinanzas.Consultar("FORMAS", Actor.Id).Rows
            AgregarFormaFiltro(Convert.ToString(fila("Codigo"), CultureInfo.InvariantCulture), Convert.ToString(fila("Nombre"), CultureInfo.InvariantCulture))
        Next
        For Each codigo In facturas.AsEnumerable().Select(Function(fila) Convert.ToString(fila("FormaPagoCodigo"), CultureInfo.InvariantCulture)).Where(Function(valor) Not String.IsNullOrWhiteSpace(valor)).Distinct(StringComparer.OrdinalIgnoreCase)
            AgregarFormaFiltro(codigo, CultureInfo.GetCultureInfo("es-PA").TextInfo.ToTitleCase(codigo.ToLower(CultureInfo.GetCultureInfo("es-PA"))))
        Next
    End Sub

    Private Sub AgregarFormaFiltro(codigo As String, nombre As String)
        If String.IsNullOrWhiteSpace(codigo) OrElse formaFactura.Items.FindByValue(codigo) IsNot Nothing Then Return
        formaFactura.Items.Add(New ListItem(nombre, codigo))
    End Sub

    Private Sub AplicarFiltrosFactura(facturas As DataTable)
        Dim texto As String = buscarFactura.Text.Trim()
        Dim clienteSeleccionado As String = clienteFactura.SelectedValue
        Dim formaSeleccionada As String = formaFactura.SelectedValue
        Dim estadoSeleccionado As String = estadoFactura.SelectedValue
        Dim desde As Nullable(Of Date) = FechaFiltroFactura(fechaDesdeFactura.Text, "Desde")
        Dim hasta As Nullable(Of Date) = FechaFiltroFactura(fechaHastaFactura.Text, "Hasta")
        If desde.HasValue AndAlso hasta.HasValue AndAlso desde.Value.Date > hasta.Value.Date Then Throw New ArgumentException("La fecha inicial no puede ser posterior a la fecha final.")

        Dim filas = facturas.AsEnumerable().Where(Function(fila)
            If clienteSeleccionado.Length > 0 AndAlso Convert.ToInt32(fila("ClienteId"), CultureInfo.InvariantCulture) <> Convert.ToInt32(clienteSeleccionado, CultureInfo.InvariantCulture) Then Return False
            If texto.Length > 0 Then
                Dim textoFila = String.Join(" ", {Convert.ToString(fila("NumeroRecibo"), CultureInfo.InvariantCulture), Convert.ToString(fila("Cliente"), CultureInfo.InvariantCulture), Convert.ToString(fila("NumeroPrestamo"), CultureInfo.InvariantCulture)})
                If textoFila.IndexOf(texto, StringComparison.CurrentCultureIgnoreCase) < 0 Then Return False
            End If
            If formaSeleccionada.Length > 0 AndAlso Not String.Equals(Convert.ToString(fila("FormaPagoCodigo"), CultureInfo.InvariantCulture), formaSeleccionada, StringComparison.OrdinalIgnoreCase) Then Return False
            If estadoSeleccionado.Length > 0 AndAlso Not String.Equals(Convert.ToString(fila("Estado"), CultureInfo.InvariantCulture), estadoSeleccionado, StringComparison.OrdinalIgnoreCase) Then Return False
            Dim fecha As Date = Convert.ToDateTime(fila("FechaPago"), CultureInfo.InvariantCulture).Date
            If desde.HasValue AndAlso fecha < desde.Value.Date Then Return False
            If hasta.HasValue AndAlso fecha > hasta.Value.Date Then Return False
            Return True
        End Function).ToList()

        Dim resultado As DataTable = If(filas.Count = 0, facturas.Clone(), filas.CopyToDataTable())
        conteoFacturas.Text = filas.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA")) & If(filas.Count = 1, " factura encontrada", " facturas encontradas")
        Vincular(listado, resultado)
    End Sub

    Private Shared Function FechaFiltroFactura(valor As String, nombre As String) As Nullable(Of Date)
        If String.IsNullOrWhiteSpace(valor) Then Return Nothing
        Dim fecha As Date
        If Not Date.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then Throw New ArgumentException("Seleccione una fecha válida en el filtro " & nombre.ToLowerInvariant() & ".")
        Return fecha.Date
    End Function

    Protected Sub GuardarClick(sender As Object, e As EventArgs) Handles guardar.Click
        Ejecutar(Sub()
            Dim prestamoId = Seleccion(prestamo)
            Dim id As Integer
            Try
                Dim montoIngresado = Numero(monto, "Monto recibido")
                id = ServicioFinanzas.RegistrarPago(New PagoFinanciero With {
                    .PrestamoId = prestamoId,
                    .Monto = montoIngresado,
                    .Fecha = Fecha(fechaPago),
                    .Forma = forma.SelectedValue,
                    .Referencia = referencia.Text.Trim(),
                    .Clave = CStr(ViewState("ClavePago"))
                }, Actor.Id)
            Catch ex As ArgumentException When ex.Message.IndexOf("monto", StringComparison.OrdinalIgnoreCase) >= 0 OrElse ex.Message.IndexOf("importe", StringComparison.OrdinalIgnoreCase) >= 0
                monto.CssClass = "form-control finance-invalid"
                montoErrorServidor.Text = ex.Message
                montoErrorServidor.Visible = True
                Notificar(ex.Message, esError:=True)
                Return
            End Try

            Dim comprobanteListo = MostrarComprobante(id, prestamoId)
            monto.Text = String.Empty
            montoErrorServidor.Visible = False
            monto.CssClass = "form-control"
            ViewState("ClavePago") = Guid.NewGuid().ToString("N")
            MostrarPrestamo()
            CargarCuotasCliente()
            AplicarFiltrosFactura(ServicioFinanzas.Consultar("PAGOS", Actor.Id))
            If comprobanteListo Then
                Notificar("Pago registrado. Se generó el comprobante " & reciboNumero.Text & ".")
                AbrirDialogoComprobante()
            End If
        End Sub)
    End Sub

    Protected Sub VerComprobante(sender As Object, e As GridViewCommandEventArgs) Handles listado.RowCommand
        If e.CommandName <> "VerComprobante" Then Return
        Ejecutar(Sub()
            If Not Actor.Puede("ADMINISTRADOR", "CAJERO") Then Throw New UnauthorizedAccessException("No tiene permiso para consultar comprobantes de pago.")

            Dim indice As Integer
            If Not Integer.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), indice) OrElse indice < 0 OrElse indice >= listado.DataKeys.Count Then Throw New ArgumentException("El comprobante seleccionado no es válido.")

            Dim datos = listado.DataKeys(indice).Values
            Dim pagoId = Convert.ToInt32(datos("Id"), CultureInfo.InvariantCulture)
            Response.Redirect(ResolveUrl("~/Pagos/Factura.aspx?id=" & pagoId.ToString(CultureInfo.InvariantCulture)), False)
            Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub

    Protected Sub VisibilidadComprobante(sender As Object, e As GridViewRowEventArgs) Handles listado.RowDataBound
        If e.Row.RowType <> DataControlRowType.DataRow Then Return
        Dim boton = TryCast(e.Row.FindControl("verComprobante"), LinkButton)
        If boton Is Nothing Then Return
        Dim pago = TryCast(e.Row.DataItem, DataRowView)
        boton.Visible = Actor.Puede("ADMINISTRADOR", "CAJERO") AndAlso pago IsNot Nothing AndAlso CStr(pago("Estado")) = "APLICADO"
    End Sub

    Private Function MostrarComprobante(pagoId As Integer, prestamoId As Integer) As Boolean
        Dim pagos = ServicioFinanzas.Consultar("PAGOS", Actor.Id, prestamoId)
        Dim pago = pagos.AsEnumerable().FirstOrDefault(Function(r) CInt(r("Id")) = pagoId AndAlso CInt(r("PrestamoId")) = prestamoId)
        If pago Is Nothing OrElse CStr(pago("Estado")) <> "APLICADO" Then
            comprobante.Visible = False
            Notificar("No se pudo cargar el comprobante aplicado. Revise la lista de pagos registrados.", esError:=True)
            Return False
        End If

        reciboNumero.Text = Convert.ToString(pago("NumeroRecibo"), CultureInfo.InvariantCulture)
        reciboCliente.Text = Convert.ToString(pago("Cliente"), CultureInfo.InvariantCulture)
        reciboPrestamo.Text = Convert.ToString(pago("NumeroPrestamo"), CultureInfo.InvariantCulture)
        reciboFecha.Text = CDate(pago("FechaPago")).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-PA"))
        Dim formaCodigo = Convert.ToString(pago("FormaPagoCodigo"), CultureInfo.InvariantCulture)
        Dim formaLegible = CultureInfo.GetCultureInfo("es-PA").TextInfo.ToTitleCase(formaCodigo.ToLower(CultureInfo.GetCultureInfo("es-PA")))
        Dim referenciaPago = If(Convert.IsDBNull(pago("ReferenciaExterna")), String.Empty, Convert.ToString(pago("ReferenciaExterna"), CultureInfo.InvariantCulture))
        reciboForma.Text = formaLegible & If(String.IsNullOrWhiteSpace(referenciaPago), String.Empty, " · " & referenciaPago)
        reciboMonto.Text = Dinero(pago("MontoPagado"))
        reciboCapital.Text = Dinero(pago("Capital"))
        reciboInteres.Text = Dinero(pago("Interes"))
        reciboMora.Text = Dinero(pago("Mora"))
        reciboCargos.Text = Dinero(pago("OtrosCargos"))
        reciboSaldoCapital.Text = Dinero(pago("SaldoCapitalPosterior"))
        comprobante.Visible = True
        Return True
    End Function

    Private Sub AbrirDialogoComprobante()
        ClientScript.RegisterStartupScript(Me.GetType(), "abrirDialogoComprobantePago", "window.addEventListener('load',function(){var dialog=document.getElementById('dialogComprobantePago');if(dialog&&!dialog.open)dialog.showModal();});", True)
    End Sub
End Class
