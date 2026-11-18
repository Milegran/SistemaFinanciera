Option Strict On
Imports System.Data
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Web.UI.WebControls
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.Entities

Public Class Simulador
    Inherits FinanzasPagina

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Ejecutar(Sub()
                ReiniciarPlazo()
                CargarMensual()
                Lista(producto, ServicioFinanzas.Consultar("PRODUCTOS_OPERABLES", Actor.Id))
                If producto.Items.Count > 1 Then
                    producto.SelectedIndex = 1
                    CargarOpcionesProducto()
                Else
                    condicionesProducto.Text = "No hay productos activos disponibles para simular."
                    simular.Enabled = False
                End If
                CargarClientesCotizacion()
                Dim hoy = ClienteServicio.HoyPanama
                primerPago.Text = hoy.AddMonths(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                primerPago.Attributes("min") = hoy.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                primerPago.Attributes("max") = hoy.AddYears(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            End Sub)
        End If
    End Sub

    Private Sub CargarOpcionesProducto()
        ReiniciarPlazo()
        CargarMensual()
        resultados.Visible = False
        panelComparacion.Visible = False
        simular.Enabled = False
        Session.Remove("SimuladorPlan")
        Dim id As Integer
        If Not Integer.TryParse(producto.SelectedValue, id) OrElse id <= 0 Then
            metodo.Items.Clear()
            metodo.Items.Add(New ListItem("Seleccione producto", ""))

            condicionesProducto.Text = "Selecciona un producto activo para consultar sus condiciones."
            simular.Enabled = False
            Return
        End If

        Dim metodosPermitidos = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id)
        Dim periodosPermitidos = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id)
        Lista(metodo, metodosPermitidos)
        If periodosPermitidos.Rows.Count = 0 Then Throw New ArgumentException("El producto debe permitir pagos mensuales.")
        If metodo.Items.Count > 1 Then metodo.SelectedIndex = 1
        If periodo.Items.Count > 1 Then periodo.SelectedIndex = 1

        Dim datosProducto = Fila(ServicioFinanzas.Consultar("PRODUCTO_SIMULADOR", Actor.Id, id))
        Dim minimo = CDec(datosProducto("MontoMinimo"))
        Dim maximo = CDec(datosProducto("MontoMaximo"))
        monto.Attributes("min") = minimo.ToString("0.00", CultureInfo.InvariantCulture)
        monto.Attributes("max") = maximo.ToString("0.00", CultureInfo.InvariantCulture)
        If String.IsNullOrWhiteSpace(monto.Text) Then monto.Text = Math.Min(maximo, Math.Max(minimo, 1000D)).ToString("0.00", CultureInfo.InvariantCulture)
        Dim plazoMinimo = Math.Max(CInt(datosProducto("PlazoMinimo")), CInt(Math.Ceiling(CDec(datosProducto("PlazoMinimoMeses")))))
        Dim plazoMaximo = Math.Min(CInt(datosProducto("PlazoMaximo")), CInt(Math.Floor(CDec(datosProducto("PlazoMaximoMeses")))))
        plazoMinimo = Math.Max(1, plazoMinimo)
        plazoMaximo = Math.Min(120, plazoMaximo)
        If plazoMinimo > plazoMaximo Then Throw New ArgumentException("El producto no tiene plazos mensuales válidos. Revise sus límites en Productos.")
        cuotas.Items.Clear()
        cuotas.Items.Add(New ListItem("Seleccione el plazo", ""))
        For meses As Integer = plazoMinimo To plazoMaximo
            cuotas.Items.Add(New ListItem(meses.ToString() & " meses · " & meses.ToString() & " cuotas mensuales", meses.ToString(CultureInfo.InvariantCulture)))
        Next
        cuotas.Enabled = True
        cuotas.SelectedIndex = 1

        If datosProducto.IsNull("TasaAnual") OrElse datosProducto.IsNull("TipoTasa") Then
            condicionesProducto.Text = "Este producto todavía no tiene una tasa vigente configurada."
            simular.Enabled = False
        Else
            Dim tipoTasa = If(CStr(datosProducto("TipoTasa")) = "EFECTIVA_ANUAL", "efectiva anual", "nominal anual")
            condicionesProducto.Text = String.Format(CultureInfo.GetCultureInfo("es-PA"),
                "Tasa vigente: {0:0.######}% {1} · Monto permitido: B/. {2:N2}–B/. {3:N2} · Plazo: {4}–{5} meses · Una cuota mensual",
                CDec(datosProducto("TasaAnual")), tipoTasa, minimo, maximo, plazoMinimo, plazoMaximo)
            simular.Enabled = metodo.Items.Count > 1 AndAlso periodosPermitidos.Rows.Count > 0
        End If
        resultados.Visible = False
        Session.Remove("SimuladorPlan")
        Session.Remove("SimuladorCotizacion")
    End Sub

    Protected Sub ProductoCambiado(sender As Object, e As EventArgs) Handles producto.SelectedIndexChanged
        Ejecutar(Sub() CargarOpcionesProducto())
    End Sub

    Private Function Datos(metodoId As Integer) As SolicitudFinanciera
        Return New SolicitudFinanciera With {
            .ProductoId = Seleccion(producto),
            .MetodoId = metodoId,
            .PeriodicidadId = Seleccion(periodo),
            .Monto = DecimalSimulador(monto, "Monto"),
            .CantidadCuotas = Seleccion(cuotas),
            .FechaPrimerPago = Fecha(primerPago)
        }
    End Function

    Private Shared Function DecimalSimulador(campo As TextBox, nombre As String) As Decimal
        Dim texto = If(campo.Text, String.Empty).Trim()
        If Not System.Text.RegularExpressions.Regex.IsMatch(texto, "^\d{1,12}(\.\d{1,2})?$") Then
            Throw New ArgumentException(nombre & ": use hasta 12 dígitos y máximo 2 decimales, separados por punto.")
        End If
        Dim valor As Decimal
        If Not Decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, valor) OrElse valor <= 0D Then
            Throw New ArgumentException(nombre & ": escriba un valor mayor que cero.")
        End If
        Return valor
    End Function

    Private Sub ReiniciarPlazo()
        cuotas.Items.Clear()
        cuotas.Items.Add(New ListItem("Seleccione primero un producto", ""))
        cuotas.Enabled = False
    End Sub

    Private Sub CargarMensual()
        Dim mensual = ServicioFinanzas.Consultar("PERIODICIDADES_PAGO", Actor.Id)
        periodo.Items.Clear()
        If mensual.Rows.Count = 0 Then Throw New InvalidOperationException("No está configurada la periodicidad mensual.")
        periodo.Items.Add(New ListItem("Mensual", Convert.ToString(mensual.Rows(0)("Id"), CultureInfo.InvariantCulture)))
        periodo.SelectedIndex = 0
        periodo.Enabled = False
    End Sub

    Protected Sub LimpiarSimulacion(sender As Object, e As EventArgs) Handles limpiarSimulador.Click
        producto.SelectedIndex = 0
        metodo.Items.Clear()
        metodo.Items.Add(New ListItem("Seleccione producto", ""))
        CargarMensual()
        monto.Text = String.Empty
        ReiniciarPlazo()
        primerPago.Text = String.Empty
        comparar.Checked = False
        condicionesProducto.Text = "Selecciona un producto activo para consultar sus condiciones."
        resultados.Visible = False
        panelComparacion.Visible = False
        simular.Enabled = False
        pnlMensaje.Visible = False
        mensaje.Text = String.Empty
        Session.Remove("SimuladorPlan")
        Session.Remove("SimuladorCotizacion")
        LimpiarClienteCotizacion()
    End Sub

    Protected Sub SimularClick(sender As Object, e As EventArgs) Handles simular.Click
        Ejecutar(Sub()
            Dim solicitud = Datos(Seleccion(metodo))
            Dim plan = ServicioFinanzas.SimularProducto(solicitud, Actor.Id)
            Session("SimuladorPlan") = plan
            MostrarResultados(solicitud, plan)
            If comparar.Checked Then CargarComparacion(solicitud)
        End Sub)
    End Sub

    Private Sub MostrarResultados(solicitud As SolicitudFinanciera, plan As List(Of CuotaPlan))
        If plan Is Nothing OrElse plan.Count = 0 Then Throw New InvalidOperationException("No se pudo generar el calendario de pagos.")
        totalCapital.Text = FormatoDinero(solicitud.Monto)
        totalIntereses.Text = FormatoDinero(plan.Sum(Function(q) q.Interes))
        totalPagar.Text = FormatoDinero(plan.Sum(Function(q) q.Cuota))
        cuotaMaxima.Text = FormatoDinero(plan.Max(Function(q) q.Cuota))
        Dim datosProducto = Fila(ServicioFinanzas.Consultar("PRODUCTO_SIMULADOR", Actor.Id, solicitud.ProductoId))
        Dim comision = If(datosProducto.IsNull("ValorComision"), 0D, SistemaFinanciera.DAL.FinanzasRepositorio.CalcularComision(CStr(datosProducto("TipoComision")), CDec(datosProducto("ValorComision")), solicitud.Monto))
        Dim gastos = If(datosProducto.IsNull("ValorGastos"), 0D, MotorPrestamos.Dinero(CDec(datosProducto("ValorGastos"))))
        If comision + gastos > solicitud.Monto Then Throw New ArgumentException("La comisión y los gastos del producto superan el monto a simular.")
        comisionEstimado.Text = FormatoDinero(comision)
        gastosEstimados.Text = FormatoDinero(gastos)
        netoEstimado.Text = FormatoDinero(solicitud.Monto - comision - gastos)
        metodoResumen.Text = metodo.SelectedItem.Text
        cantidadPeriodos.Text = plan.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        notaMetodo.Text = NotaSegunMetodo(ObtenerCodigoMetodo(solicitud.MetodoId))
        ' Base de la cotización: el mismo número y datos para imprimir, PDF y Excel.
        Dim ahoraPanama = DateTime.UtcNow.AddHours(-5)
        Session("SimuladorCotizacion") = New CotizacionPrestamo With {
            .Numero = "COT-" & ahoraPanama.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture),
            .FechaEmision = ahoraPanama,
            .Producto = producto.SelectedItem.Text,
            .Metodo = metodo.SelectedItem.Text,
            .NotaMetodo = notaMetodo.Text,
            .Periodicidad = If(periodo.SelectedItem Is Nothing, "Mensual", periodo.SelectedItem.Text),
            .TasaAnual = If(datosProducto.IsNull("TasaAnual"), 0D, CDec(datosProducto("TasaAnual"))),
            .TipoTasa = If(datosProducto.IsNull("TipoTasa"), "", CStr(datosProducto("TipoTasa"))),
            .Monto = solicitud.Monto,
            .Plazo = plan.Count,
            .FechaPrimerPago = solicitud.FechaPrimerPago,
            .Comision = comision,
            .Gastos = gastos,
            .Asesor = If(Actor Is Nothing, "", Actor.Nombre),
            .Plan = plan}
        documentoCotizacion.Text = ""
        tablaPlan.PageIndex = 0
        Vincular(tablaPlan, plan)
        resultados.Visible = True
        panelComparacion.Visible = False
    End Sub

    Private Sub CargarComparacion(solicitud As SolicitudFinanciera)
        Dim comparacion As New DataTable()
        comparacion.Columns.Add("Metodo", GetType(String))
        comparacion.Columns.Add("CuotaMaxima", GetType(String))
        comparacion.Columns.Add("Intereses", GetType(String))
        comparacion.Columns.Add("Total", GetType(String))
        comparacion.Columns.Add("Nota", GetType(String))

        For Each opcion As DataRow In ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, solicitud.ProductoId).Rows
            Dim nombre = CStr(opcion("Nombre"))
            Dim fila = comparacion.NewRow()
            fila("Metodo") = nombre
            Try
                Dim alternativa = Datos(CInt(opcion("Id")))
                alternativa.ProductoId = solicitud.ProductoId
                alternativa.MetodoId = CInt(opcion("Id"))
                alternativa.PeriodicidadId = solicitud.PeriodicidadId
                alternativa.Monto = solicitud.Monto
                alternativa.CantidadCuotas = solicitud.CantidadCuotas
                alternativa.FechaPrimerPago = solicitud.FechaPrimerPago
                Dim plan = ServicioFinanzas.SimularProducto(alternativa, Actor.Id)
                fila("CuotaMaxima") = FormatoDinero(plan.Max(Function(q) q.Cuota))
                fila("Intereses") = FormatoDinero(plan.Sum(Function(q) q.Interes))
                fila("Total") = FormatoDinero(plan.Sum(Function(q) q.Cuota))
                fila("Nota") = If(CInt(opcion("Id")) = solicitud.MetodoId, "Método seleccionado", "Disponible")
            Catch ex As ArgumentException
                fila("CuotaMaxima") = "—"
                fila("Intereses") = "—"
                fila("Total") = "—"
                fila("Nota") = "No compatible con la tasa: " & ex.Message
            End Try
            comparacion.Rows.Add(fila)
        Next
        Vincular(tablaComparacion, comparacion)
        panelComparacion.Visible = True
    End Sub

    Private Function ObtenerCodigoMetodo(metodoId As Integer) As String
        Dim catalogo = ServicioFinanzas.Consultar("METODOS", Actor.Id)
        Dim opciones = catalogo.Select("Id = " & metodoId.ToString(CultureInfo.InvariantCulture))
        Return If(opciones.Length = 0, "", CStr(opciones(0)("Codigo")))
    End Function

    Private Shared Function NotaSegunMetodo(codigo As String) As String
        Select Case codigo
            Case "FRANCES" : Return "Cuotas niveladas; el último período puede ajustar centavos para cerrar el saldo."
            Case "ALEMAN" : Return "Abono de capital uniforme; la cuota disminuye conforme baja el saldo."
            Case "AMERICANO" : Return "Intereses por período y devolución completa del capital en la última cuota."
            Case "SIMPLE" : Return "Interés simple proporcional al plazo, distribuido entre las cuotas."
            Case "COMPUESTO" : Return "Capitalización proyectada y pago global al vencimiento; no hay cobros intermedios."
            Case Else : Return "Calendario generado con el método configurado para el producto."
        End Select
    End Function

    Private Shared Function FormatoDinero(valor As Decimal) As String
        Return "B/. " & valor.ToString("N2", CultureInfo.GetCultureInfo("es-PA"))
    End Function

    ' ---------- Cotización: cliente, impresión, PDF y Excel ----------

    Private Sub CargarClientesCotizacion()
        clienteCotizacion.Items.Clear()
        clienteCotizacion.Items.Add(New ListItem("Cliente no registrado (escribir datos)", ""))
        Try
            For Each fila As DataRow In ServicioFinanzas.Consultar("CLIENTES", Actor.Id).Rows
                clienteCotizacion.Items.Add(New ListItem(Convert.ToString(fila("Nombre"), CultureInfo.InvariantCulture), Convert.ToString(fila("Id"), CultureInfo.InvariantCulture)))
            Next
        Catch ex As Exception When TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException
            ' Sin permiso para consultar clientes: solo se permite escribir los datos.
            clienteCotizacion.Enabled = False
        End Try
    End Sub

    Private Sub LimpiarClienteCotizacion()
        If clienteCotizacion.Items.Count > 0 Then clienteCotizacion.SelectedIndex = 0
        For Each campo In {nombreCotizacion, identificacionCotizacion, telefonoCotizacion, correoCotizacion}
            campo.Text = String.Empty
            campo.ReadOnly = False
        Next
    End Sub

    Protected Sub ClienteCotizacionCambiado(sender As Object, e As EventArgs) Handles clienteCotizacion.SelectedIndexChanged
        Ejecutar(Sub()
            Dim id As Integer
            If Not Integer.TryParse(clienteCotizacion.SelectedValue, id) OrElse id <= 0 Then
                LimpiarClienteCotizacion()
                Return
            End If
            Dim config = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            Dim cliente = New ClienteServicio(New SistemaFinanciera.DAL.ClienteRepositorio(config.ConnectionString)).Obtener(id)
            If cliente Is Nothing Then Throw New InvalidOperationException("No se encontró el cliente seleccionado.")
            nombreCotizacion.Text = cliente.NombreCompleto
            identificacionCotizacion.Text = If(cliente.Identificacion, "")
            telefonoCotizacion.Text = If(cliente.Telefono, "")
            correoCotizacion.Text = If(cliente.Correo, "")
            For Each campo In {nombreCotizacion, identificacionCotizacion, telefonoCotizacion, correoCotizacion}
                campo.ReadOnly = True
            Next
        End Sub)
    End Sub

    ''' <summary>Abre el desplegable de datos del cliente si ya hay alguno escrito o seleccionado.</summary>
    Protected Function ClienteCotizacionConDatos() As Boolean
        Return clienteCotizacion.SelectedIndex > 0 OrElse {nombreCotizacion, identificacionCotizacion, telefonoCotizacion, correoCotizacion}.Any(Function(campo) Not String.IsNullOrWhiteSpace(campo.Text))
    End Function

    Private Function CotizacionActual() As CotizacionPrestamo
        Dim c = TryCast(Session("SimuladorCotizacion"), CotizacionPrestamo)
        If c Is Nothing OrElse c.Plan Is Nothing OrElse c.Plan.Count = 0 Then Throw New InvalidOperationException("Calcula la simulación antes de generar la cotización.")
        Dim nombre = nombreCotizacion.Text.Trim()
        Dim identificacion = identificacionCotizacion.Text.Trim()
        Dim telefono = telefonoCotizacion.Text.Trim()
        Dim correo = correoCotizacion.Text.Trim()
        ' Los datos del cliente son opcionales; si se escribe alguno, el nombre es necesario para identificarlo.
        If nombre = "" AndAlso (identificacion <> "" OrElse telefono <> "" OrElse correo <> "") Then Throw New ArgumentException("Cotización: escribe el nombre del cliente o deja todos sus datos vacíos.")
        If nombre <> "" AndAlso (nombre.Length < 3 OrElse nombre.Length > 100 OrElse Not RegularExpressions.Regex.IsMatch(nombre, "^[\p{L}][\p{L} .'\-]*$")) Then Throw New ArgumentException("Cotización: el nombre del cliente admite solo letras, espacios, punto, apóstrofo o guion (3 a 100 caracteres).")
        If identificacion.Length > 30 OrElse (identificacion <> "" AndAlso Not RegularExpressions.Regex.IsMatch(identificacion, "^[A-Za-z0-9][A-Za-z0-9\-]*$")) Then Throw New ArgumentException("Cotización: la identificación solo admite letras, números y guiones (máximo 30).")
        If telefono <> "" AndAlso Not RegularExpressions.Regex.IsMatch(telefono, "^\+?[0-9][0-9 \-]{6,14}$") Then Throw New ArgumentException("Cotización: escribe un teléfono válido (7 a 15 dígitos).")
        If correo.Length > 100 OrElse (correo <> "" AndAlso Not RegularExpressions.Regex.IsMatch(correo, "^[^\s@<>]+@[^\s@<>]+\.[^\s@<>]+$")) Then Throw New ArgumentException("Cotización: escribe un correo válido. Ej.: nombre@dominio.com.")
        c.ClienteNombre = nombre
        c.ClienteIdentificacion = identificacion
        c.ClienteTelefono = telefono
        c.ClienteCorreo = correo
        c.ClienteRegistrado = clienteCotizacion.SelectedValue <> ""
        If String.IsNullOrWhiteSpace(c.Asesor) AndAlso Actor IsNot Nothing Then c.Asesor = Actor.Nombre
        Return c
    End Function

    Private ReadOnly Property RutaLogo As String
        Get
            Return Server.MapPath("~/Content/Images/capitalwest-login.png")
        End Get
    End Property

    Protected Sub ImprimirCotizacionClick(sender As Object, e As EventArgs) Handles imprimirCotizacion.Click
        Ejecutar(Sub()
            Dim c = CotizacionActual()
            ' Logo incrustado (data URI): no depende de rutas ni de que la imagen termine de cargar antes de imprimir.
            Dim logo = If(IO.File.Exists(RutaLogo), "data:image/png;base64," & Convert.ToBase64String(IO.File.ReadAllBytes(RutaLogo)), ResolveUrl("~/Content/Images/capitalwest-login.png"))
            documentoCotizacion.Text = c.Html(logo)
            ClientScript.RegisterStartupScript(Me.GetType(), "imprimirCotizacion", "window.addEventListener('load',function(){setTimeout(function(){window.print();},250);});", True)
        End Sub)
    End Sub

    Protected Sub PdfCotizacionClick(sender As Object, e As EventArgs) Handles pdfCotizacion.Click
        Ejecutar(Sub()
            Dim c = CotizacionActual()
            EnviarArchivo(CotizacionPdf.Generar(c, RutaLogo), "application/pdf", "Cotizacion-" & c.Numero & ".pdf")
        End Sub)
    End Sub

    Protected Sub ExcelCotizacionClick(sender As Object, e As EventArgs) Handles excelCotizacion.Click
        Ejecutar(Sub()
            Dim c = CotizacionActual()
            EnviarArchivo(CotizacionExcel.Generar(c, RutaLogo), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cotizacion-" & c.Numero & ".xlsx")
        End Sub)
    End Sub

    Private Sub EnviarArchivo(contenido As Byte(), tipo As String, nombre As String)
        Response.Clear()
        Response.ContentType = tipo
        Response.AddHeader("Content-Disposition", "attachment; filename=""" & nombre & """")
        Response.AddHeader("Content-Length", contenido.Length.ToString(CultureInfo.InvariantCulture))
        Response.BinaryWrite(contenido)
        Response.Flush()
        Response.SuppressContent = True
        Context.ApplicationInstance.CompleteRequest()
    End Sub

    Protected Sub CambiarPagina(sender As Object, e As GridViewPageEventArgs) Handles tablaPlan.PageIndexChanging
        Dim plan = TryCast(Session("SimuladorPlan"), List(Of CuotaPlan))
        If plan Is Nothing Then Return
        tablaPlan.PageIndex = e.NewPageIndex
        Vincular(tablaPlan, plan)
    End Sub
End Class
