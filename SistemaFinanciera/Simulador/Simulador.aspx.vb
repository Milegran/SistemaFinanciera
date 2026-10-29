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
                Lista(producto, ServicioFinanzas.Consultar("PRODUCTOS_ACTIVOS", Actor.Id))
                If producto.Items.Count > 1 Then
                    producto.SelectedIndex = 1
                    CargarOpcionesProducto()
                Else
                    condicionesProducto.Text = "No hay productos activos disponibles para simular."
                    simular.Enabled = False
                End If
                Dim hoy = ClienteServicio.HoyPanama
                primerPago.Text = hoy.AddMonths(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                primerPago.Attributes("min") = hoy.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                primerPago.Attributes("max") = hoy.AddYears(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            End Sub)
        End If
    End Sub

    Private Sub CargarOpcionesProducto()
        Dim id As Integer
        If Not Integer.TryParse(producto.SelectedValue, id) OrElse id <= 0 Then
            metodo.Items.Clear()
            metodo.Items.Add(New ListItem("Seleccione producto", ""))
            periodo.Items.Clear()
            periodo.Items.Add(New ListItem("Seleccione producto", ""))
            condicionesProducto.Text = "Selecciona un producto activo para consultar sus condiciones."
            simular.Enabled = False
            Return
        End If

        Dim metodosPermitidos = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id)
        Dim periodosPermitidos = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id)
        Lista(metodo, metodosPermitidos)
        Lista(periodo, periodosPermitidos)
        If metodo.Items.Count > 1 Then metodo.SelectedIndex = 1
        If periodo.Items.Count > 1 Then periodo.SelectedIndex = 1

        Dim datosProducto = Fila(ServicioFinanzas.Consultar("PRODUCTO_SIMULADOR", Actor.Id, id))
        Dim minimo = CDec(datosProducto("MontoMinimo"))
        Dim maximo = CDec(datosProducto("MontoMaximo"))
        monto.Attributes("min") = minimo.ToString("0.00", CultureInfo.InvariantCulture)
        monto.Attributes("max") = maximo.ToString("0.00", CultureInfo.InvariantCulture)
        If String.IsNullOrWhiteSpace(monto.Text) Then monto.Text = Math.Min(maximo, Math.Max(minimo, 1000D)).ToString("0.00", CultureInfo.InvariantCulture)
        cuotas.Attributes("min") = CInt(datosProducto("PlazoMinimo")).ToString(CultureInfo.InvariantCulture)
        cuotas.Attributes("max") = CInt(datosProducto("PlazoMaximo")).ToString(CultureInfo.InvariantCulture)
        If String.IsNullOrWhiteSpace(cuotas.Text) Then cuotas.Text = Math.Min(CInt(datosProducto("PlazoMaximo")), Math.Max(CInt(datosProducto("PlazoMinimo")), 12)).ToString(CultureInfo.InvariantCulture)

        If datosProducto.IsNull("TasaAnual") OrElse datosProducto.IsNull("TipoTasa") Then
            condicionesProducto.Text = "Este producto todavía no tiene una tasa vigente configurada."
            simular.Enabled = False
        Else
            Dim tipoTasa = If(CStr(datosProducto("TipoTasa")) = "EFECTIVA_ANUAL", "efectiva anual", "nominal anual")
            condicionesProducto.Text = String.Format(CultureInfo.GetCultureInfo("es-PA"),
                "Tasa vigente: {0:0.######}% {1} · Monto permitido: B/. {2:N2}–B/. {3:N2} · Plazo: {4}–{5} cuotas",
                CDec(datosProducto("TasaAnual")), tipoTasa, minimo, maximo, CInt(datosProducto("PlazoMinimo")), CInt(datosProducto("PlazoMaximo")))
            simular.Enabled = metodo.Items.Count > 1 AndAlso periodo.Items.Count > 1
        End If
        resultados.Visible = False
        Session.Remove("SimuladorPlan")
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
            .CantidadCuotas = CuotasSimulador(cuotas),
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

    Private Shared Function CuotasSimulador(campo As TextBox) As Integer
        Dim valor = DecimalSimulador(campo, "Cantidad de cuotas")
        If valor <> Decimal.Truncate(valor) OrElse valor > Integer.MaxValue Then
            Throw New ArgumentException("Cantidad de cuotas: escriba un número entero; las cuotas no admiten fracciones.")
        End If
        Return Decimal.ToInt32(valor)
    End Function

    Protected Sub LimpiarSimulacion(sender As Object, e As EventArgs) Handles limpiarSimulador.Click
        producto.SelectedIndex = 0
        metodo.Items.Clear()
        metodo.Items.Add(New ListItem("Seleccione producto", ""))
        periodo.Items.Clear()
        periodo.Items.Add(New ListItem("Seleccione producto", ""))
        monto.Text = String.Empty
        cuotas.Text = String.Empty
        primerPago.Text = String.Empty
        comparar.Checked = False
        condicionesProducto.Text = "Selecciona un producto activo para consultar sus condiciones."
        resultados.Visible = False
        panelComparacion.Visible = False
        simular.Enabled = False
        pnlMensaje.Visible = False
        mensaje.Text = String.Empty
        Session.Remove("SimuladorPlan")
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
        Dim comision = If(datosProducto.IsNull("ValorComision"), 0D, MotorPrestamos.Dinero(CDec(datosProducto("ValorComision"))))
        Dim gastos = If(datosProducto.IsNull("ValorGastos"), 0D, MotorPrestamos.Dinero(CDec(datosProducto("ValorGastos"))))
        If comision + gastos > solicitud.Monto Then Throw New ArgumentException("La comisión y los gastos del producto superan el monto a simular.")
        comisionEstimado.Text = FormatoDinero(comision)
        gastosEstimados.Text = FormatoDinero(gastos)
        netoEstimado.Text = FormatoDinero(solicitud.Monto - comision - gastos)
        metodoResumen.Text = metodo.SelectedItem.Text
        cantidadPeriodos.Text = plan.Count.ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        notaMetodo.Text = NotaSegunMetodo(ObtenerCodigoMetodo(solicitud.MetodoId))
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

    Protected Sub CambiarPagina(sender As Object, e As GridViewPageEventArgs) Handles tablaPlan.PageIndexChanging
        Dim plan = TryCast(Session("SimuladorPlan"), List(Of CuotaPlan))
        If plan Is Nothing Then Return
        tablaPlan.PageIndex = e.NewPageIndex
        Vincular(tablaPlan, plan)
    End Sub
End Class
