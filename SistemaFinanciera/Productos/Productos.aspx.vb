Option Strict On
Imports System.Data
Imports System.Web
Imports SistemaFinanciera.Entities
Public Class Productos
    Inherits FinanzasPagina
    Private _metodoIds As Dictionary(Of Integer, List(Of Integer))
    Private _metodoNombres As Dictionary(Of Integer, List(Of String))
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        nombre.Attributes("data-patron") = SistemaFinanciera.BL.FinanzasServicio.PatronNombreProducto
        edicion.Visible = Actor.Puede("ADMINISTRADOR")
        pnlAccionCrear.Visible = Actor.Puede("ADMINISTRADOR")
        listado.Columns(listado.Columns.Count - 1).Visible = edicion.Visible
        If Not IsPostBack Then Ejecutar(Sub()
            Lista(metodos, ServicioFinanzas.Consultar("METODOS", Actor.Id))
            Lista(periodos, ServicioFinanzas.Consultar("PERIODICIDADES_PAGO", Actor.Id))
            Lista(capitalizacion, ServicioFinanzas.Consultar("PERIODICIDADES_PAGO", Actor.Id))
            If capitalizacion.Items.Count > 1 Then capitalizacion.Items.RemoveAt(0)
            capitalizacion.Enabled = False
            Lista(tipoTasa, ServicioFinanzas.Consultar("TIPOS_TASA_ACTIVOS", Actor.Id), "Codigo")
            Lista(tipoTasaForm, ServicioFinanzas.Consultar("TIPOS_TASA_ACTIVOS", Actor.Id), "Codigo")
            Dim productosTodos = ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id)
            Lista(ddlProductoTasa, productosTodos)
            ddlProductoTasa.Items(0).Text = "Todos los productos"
            Lista(ddlProductoTasaForm, ServicioFinanzas.Consultar("PRODUCTOS_ACTIVOS", Actor.Id))
            fechaDesdeTasa.Text = Date.UtcNow.AddHours(-5).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            Limpiar() : CargarListado() : CargarTasas()
        End Sub)
    End Sub
    Private Sub CargarListado()
        Dim todos = ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id)
        _metodoIds = New Dictionary(Of Integer, List(Of Integer))()
        _metodoNombres = New Dictionary(Of Integer, List(Of String))()
        For Each fila As DataRow In todos.Rows
            Dim pid = CInt(fila("Id"))
            Dim metodosProducto = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, pid)
            _metodoIds(pid) = metodosProducto.AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
            _metodoNombres(pid) = metodosProducto.AsEnumerable().Select(Function(x) CStr(x("Nombre"))).ToList()
        Next
        Dim seleccionMetodo = ddlMetodoFiltro.SelectedValue
        ddlMetodoFiltro.Items.Clear()
        ddlMetodoFiltro.Items.Add(New ListItem("Método: Todos", ""))
        For Each m As DataRow In ServicioFinanzas.Consultar("METODOS", Actor.Id).Rows
            ddlMetodoFiltro.Items.Add(New ListItem(CStr(m("Nombre")), CStr(m("Id"))))
        Next
        If ddlMetodoFiltro.Items.FindByValue(seleccionMetodo) IsNot Nothing Then ddlMetodoFiltro.SelectedValue = seleccionMetodo
        Dim estado As Nullable(Of Boolean) = Nothing
        If ddlEstadoProducto.SelectedValue <> "" Then estado = ddlEstadoProducto.SelectedValue = "1"
        Dim termino = If(txtBuscarProducto.Text, "").Trim()
        Dim metodoId As Integer
        Dim conMetodo = Integer.TryParse(ddlMetodoFiltro.SelectedValue, metodoId)
        Dim filtrados = todos.Clone()
        For Each fila As DataRow In todos.Rows
            If CoincideProducto(fila, estado, metodoId, conMetodo, termino) Then filtrados.ImportRow(fila)
        Next
        If listado.PageIndex * listado.PageSize >= filtrados.Rows.Count Then listado.PageIndex = 0
        listado.DataSource = filtrados
        listado.DataBind()
        Dim primero = If(filtrados.Rows.Count = 0, 0, listado.PageIndex * listado.PageSize + 1)
        Dim ultimo = Math.Min(filtrados.Rows.Count, (listado.PageIndex + 1) * listado.PageSize)
        litConteo.Text = "Mostrando " & primero.ToString() & "–" & ultimo.ToString() & " de " & filtrados.Rows.Count.ToString() & " productos"
    End Sub
    Private Function CoincideProducto(fila As DataRow, estado As Nullable(Of Boolean), metodoId As Integer, conMetodo As Boolean, termino As String) As Boolean
        If estado.HasValue AndAlso CBool(fila("Activo")) <> estado.Value Then Return False
        Dim pid = CInt(fila("Id"))
        If conMetodo Then
            If Not _metodoIds.ContainsKey(pid) OrElse Not _metodoIds(pid).Contains(metodoId) Then Return False
        End If
        If String.IsNullOrEmpty(termino) Then Return True
        Return Contiene(CStr(fila("Nombre")), termino) OrElse Contiene(CStr(fila("Codigo")), termino) OrElse Contiene(If(IsDBNull(fila("Descripcion")), "", CStr(fila("Descripcion"))), termino)
    End Function
    Private Shared Function Contiene(valor As String, termino As String) As Boolean
        Return Not String.IsNullOrEmpty(valor) AndAlso valor.IndexOf(termino, StringComparison.OrdinalIgnoreCase) >= 0
    End Function
    Public Function MetodosPills(productoId As Integer) As String
        If _metodoNombres Is Nothing OrElse Not _metodoNombres.ContainsKey(productoId) Then Return ""
        Return String.Concat(_metodoNombres(productoId).Select(Function(n) "<span class=""method-pill"">" & HttpUtility.HtmlEncode(n) & "</span>"))
    End Function
    Private Sub Limpiar()
        ViewState("ProductoId") = 0
        codigo.Text = "" : nombre.Text = "" : descripcion.Text = ""
        minimo.Text = "100.00" : maximo.Text = "10000.00"
        mesesMin.Text = "1" : mesesMax.Text = "120"
        tipoComision.SelectedValue = "FIJO"
        tasa.Text = "12" : comision.Text = "0.00" : gastos.Text = "0.00"
        activo.Checked = True
        For Each item As ListItem In metodos.Items
            item.Selected = True
        Next
        For Each item As ListItem In periodos.Items
            item.Selected = True
        Next
        If capitalizacion.Items.FindByValue("1") IsNot Nothing Then capitalizacion.SelectedValue = "1"
    End Sub
    Protected Sub NuevoClick(sender As Object, e As EventArgs) Handles nuevo.Click
        ViewState.Remove("DetalleProductoId")
        Limpiar()
        ClientScript.RegisterStartupScript(GetType(Page), "abrirModalProducto", "window.__abrirModalProducto=true;", True)
    End Sub
    Private Sub CargarProductoEnFormulario(id As Integer)
        Dim filaProducto = Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))
        ViewState("ProductoId") = id
        codigo.Text = CStr(filaProducto("Codigo")) : nombre.Text = CStr(filaProducto("Nombre")) : descripcion.Text = CStr(filaProducto("Descripcion"))
        minimo.Text = Dinero(filaProducto("MontoMinimo")) : maximo.Text = Dinero(filaProducto("MontoMaximo"))
        ' Conservar el rango efectivo que admitían cuotas y meses antes de unificarlos.
        mesesMin.Text = Math.Max(CInt(filaProducto("PlazoMinimo")), CInt(Math.Ceiling(CDec(filaProducto("PlazoMinimoMeses"))))).ToString()
        mesesMax.Text = Math.Min(CInt(filaProducto("PlazoMaximo")), CInt(Math.Floor(CDec(filaProducto("PlazoMaximoMeses"))))).ToString()
        tasa.Text = Convert.ToDecimal(filaProducto("TasaAnual")).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)
        Dim tasaActual = CStr(filaProducto("TipoTasa"))
        tipoTasa.SelectedValue = If(tipoTasa.Items.FindByValue(tasaActual) Is Nothing, "", tasaActual)
        capitalizacion.SelectedIndex = 0
        tipoComision.SelectedValue = CStr(filaProducto("TipoComision"))
        comision.Text = Dinero(filaProducto("ValorComision")) : gastos.Text = Dinero(filaProducto("ValorGastos")) : activo.Checked = CBool(filaProducto("Activo"))
        Dim permitidos = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id).AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
        For Each item As ListItem In metodos.Items
            item.Selected = permitidos.Contains(CInt(item.Value))
        Next
        permitidos = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id).AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
        For Each item As ListItem In periodos.Items
            item.Selected = True
        Next
    End Sub
    Private Function CargarProducto(id As Integer) As ProductoFinanciero
        Dim filaProducto = Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))
        Dim p As New ProductoFinanciero With {.Id = id, .Codigo = CStr(filaProducto("Codigo")), .Nombre = CStr(filaProducto("Nombre")), .Descripcion = If(IsDBNull(filaProducto("Descripcion")), "", CStr(filaProducto("Descripcion"))), .MontoMinimo = Convert.ToDecimal(filaProducto("MontoMinimo")), .MontoMaximo = Convert.ToDecimal(filaProducto("MontoMaximo")), .CuotasMinimas = CInt(filaProducto("PlazoMinimo")), .CuotasMaximas = CInt(filaProducto("PlazoMaximo")), .MesesMinimos = Convert.ToDecimal(filaProducto("PlazoMinimoMeses")), .MesesMaximos = Convert.ToDecimal(filaProducto("PlazoMaximoMeses")), .TasaAnual = Convert.ToDecimal(filaProducto("TasaAnual")), .TipoTasa = CStr(filaProducto("TipoTasa")), .CapitalizacionId = CInt(filaProducto("CapitalizacionId")), .TipoComision = CStr(filaProducto("TipoComision")), .Comision = Convert.ToDecimal(filaProducto("ValorComision")), .Gastos = Convert.ToDecimal(filaProducto("ValorGastos")), .Activo = CBool(filaProducto("Activo"))}
        p.Metodos = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id).AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
        p.Periodicidades = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id).AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
        Return p
    End Function
    Protected Sub AccionProducto(sender As Object, e As GridViewCommandEventArgs) Handles listado.RowCommand
        If e.CommandName <> "Elegir" AndAlso e.CommandName <> "VerProducto" AndAlso e.CommandName <> "CambiarEstado" AndAlso e.CommandName <> "EliminarProducto" Then Return
        Ejecutar(Sub()
            Dim indice = CInt(e.CommandArgument)
            Dim id = CInt(listado.DataKeys(indice).Value)
            If e.CommandName = "VerProducto" Then
                MostrarDetalleProducto(id)
                Return
            End If
            If Not Actor.Puede("ADMINISTRADOR") Then Throw New UnauthorizedAccessException("Solo el administrador puede editar productos.")
            If e.CommandName = "EliminarProducto" Then
                ServicioFinanzas.EliminarProducto(id, Actor.Id)
                CargarListado()
                CargarTasas()
                Notificar("Producto eliminado correctamente.")
                Return
            End If
            If e.CommandName = "CambiarEstado" Then
                Dim activo = Not CBool(Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))("Activo"))
                ServicioFinanzas.CambiarEstadoProducto(id, activo, Actor.Id)
                CargarListado()
                Notificar(If(activo, "Producto reactivado.", "Producto desactivado."))
                Return
            End If
            CargarProductoEnFormulario(id)
            ClientScript.RegisterStartupScript(GetType(Page), "abrirModalProducto", "window.__abrirModalProducto=true;", True)
        End Sub)
    End Sub
    Private Sub MostrarDetalleProducto(id As Integer)
        Dim producto = Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))
        Dim nombreProducto = CStr(producto("Nombre"))
        Dim codigoProducto = CStr(producto("Codigo"))
        Dim esActivo = CBool(producto("Activo"))
        Dim moneda = System.Globalization.CultureInfo.GetCultureInfo("es-PA")
        Dim montoMinimo = Convert.ToDecimal(producto("MontoMinimo"))
        Dim montoMaximo = Convert.ToDecimal(producto("MontoMaximo"))
        Dim cuotasMinimas = CInt(producto("PlazoMinimo"))
        Dim cuotasMaximas = CInt(producto("PlazoMaximo"))
        Dim mesesMinimos = Convert.ToDecimal(producto("PlazoMinimoMeses"))
        Dim mesesMaximos = Convert.ToDecimal(producto("PlazoMaximoMeses"))
        Dim comisionFija = Convert.ToDecimal(producto("ValorComision"))
        Dim gastosFijos = Convert.ToDecimal(producto("ValorGastos"))
        Dim tasaConfigurada = Not IsDBNull(producto("TasaAnual")) AndAlso Not IsDBNull(producto("TipoTasa"))
        Dim tasaAnual = If(tasaConfigurada, Convert.ToDecimal(producto("TasaAnual")), 0D)
        Dim tipoTasaCodigo = If(tasaConfigurada, CStr(producto("TipoTasa")), "")
        Dim estadoCss = If(esActivo, "customer-status", "customer-status is-inactive")
        litProductoDetalleCabecera.Text = "<div class='customer-detail-identity'><span class='customer-detail-avatar product-detail-avatar'><svg viewBox='0 0 24 24' aria-hidden='true'><path d='M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6'/></svg></span><div><div class='customer-detail-name'><h1>" & Server.HtmlEncode(nombreProducto) & "</h1><span class='" & estadoCss & "'>" & If(esActivo, "Activo", "Inactivo") & "</span></div><p>Producto financiero · Código: " & Server.HtmlEncode(codigoProducto) & "</p></div></div>"
        litProductoMontoKpi.Text = "B/. " & montoMinimo.ToString("N2", moneda) & " – B/. " & montoMaximo.ToString("N2", moneda)
        litProductoPlazoKpi.Text = cuotasMinimas.ToString("N0", moneda) & "–" & cuotasMaximas.ToString("N0", moneda)
        litProductoTasaKpi.Text = If(tasaConfigurada, tasaAnual.ToString("0.######", moneda) & "%", "Sin tasa")
        litProductoTipoTasaKpi.Text = If(tasaConfigurada, TipoTasaTexto(tipoTasaCodigo) & " · anual", "Configure una tasa para habilitar el producto")
        litProductoCostosKpi.Text = If(CStr(producto("TipoComision")) = "PORCENTAJE", comisionFija.ToString("0.##", moneda) & "% + B/. " & gastosFijos.ToString("N2", moneda), "B/. " & (comisionFija + gastosFijos).ToString("N2", moneda))
        litProductoCodigo.Text = codigoProducto
        Dim descripcionProducto = If(IsDBNull(producto("Descripcion")), "", CStr(producto("Descripcion")))
        litProductoDescripcion.Text = If(String.IsNullOrWhiteSpace(descripcionProducto), "Sin descripción", descripcionProducto)
        litProductoMontoMinimo.Text = "B/. " & montoMinimo.ToString("N2", moneda)
        litProductoMontoMaximo.Text = "B/. " & montoMaximo.ToString("N2", moneda)
        litProductoCuotas.Text = cuotasMinimas.ToString("N0", moneda) & "–" & cuotasMaximas.ToString("N0", moneda) & " cuotas"
        litProductoMeses.Text = mesesMinimos.ToString("0.##", moneda) & "–" & mesesMaximos.ToString("0.##", moneda) & " meses"
        litProductoComision.Text = If(CStr(producto("TipoComision")) = "PORCENTAJE", comisionFija.ToString("0.##", moneda) & "% del monto solicitado", "B/. " & comisionFija.ToString("N2", moneda))
        litProductoGastos.Text = "B/. " & gastosFijos.ToString("N2", moneda)
        litProductoCapitalizacion.Text = If(IsDBNull(producto("CapitalizacionId")), "No definida", If(capitalizacion.Items.FindByValue(CStr(producto("CapitalizacionId"))) Is Nothing, "No disponible", capitalizacion.Items.FindByValue(CStr(producto("CapitalizacionId"))).Text))
        Dim metodosProducto = ServicioFinanzas.Consultar("PRODUCTO_METODOS", Actor.Id, id)
        Dim periodicidadesProducto = ServicioFinanzas.Consultar("PRODUCTO_PERIODICIDADES", Actor.Id, id)
        litProductoMetodos.Text = If(metodosProducto.Rows.Count = 0, "Sin métodos asignados", String.Join(" · ", metodosProducto.AsEnumerable().Select(Function(f) CStr(f("Nombre")))))
        litProductoPeriodicidades.Text = If(periodicidadesProducto.Rows.Count = 0, "Sin periodicidades asignadas", String.Join(" · ", periodicidadesProducto.AsEnumerable().Select(Function(f) CStr(f("Nombre")))))
        Dim todasLasTasas = ServicioFinanzas.ListarTasas(Actor.Id)
        Dim historial = todasLasTasas.Clone()
        For Each filaTasa As DataRow In todasLasTasas.Select("ProductoId = " & id.ToString(System.Globalization.CultureInfo.InvariantCulture))
            historial.ImportRow(filaTasa)
        Next
        Vincular(historialTasasDetalle, historial)
        ViewState("DetalleProductoId") = id
        hfTabActiva.Value = "productos"
        btnEstadoDetalle.Text = If(esActivo, "Desactivar producto", "Reactivar producto")
        btnEstadoDetalle.Attributes("data-confirm-action") = If(esActivo, "desactivar", "reactivar")
        btnEstadoDetalle.CssClass = If(esActivo, "btn btn-light customer-action customer-danger-action", "btn btn-light customer-action")
        Dim puedeEditar = Actor.Puede("ADMINISTRADOR")
        btnEditarDetalle.Visible = puedeEditar
        btnNuevaTasaDetalle.Visible = puedeEditar
        btnEstadoDetalle.Visible = puedeEditar
        pnlHeaderProductos.Visible = False
        pnlCatalogosProductos.Visible = False
        pnlMensaje.Visible = False
        pnlDetalleProducto.Visible = True
    End Sub
    Protected Sub VolverDetalleProducto(sender As Object, e As EventArgs) Handles btnVolverDetalle.Click
        ViewState.Remove("DetalleProductoId")
        pnlDetalleProducto.Visible = False
        pnlHeaderProductos.Visible = True
        pnlCatalogosProductos.Visible = True
        pnlMensaje.Visible = True
        hfTabActiva.Value = "productos"
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub EditarDesdeDetalle(sender As Object, e As EventArgs) Handles btnEditarDetalle.Click
        Ejecutar(Sub()
            Dim id = CInt(ViewState("DetalleProductoId"))
            CargarProductoEnFormulario(id)
            ClientScript.RegisterStartupScript(GetType(Page), "abrirModalProducto", "window.__abrirModalProducto=true;", True)
        End Sub)
    End Sub
    Protected Sub NuevaTasaDesdeDetalle(sender As Object, e As EventArgs) Handles btnNuevaTasaDetalle.Click
        Ejecutar(Sub()
            Dim id = CInt(ViewState("DetalleProductoId"))
            If ddlProductoTasaForm.Items.FindByValue(id.ToString(System.Globalization.CultureInfo.InvariantCulture)) Is Nothing Then Throw New ArgumentException("El producto ya no está disponible para asignarle una tasa.")
            ddlProductoTasaForm.SelectedValue = id.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ViewState.Remove("DetalleProductoId")
            pnlDetalleProducto.Visible = False
            pnlHeaderProductos.Visible = True
            pnlCatalogosProductos.Visible = True
            pnlMensaje.Visible = True
            hfTabActiva.Value = "tasas"
            ClientScript.RegisterStartupScript(GetType(Page), "abrirModalTasa", "window.__abrirModalTasa=true;", True)
        End Sub)
    End Sub
    Protected Sub CambiarEstadoDesdeDetalle(sender As Object, e As EventArgs) Handles btnEstadoDetalle.Click
        Ejecutar(Sub()
            If Not Actor.Puede("ADMINISTRADOR") Then Throw New UnauthorizedAccessException("Solo el administrador puede cambiar el estado del producto.")
            Dim id = CInt(ViewState("DetalleProductoId"))
            Dim activo = Not CBool(Fila(ServicioFinanzas.Consultar("PRODUCTOS", Actor.Id, id))("Activo"))
            ServicioFinanzas.CambiarEstadoProducto(id, activo, Actor.Id)
            CargarListado()
            MostrarDetalleProducto(id)
        End Sub)
    End Sub
    Protected Sub GuardarClick(sender As Object, e As EventArgs) Handles guardar.Click
        Ejecutar(Sub()
            Dim p As New ProductoFinanciero With {.Id = CInt(ViewState("ProductoId")), .Codigo = codigo.Text.Trim(), .Nombre = nombre.Text.Trim(), .Descripcion = descripcion.Text.Trim(), .MontoMinimo = Numero(minimo, "Monto mínimo"), .MontoMaximo = Numero(maximo, "Monto máximo"), .CuotasMinimas = Entero(mesesMin, "Plazo mínimo en meses"), .CuotasMaximas = Entero(mesesMax, "Plazo máximo en meses"), .MesesMinimos = Entero(mesesMin, "Plazo mínimo en meses"), .MesesMaximos = Entero(mesesMax, "Plazo máximo en meses"), .TasaAnual = Numero(tasa, "Tasa"), .TipoTasa = tipoTasa.SelectedValue, .CapitalizacionId = Seleccion(capitalizacion), .TipoComision = tipoComision.SelectedValue, .Comision = Numero(comision, "Comisión"), .Gastos = Numero(gastos, "Gastos"), .Activo = activo.Checked}
            p.Metodos = metodos.Items.Cast(Of ListItem)().Where(Function(x) x.Selected).Select(Function(x) CInt(x.Value)).ToList()
            p.Periodicidades = ServicioFinanzas.Consultar("PERIODICIDADES_PAGO", Actor.Id).AsEnumerable().Select(Function(x) CInt(x("Id"))).ToList()
            Dim id = ServicioFinanzas.GuardarProducto(p, Actor.Id)
            codigo.Text = p.Codigo
            ViewState("ProductoId") = id
            CargarListado()
            If ViewState("DetalleProductoId") IsNot Nothing AndAlso CInt(ViewState("DetalleProductoId")) = id Then
                MostrarDetalleProducto(id)
            Else
                Notificar("Producto guardado. Número interno: " & id)
            End If
        End Sub)
    End Sub
    Protected Sub FiltroCambiado(sender As Object, e As EventArgs) Handles ddlEstadoProducto.SelectedIndexChanged, ddlMetodoFiltro.SelectedIndexChanged
        listado.PageIndex = 0
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub Buscar(sender As Object, e As EventArgs) Handles btnBuscarProducto.Click
        listado.PageIndex = 0
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub LimpiarFiltros(sender As Object, e As EventArgs) Handles btnLimpiarProducto.Click
        txtBuscarProducto.Text = ""
        ddlEstadoProducto.SelectedValue = ""
        ddlMetodoFiltro.SelectedValue = ""
        listado.PageIndex = 0
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub Paginar(sender As Object, e As GridViewPageEventArgs) Handles listado.PageIndexChanging
        listado.PageIndex = e.NewPageIndex
        Ejecutar(Sub() CargarListado())
    End Sub
    Private Sub CargarTasas()
        Dim todas = ServicioFinanzas.ListarTasas(Actor.Id)
        Dim productoId As Integer
        Dim conProducto = Integer.TryParse(ddlProductoTasa.SelectedValue, productoId)
        Dim modo = If(ddlEstadoTasa.SelectedValue, "")
        Dim filtradas = todas.Clone()
        For Each fila As DataRow In todas.Rows
            If conProducto AndAlso CInt(fila("ProductoId")) <> productoId Then Continue For
            If modo = "vigente" AndAlso Not (CBool(fila("Activo")) AndAlso CBool(fila("EsPredeterminada"))) Then Continue For
            If modo = "cerrada" AndAlso (CBool(fila("Activo")) AndAlso CBool(fila("EsPredeterminada"))) Then Continue For
            filtradas.ImportRow(fila)
        Next
        If listadoTasas.PageIndex * listadoTasas.PageSize >= filtradas.Rows.Count Then listadoTasas.PageIndex = 0
        listadoTasas.DataSource = filtradas
        listadoTasas.DataBind()
        Dim primero = If(filtradas.Rows.Count = 0, 0, listadoTasas.PageIndex * listadoTasas.PageSize + 1)
        Dim ultimo = Math.Min(filtradas.Rows.Count, (listadoTasas.PageIndex + 1) * listadoTasas.PageSize)
        litConteoTasas.Text = "Mostrando " & primero.ToString() & "–" & ultimo.ToString() & " de " & filtradas.Rows.Count.ToString() & " tasas"
    End Sub
    Protected Sub FiltroTasaCambiado(sender As Object, e As EventArgs) Handles ddlProductoTasa.SelectedIndexChanged, ddlEstadoTasa.SelectedIndexChanged
        listadoTasas.PageIndex = 0
        Ejecutar(Sub() CargarTasas())
    End Sub
    Protected Sub LimpiarFiltrosTasas(sender As Object, e As EventArgs) Handles btnLimpiarTasas.Click
        ddlProductoTasa.SelectedValue = ""
        ddlEstadoTasa.SelectedValue = ""
        listadoTasas.PageIndex = 0
        Ejecutar(Sub() CargarTasas())
    End Sub
    Protected Sub PaginarTasas(sender As Object, e As GridViewPageEventArgs) Handles listadoTasas.PageIndexChanging
        listadoTasas.PageIndex = e.NewPageIndex
        Ejecutar(Sub() CargarTasas())
    End Sub
    Protected Sub GuardarTasaClick(sender As Object, e As EventArgs) Handles btnGuardarTasa.Click
        Ejecutar(Sub()
            Dim productoId = Seleccion(ddlProductoTasaForm)
            Dim porcentaje = Numero(tasaValor, "Tasa")
            Dim tipo = tipoTasaForm.SelectedValue
            Dim fechaTasa As Date = Fecha(fechaDesdeTasa)
            Dim id = ServicioFinanzas.GuardarTasa(productoId, porcentaje, tipo, fechaTasa, Actor.Id)
            fechaDesdeTasa.Text = Date.UtcNow.AddHours(-5).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            tasaValor.Text = ""
            CargarTasas() : CargarListado()
            Notificar("Tasa publicada. Número interno: " & id & ". La tasa anterior quedó cerrada.")
            ClientScript.RegisterStartupScript(GetType(Page), "cerrarModalTasa", "window.__cerrarModalTasa=true;", True)
        End Sub)
    End Sub
    Public Function TipoTasaTexto(codigo As Object) As String
        Dim c = If(codigo Is Nothing OrElse IsDBNull(codigo), "", CStr(codigo))
        If c = "NOMINAL_ANUAL" Then Return "Nominal anual"
        If c = "EFECTIVA_ANUAL" Then Return "Efectiva anual"
        Return c
    End Function
    Public Function FechaCorta(valor As Object) As String
        If valor Is Nothing OrElse IsDBNull(valor) Then Return "—"
        Return CDate(valor).ToString("dd-MMM-yyyy", New System.Globalization.CultureInfo("es-PA"))
    End Function
    Protected Sub ProductoRowDataBound(sender As Object, e As GridViewRowEventArgs) Handles listado.RowDataBound
        If e.Row.RowIndex < 0 Then Return
        Dim ver = TryCast(e.Row.FindControl("btnVerProducto"), LinkButton)
        If ver Is Nothing Then Return
        e.Row.Attributes("data-view-button") = ver.ClientID
        e.Row.Attributes("tabindex") = "0"
        e.Row.CssClass = (e.Row.CssClass & " client-row-clickable").Trim()
    End Sub
End Class
