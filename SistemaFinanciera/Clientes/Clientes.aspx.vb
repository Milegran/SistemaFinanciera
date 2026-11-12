Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Text
Imports SistemaFinanciera.Entities
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL

Public Class Clientes
    Inherits FinanzasPagina
    Public ReadOnly Property ReglasClienteJson As String
        Get
            Return New System.Web.Script.Serialization.JavaScriptSerializer().Serialize(New With {
                .nombre = ClienteServicio.PatronNombre, .cedula = ClienteServicio.PatronCedula,
                .pasaporte = ClienteServicio.PatronPasaporte, .ruc = ClienteServicio.PatronRuc,
                .telefono = ClienteServicio.PatronTelefono,
                .fechaMaxima = ClienteServicio.HoyPanama.ToString("yyyy-MM-dd"),
                .fechaMinima = ClienteServicio.HoyPanama.AddYears(-120).ToString("yyyy-MM-dd")})
        End Get
    End Property
    Private ReadOnly Property Servicio As ClienteServicio
        Get
            Dim config = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If config Is Nothing Then Throw New InvalidOperationException("No está configurada la conexión del sistema.")
            Return New ClienteServicio(New ClienteRepositorio(config.ConnectionString))
        End Get
    End Property
    Private Property ClienteId As Integer
        Get
            Return If(ViewState("ClienteId") Is Nothing, 0, CInt(ViewState("ClienteId")))
        End Get
        Set(value As Integer)
            ViewState("ClienteId") = value
        End Set
    End Property
    Protected Sub Page_Init(sender As Object, e As EventArgs) Handles Me.Init
        ddlEstadoCivil.Items.Clear()
        ddlEstadoCivil.Items.Add(New ListItem("Seleccione...", ""))
        For Each opcion In Servicio.EstadosCivilesActivos()
            ddlEstadoCivil.Items.Add(New ListItem(opcion.Value, opcion.Key))
        Next
        ddlEstadoCivil.Items.Add(New ListItem("No aplica", "NO_APLICA"))
    End Sub
    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        txtFechaNacimiento.Attributes("min") = ClienteServicio.HoyPanama.AddYears(-120).ToString("yyyy-MM-dd")
        txtFechaNacimiento.Attributes("max") = ClienteServicio.HoyPanama.ToString("yyyy-MM-dd")
        pnlMensaje.Visible = False
        pnlErrorFormulario.Visible = False
        If IsPostBack AndAlso pnlFormularioCliente.Visible AndAlso ClienteId > 0 Then
            Dim clienteActual = Servicio.Obtener(ClienteId)
            If clienteActual.TipoPersona = "NATURAL" AndAlso Not String.IsNullOrWhiteSpace(clienteActual.EstadoCivil) AndAlso ddlEstadoCivil.Items.FindByValue(clienteActual.EstadoCivil) Is Nothing Then
                ddlEstadoCivil.Items.Insert(1, New ListItem(TextoLegible(clienteActual.EstadoCivil) & " (inactivo · registro actual)", clienteActual.EstadoCivil))
                If String.IsNullOrEmpty(ddlEstadoCivil.SelectedValue) Then ddlEstadoCivil.SelectedValue = clienteActual.EstadoCivil
            End If
        End If
        If Not IsPostBack Then
            litKpiActivos.Text = "—"
            litKpiInactivos.Text = "—"
            litKpiPrestamo.Text = "—"
            litCarteraActiva.Text = "—"
            EjecutarCliente(Sub()
                         CargarListado()
                     End Sub)
        End If
    End Sub
    Private Sub EjecutarCliente(operacion As Action)
        Try
            operacion()
        Catch ex As ArgumentException
            Mensaje(ex.Message, True)
        Catch ex As InvalidOperationException
            Mensaje(ex.Message, True)
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Clientes: {0}", ex.ToString())
            Mensaje("No se pudo completar la operación. Verifique la conexión a la base de datos o contacte al administrador.", True)
        End Try
    End Sub
    Private Sub Mensaje(texto As String, errorOperacion As Boolean)
        If pnlFormularioCliente.Visible AndAlso errorOperacion Then
            pnlErrorFormulario.Visible = True
            pnlErrorFormulario.Style("white-space") = "pre-line"
            litErrorFormulario.Text = texto
            Return
        End If
        pnlMensaje.Visible = True
        pnlMensaje.CssClass = If(errorOperacion, "alert alert-danger", "alert alert-success")
        pnlMensaje.Style("white-space") = "pre-line"
        litMensaje.Text = texto
    End Sub
    Private Sub CargarListado()
        Dim todos = Servicio.Listar("", Nothing)
        Dim resumen = ServicioFinanzas.ObtenerPanelClientes(Actor.Id)
        Dim prestamosPorCliente As New Dictionary(Of Integer, Integer)()
        For Each fila As DataRow In resumen.Tables("ClientesConPrestamo").Rows
            prestamosPorCliente(CInt(fila("ClienteId"))) = CInt(fila("PrestamosActivos"))
        Next
        For Each cliente In todos
            cliente.PrestamosActivos = If(prestamosPorCliente.ContainsKey(cliente.Id), prestamosPorCliente(cliente.Id), 0)
        Next
        litKpiActivos.Text = System.Linq.Enumerable.Count(todos, Function(c) c.Activo).ToString()
        litKpiInactivos.Text = System.Linq.Enumerable.Count(todos, Function(c) Not c.Activo).ToString()
        Dim indicadores = resumen.Tables("Indicadores").Rows(0)
        litKpiPrestamo.Text = CInt(indicadores("ClientesConPrestamo")).ToString("N0", CultureInfo.GetCultureInfo("es-PA"))
        litCarteraActiva.Text = Moneda(CDec(indicadores("CarteraActiva")))
        Dim seleccion = ddlProvinciaFiltro.SelectedValue
        ddlProvinciaFiltro.Items.Clear()
        ddlProvinciaFiltro.Items.Add(New ListItem("Provincia: Todas", ""))
        For Each provincia In todos.Select(Function(c) c.Provincia).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(v) v)
            ddlProvinciaFiltro.Items.Add(New ListItem(provincia, provincia))
        Next
        If ddlProvinciaFiltro.Items.FindByValue(seleccion) IsNot Nothing Then ddlProvinciaFiltro.SelectedValue = seleccion
        Dim estado As Nullable(Of Boolean) = Nothing
        If ddlEstado.SelectedValue <> "" Then estado = ddlEstado.SelectedValue = "1"
        Dim termino = If(txtBuscar.Text, "").Trim()
        Dim provinciaFiltro = ddlProvinciaFiltro.SelectedValue
        Dim personaFiltro = ddlPersonaFiltro.SelectedValue
        Dim soloConPrestamo = hidFiltroPrestamo.Value = "1"
        Dim clientes = todos.Where(Function(c) CoincideFiltroCliente(c, estado, provinciaFiltro, personaFiltro, soloConPrestamo, termino)).ToList()
        If gvClientes.PageIndex * gvClientes.PageSize >= clientes.Count Then gvClientes.PageIndex = 0
        gvClientes.DataSource = clientes
        gvClientes.DataBind()
        Dim primero = If(clientes.Count = 0, 0, gvClientes.PageIndex * gvClientes.PageSize + 1)
        Dim ultimo = Math.Min(clientes.Count, (gvClientes.PageIndex + 1) * gvClientes.PageSize)
        litConteo.Text = "Mostrando " & primero.ToString() & "–" & ultimo.ToString() & " de " & clientes.Count.ToString() & " clientes"
    End Sub
    Private Shared Function CoincideFiltroCliente(cliente As Cliente, estado As Nullable(Of Boolean), provincia As String, tipoPersona As String, soloConPrestamo As Boolean, termino As String) As Boolean
        If estado.HasValue AndAlso cliente.Activo <> estado.Value Then Return False
        If Not String.IsNullOrEmpty(provincia) AndAlso Not String.Equals(cliente.Provincia, provincia, StringComparison.OrdinalIgnoreCase) Then Return False
        If Not String.IsNullOrEmpty(tipoPersona) AndAlso cliente.TipoPersona <> tipoPersona Then Return False
        If soloConPrestamo AndAlso cliente.PrestamosActivos = 0 Then Return False
        If String.IsNullOrEmpty(termino) Then Return True
        Return Contiene(cliente.NombreCompleto, termino) OrElse Contiene(cliente.Identificacion, termino) OrElse Contiene(cliente.Telefono, termino) OrElse Contiene(cliente.Correo, termino)
    End Function
    Private Shared Function Contiene(valor As String, termino As String) As Boolean
        Return If(valor, String.Empty).IndexOf(termino, StringComparison.OrdinalIgnoreCase) >= 0
    End Function
    Private Sub MostrarFormulario(c As Cliente)
        ClienteId = c.Id
        hidFormTab.Value = "personal"
        pnlPerfil.Visible = False
        ViewState("ClienteVersion") = c.Version
        litTituloFormulario.Text = If(c.Id = 0, "Registrar cliente", "Consultar / editar cliente")
        ddlTipoPersona.SelectedValue = If(c.TipoPersona, "")
        ddlTipoIdentificacion.SelectedValue = If(c.TipoIdentificacion, "")
        txtIdentificacion.Text = If(c.Identificacion, "")
        txtNombres.Text = If(c.Nombres, "")
        txtApellidos.Text = If(c.Apellidos, "")
        ddlSexo.SelectedValue = If(c.Sexo, "")
        If c.Id > 0 AndAlso c.TipoPersona = "NATURAL" AndAlso Not String.IsNullOrWhiteSpace(c.EstadoCivil) AndAlso ddlEstadoCivil.Items.FindByValue(c.EstadoCivil) Is Nothing Then
            ddlEstadoCivil.Items.Insert(1, New ListItem(TextoLegible(c.EstadoCivil) & " (inactivo · registro actual)", c.EstadoCivil))
        End If
        ddlEstadoCivil.SelectedValue = If(c.EstadoCivil, "")
        txtTelefono.Text = If(c.Telefono, "")
        txtCorreo.Text = If(c.Correo, "")
        txtDireccion.Text = If(c.Direccion, "")
        CargarUbicacion(ddlProvincia, 1, c.CodigoProvincia)
        CargarUbicacion(ddlDistrito, 2, c.CodigoDistrito)
        CargarUbicacion(ddlCorregimiento, 3, c.CodigoCorregimiento)
        txtOcupacion.Text = If(c.Ocupacion, "")
        txtReferencias.Text = If(c.Referencias, "")
        txtFechaNacimiento.Text = If(c.FechaNacimiento.HasValue, c.FechaNacimiento.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "")
        txtIngresos.Text = c.Ingresos.ToString("0.00", CultureInfo.InvariantCulture)
        ActualizarTipoPersona()
        pnlFormularioCliente.Visible = True
        pnlListadoClientes.Visible = False
    End Sub
    Private Sub CargarUbicacion(lista As DropDownList, nivel As Integer, Optional seleccionado As String = "")
        lista.Items.Clear()
        lista.Items.Add(New ListItem("Seleccione...", ""))
        Dim repo As New ClienteRepositorio(ConfigurationManager.ConnectionStrings("SistemaFinanciera").ConnectionString)
        For Each opcion In repo.Ubicaciones(nivel, ddlProvincia.SelectedValue, ddlDistrito.SelectedValue)
            lista.Items.Add(New ListItem(opcion.Value, opcion.Key))
        Next
        If lista.Items.FindByValue(If(seleccionado, "")) IsNot Nothing Then lista.SelectedValue = If(seleccionado, "")
        lista.Enabled = nivel = 1 OrElse (ddlProvincia.SelectedValue <> "" AndAlso (nivel = 2 OrElse ddlDistrito.SelectedValue <> ""))
    End Sub
    Protected Sub ProvinciaCambiada(sender As Object, e As EventArgs) Handles ddlProvincia.SelectedIndexChanged
        EjecutarCliente(Sub()
                     CargarUbicacion(ddlDistrito, 2)
                     CargarUbicacion(ddlCorregimiento, 3)
                 End Sub)
    End Sub
    Protected Sub DistritoCambiado(sender As Object, e As EventArgs) Handles ddlDistrito.SelectedIndexChanged
        EjecutarCliente(Sub() CargarUbicacion(ddlCorregimiento, 3))
    End Sub
    Private Sub ActualizarTipoPersona()
        Dim natural = ddlTipoPersona.SelectedValue <> "JURIDICA"
        For Each opcion As ListItem In ddlTipoIdentificacion.Items
            opcion.Enabled = opcion.Value = "" OrElse If(natural, opcion.Value <> "RUC", opcion.Value = "RUC")
        Next
        txtApellidos.Enabled = natural
        txtFechaNacimiento.Enabled = natural
        ddlSexo.Enabled = natural
        ddlEstadoCivil.Enabled = natural
        ddlSexo.Items.FindByValue("NO_APLICA").Enabled = Not natural
        ddlEstadoCivil.Items.FindByValue("NO_APLICA").Enabled = Not natural
        If Not natural Then
            ddlTipoIdentificacion.SelectedValue = "RUC"
            txtApellidos.Text = ""
            txtFechaNacimiento.Text = ""
            ddlSexo.SelectedValue = "NO_APLICA"
            ddlEstadoCivil.SelectedValue = "NO_APLICA"
        Else
            If ddlTipoIdentificacion.SelectedValue = "RUC" Then ddlTipoIdentificacion.SelectedValue = "CEDULA"
            If ddlSexo.SelectedValue = "NO_APLICA" Then ddlSexo.SelectedValue = ""
            If ddlEstadoCivil.SelectedValue = "NO_APLICA" Then ddlEstadoCivil.SelectedValue = ""
        End If
    End Sub
    Protected Sub TipoPersonaCambiado(sender As Object, e As EventArgs) Handles ddlTipoPersona.SelectedIndexChanged
        ActualizarTipoPersona()
    End Sub
    Protected Sub NuevoCliente(sender As Object, e As EventArgs) Handles btnNuevoCliente.Click
        EjecutarCliente(Sub() MostrarFormulario(New Cliente With {.TipoPersona = "NATURAL", .TipoIdentificacion = "CEDULA"}))
    End Sub
    Protected Sub Cancelar(sender As Object, e As EventArgs) Handles btnCancelar.Click
        ClienteId = 0
        ViewState.Remove("ClienteVersion")
        pnlFormularioCliente.Visible = False
        pnlListadoClientes.Visible = True
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Protected Sub Guardar(sender As Object, e As EventArgs) Handles btnGuardarCliente.Click
        EjecutarCliente(Sub()
                     Dim ingresos As Decimal
                     ' NumberStyles sin separador de miles: 1,50 no puede convertirse accidentalmente en 150.
                     If Not Decimal.TryParse(txtIngresos.Text, NumberStyles.AllowDecimalPoint Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, ingresos) Then Throw New ArgumentException("Indique los ingresos con punto decimal, por ejemplo 1250.50.")
                     Dim nacimiento As Date
                     Dim fecha As Nullable(Of Date) = Nothing
                     If ddlTipoPersona.SelectedValue = "NATURAL" Then
                         If Not Date.TryParseExact(txtFechaNacimiento.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, nacimiento) Then Throw New ArgumentException("Indique una fecha de nacimiento válida.")
                         fecha = nacimiento
                     End If
                     Dim c As New Cliente With {.Id = ClienteId, .Version = DirectCast(ViewState("ClienteVersion"), Byte()), .FechaNacimiento = fecha, .Ingresos = ingresos}
                     c.TipoPersona = ddlTipoPersona.SelectedValue
                     c.TipoIdentificacion = ddlTipoIdentificacion.SelectedValue
                     c.Identificacion = txtIdentificacion.Text.Trim()
                     c.Nombres = txtNombres.Text.Trim()
                     c.Apellidos = txtApellidos.Text.Trim()
                     c.Sexo = ddlSexo.SelectedValue
                     c.EstadoCivil = ddlEstadoCivil.SelectedValue
                     c.Telefono = txtTelefono.Text.Trim()
                     c.Correo = txtCorreo.Text.Trim()
                     c.Direccion = txtDireccion.Text.Trim()
                     c.CodigoProvincia = ddlProvincia.SelectedValue
                     c.Provincia = If(ddlProvincia.SelectedValue = "", "", ddlProvincia.SelectedItem.Text)
                     c.CodigoDistrito = ddlDistrito.SelectedValue
                     c.Distrito = If(ddlDistrito.SelectedValue = "", "", ddlDistrito.SelectedItem.Text)
                     c.CodigoCorregimiento = ddlCorregimiento.SelectedValue
                     c.Corregimiento = If(ddlCorregimiento.SelectedValue = "", "", ddlCorregimiento.SelectedItem.Text)
                     c.Ocupacion = txtOcupacion.Text.Trim()
                     c.Referencias = txtReferencias.Text.Trim()
                     Dim esNuevo = c.Id = 0
                     Dim id = Servicio.Guardar(c)
                     Try
                         Dim evento = If(esNuevo, "CLIENTE_CREADO", "CLIENTE_ACTUALIZADO")
                         Dim configuracion = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
                         If configuracion IsNot Nothing Then
                             Dim repositorioFinanzas As New FinanzasRepositorio(configuracion.ConnectionString)
                             repositorioFinanzas.RegistrarEventoCliente(Actor.Id, evento, id)
                         End If
                     Catch ex As Exception
                         Diagnostics.Trace.TraceError("No se pudo notificar el guardado del cliente {0}: {1}", id, ex.ToString())
                     End Try
                     ClienteId = 0
                     ViewState.Remove("ClienteVersion")
                     pnlFormularioCliente.Visible = False
                     pnlListadoClientes.Visible = True
                     CargarListado()
                     Mensaje("Cliente guardado correctamente (número " & id.ToString() & ").", False)
                 End Sub)
    End Sub
    Protected Sub FiltroCambiado(sender As Object, e As EventArgs) Handles ddlEstado.SelectedIndexChanged, ddlProvinciaFiltro.SelectedIndexChanged, ddlPersonaFiltro.SelectedIndexChanged
        hidFiltroPrestamo.Value = "0"
        gvClientes.PageIndex = 0
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Protected Sub Limpiar(sender As Object, e As EventArgs) Handles btnLimpiar.Click
        txtBuscar.Text = ""
        ddlEstado.SelectedValue = ""
        ddlProvinciaFiltro.SelectedValue = ""
        ddlPersonaFiltro.SelectedValue = ""
        hidFiltroPrestamo.Value = "0"
        gvClientes.PageIndex = 0
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Public Function Iniciales(valor As Object) As String
        Dim palabras = Convert.ToString(valor).Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        Return String.Concat(palabras.Take(2).Select(Function(palabra) palabra.Substring(0, 1))).ToUpperInvariant()
    End Function
    Public Function AvatarCliente(id As Object) As String
        Dim identificador As Integer
        If id Is Nothing OrElse Not Integer.TryParse(Convert.ToString(id, CultureInfo.InvariantCulture), identificador) Then Return "tone-4"
        Select Case Math.Abs(identificador Mod 5)
            Case 1 : Return "tone-1"
            Case 2 : Return "tone-2"
            Case 3 : Return "tone-5"
            Case 4 : Return "tone-3"
            Case Else : Return "tone-4"
        End Select
    End Function
    Protected Sub ClienteRowDataBound(sender As Object, e As GridViewRowEventArgs)
        If e.Row.RowType <> DataControlRowType.DataRow Then Return
        Dim ver = TryCast(e.Row.FindControl("btnVer"), LinkButton)
        If ver Is Nothing Then Return
        e.Row.Attributes("data-view-button") = ver.ClientID
        e.Row.Attributes("tabindex") = "0"
        e.Row.Attributes("role") = "link"
        e.Row.Attributes("aria-label") = "Consultar perfil de " & Convert.ToString(DataBinder.Eval(e.Row.DataItem, "NombreCompleto"))
        e.Row.CssClass = (e.Row.CssClass & " client-row-clickable").Trim()
    End Sub
    Private Sub MostrarPerfil(c As Cliente)
        ViewState("PerfilId") = c.Id
        Dim datos = ServicioFinanzas.ObtenerPerfilCliente(Actor.Id, c.Id)
        litPerfilCabecera.Text = RenderPerfilCabecera(c)
        litPerfil.Text = RenderPerfil(c, datos)
        btnCambiarEstadoPerfil.Text = If(c.Activo, "Desactivar cliente", "Reactivar cliente")
        btnCambiarEstadoPerfil.Attributes("data-confirm-action") = If(c.Activo, "desactivar", "reactivar")
        btnCambiarEstadoPerfil.CssClass = If(c.Activo, "btn btn-light customer-action customer-danger-action", "btn btn-light customer-action")
        lnkNuevaSolicitud.NavigateUrl = ResolveUrl("~/Solicitudes/Solicitudes.aspx")
        pnlListadoClientes.Visible = False
        pnlFormularioCliente.Visible = False
        pnlPerfil.Visible = True
    End Sub
    Private Function RenderPerfilCabecera(c As Cliente) As String
        Dim ubicacion = String.Join(", ", New String() {c.Distrito, c.Provincia}.Where(Function(x) Not String.IsNullOrWhiteSpace(x)))
        Dim claseEstado = If(c.Activo, "customer-status", "customer-status is-inactive")
        Dim tipoId = If(c.TipoIdentificacion = "RUC", "RUC: ", "Cédula: ")
        Return "<div class='customer-detail-identity'><span class='person-avatar customer-detail-avatar " & AvatarCliente(c.Id) & "'>" &
            Server.HtmlEncode(Iniciales(c.NombreCompleto)) & "</span><div><div class='customer-detail-name'><h1>" & Server.HtmlEncode(c.NombreCompleto) & "</h1><span class='" & claseEstado & "'>" & Server.HtmlEncode(c.Estado) &
            "</span></div><p>" & Server.HtmlEncode(tipoId & c.Identificacion) & If(String.IsNullOrWhiteSpace(ubicacion), "", " · " & Server.HtmlEncode(ubicacion & ", República de Panamá")) & "</p></div></div>"
    End Function
    Private Function RenderPerfil(c As Cliente, datos As DataSet) As String
        Dim html As New StringBuilder()
        Dim indicadores = datos.Tables("Indicadores").Rows(0)
        Dim prestamos = datos.Tables("Prestamos")
        Dim pagos = datos.Tables("Pagos")
        Dim referencias = datos.Tables("Referencias")
        Dim enMora = CInt(indicadores("CuotasEnMora"))
        Dim cantidadReferencias = referencias.Rows.Count
        If cantidadReferencias = 0 AndAlso Not String.IsNullOrWhiteSpace(c.Referencias) Then cantidadReferencias = 1
        html.Append("<section class='customer-profile-kpis' aria-label='Resumen financiero del cliente'>")
        HtmlCard(html, "Préstamos activos", CInt(indicadores("PrestamosActivos")).ToString("N0", CulturaCliente), prestamos.Rows.Count.ToString("N0", CulturaCliente) & " en historial", "")
        HtmlCard(html, "Saldo pendiente total", Moneda(CDec(indicadores("SaldoPendiente"))), "Capital restante", "is-green")
        HtmlCard(html, "Cuotas en mora", enMora.ToString("N0", CulturaCliente), If(enMora = 0, "Al día con sus pagos", "Requieren seguimiento"), If(enMora = 0, "", "is-red"))
        HtmlCard(html, "Histórico pagado", Moneda(CDec(indicadores("HistoricoPagado"))), "Pagos aplicados", "")
        html.Append("</section><section class='customer-profile-panel'><nav class='customer-profile-tabs' role='tablist' aria-label='Información del cliente'>")
        TabPerfil(html, "general", "Información general", True)
        TabPerfil(html, "prestamos", "Historial de Préstamos (" & prestamos.Rows.Count.ToString(CultureInfo.InvariantCulture) & ")", False)
        TabPerfil(html, "estado", "Estado de Cuenta", False)
        TabPerfil(html, "referencias", "Referencias Personales (" & cantidadReferencias.ToString(CultureInfo.InvariantCulture) & ")", False)
        html.Append("</nav><section class='customer-tab-panel' data-profile-panel='general'><dl class='customer-detail-grid'>")
        Dim ubicacion = String.Join(", ", New String() {c.Corregimiento, c.Distrito, c.Provincia}.Where(Function(x) Not String.IsNullOrWhiteSpace(x)))
        DatoPerfil(html, "Tipo de persona", If(c.TipoPersona = "NATURAL", "Natural", "Jurídica"))
        DatoPerfil(html, If(c.TipoIdentificacion = "RUC", "RUC", "Cédula de identidad"), c.Identificacion)
        DatoPerfil(html, "Fecha de nacimiento", If(c.FechaNacimiento.HasValue, FechaLarga(c.FechaNacimiento.GetValueOrDefault()), "No registrada"))
        DatoPerfil(html, "Teléfono principal", c.Telefono)
        DatoPerfil(html, "Correo electrónico", c.Correo)
        DatoPerfil(html, "Ubicación residencial", If(String.IsNullOrWhiteSpace(ubicacion), "No registrada", ubicacion & ", República de Panamá"))
        DatoPerfil(html, "Situación laboral", c.Ocupacion)
        DatoPerfil(html, "Sexo", TextoLegible(c.Sexo))
        DatoPerfil(html, "Estado civil", TextoLegible(c.EstadoCivil))
        DatoPerfil(html, "Ingreso mensual comprobado", Moneda(c.Ingresos), "is-green")
        DatoPerfil(html, "Dirección", c.Direccion)
        html.Append("</dl></section><section class='customer-tab-panel' data-profile-panel='prestamos' hidden>")
        TablaPrestamos(html, prestamos)
        html.Append("</section><section class='customer-tab-panel' data-profile-panel='estado' hidden><h2 class='customer-subtitle'>Últimos pagos registrados en Balboas (B/.)</h2>")
        TablaPagos(html, pagos)
        html.Append("</section><section class='customer-tab-panel' data-profile-panel='referencias' hidden>")
        TablaReferencias(html, referencias, c.Referencias)
        html.Append("</section></section>")
        Return html.ToString()
    End Function
    Private ReadOnly Property CulturaCliente As CultureInfo
        Get
            Return CultureInfo.GetCultureInfo("es-PA")
        End Get
    End Property
    Private Sub HtmlCard(html As StringBuilder, titulo As String, valor As String, nota As String, clase As String)
        html.Append("<article class='customer-metric'><span>" & Server.HtmlEncode(titulo) & "</span><strong class='" & clase & "'>" & Server.HtmlEncode(valor) & "</strong><small>" & Server.HtmlEncode(nota) & "</small></article>")
    End Sub
    Private Sub TabPerfil(html As StringBuilder, id As String, texto As String, activo As Boolean)
        html.Append("<button type='button' role='tab' aria-selected='" & If(activo, "true", "false") & "' class='customer-profile-tab" & If(activo, " is-active", "") & "' data-profile-tab='" & id & "'>" & Server.HtmlEncode(texto) & "</button>")
    End Sub
    Private Sub DatoPerfil(html As StringBuilder, etiqueta As String, valor As String, Optional clase As String = "")
        html.Append("<div class='customer-detail-item'><dt>" & Server.HtmlEncode(etiqueta) & "</dt><dd class='" & clase & "'>" & Server.HtmlEncode(If(String.IsNullOrWhiteSpace(valor), "No registrado", valor)) & "</dd></div>")
    End Sub
    Private Sub TablaPrestamos(html As StringBuilder, datos As DataTable)
        html.Append("<div class='customer-table-scroll'><table class='customer-detail-table'><thead><tr><th>N° préstamo</th><th>Producto</th><th>Monto aprobado</th><th>Saldo actual</th><th>Método</th><th>Plazo</th><th>Estado</th></tr></thead><tbody>")
        If datos.Rows.Count = 0 Then
            html.Append("<tr><td colspan='7' class='customer-empty'>Este cliente aún no tiene préstamos registrados.</td></tr>")
        Else
            For Each fila As DataRow In datos.Rows
                Dim estado = EstadoLegible(CStr(fila("EstadoActual")))
                Dim meses = CDec(fila("PlazoMeses")).ToString("0.##", CulturaCliente)
                html.Append("<tr><td class='is-code'>" & Server.HtmlEncode(CStr(fila("NumeroPrestamo"))) & "</td><td>" & Server.HtmlEncode(CStr(fila("Producto"))) & "</td><td>" & Moneda(CDec(fila("MontoAprobado"))) & "</td><td class='is-green'>" & Moneda(CDec(fila("SaldoCapital"))) & "</td><td>" & Server.HtmlEncode(CStr(fila("Metodo"))) & "</td><td>" & CInt(fila("CantidadCuotas")).ToString(CultureInfo.InvariantCulture) & " cuotas · " & meses & " meses</td><td><span class='customer-status " & If(estado = "En mora", "is-inactive", "") & "'>" & Server.HtmlEncode(estado) & "</span></td></tr>")
            Next
        End If
        html.Append("</tbody></table></div>")
    End Sub
    Private Sub TablaPagos(html As StringBuilder, datos As DataTable)
        html.Append("<div class='customer-table-scroll'><table class='customer-detail-table'><thead><tr><th>Fecha</th><th>Transacción</th><th>Canal</th><th>Monto abonado</th><th>Estado</th></tr></thead><tbody>")
        If datos.Rows.Count = 0 Then
            html.Append("<tr><td colspan='5' class='customer-empty'>Todavía no hay pagos registrados para este cliente.</td></tr>")
        Else
            For Each fila As DataRow In datos.Rows
                Dim fecha = CDate(fila("FechaPago")).AddHours(-5).ToString("dd 'de' MMMM, yyyy", CulturaCliente)
                Dim estado = If(CStr(fila("Estado")) = "APLICADO", "Aplicado", "Anulado")
                html.Append("<tr><td>" & Server.HtmlEncode(fecha) & "</td><td>" & Server.HtmlEncode(CStr(fila("NumeroRecibo"))) & " · " & Server.HtmlEncode(TextoLegible(CStr(fila("TipoPago")))) & "</td><td>" & Server.HtmlEncode(CStr(fila("Canal"))) & "</td><td class='is-green'>" & Moneda(CDec(fila("MontoPagado"))) & "</td><td><span class='customer-status " & If(estado = "Aplicado", "", "is-inactive") & "'>" & estado & "</span></td></tr>")
            Next
        End If
        html.Append("</tbody></table></div>")
    End Sub
    Private Sub TablaReferencias(html As StringBuilder, datos As DataTable, referenciaLibre As String)
        html.Append("<div class='customer-table-scroll'><table class='customer-detail-table'><thead><tr><th>Nombre</th><th>Parentesco</th><th>Teléfono</th></tr></thead><tbody>")
        If datos.Rows.Count = 0 AndAlso String.IsNullOrWhiteSpace(referenciaLibre) Then
            html.Append("<tr><td colspan='3' class='customer-empty'>No hay referencias personales registradas.</td></tr>")
        ElseIf datos.Rows.Count > 0 Then
            For Each fila As DataRow In datos.Rows
                html.Append("<tr><td>" & Server.HtmlEncode(CStr(fila("Nombre"))) & "</td><td>" & Server.HtmlEncode(CStr(fila("Parentesco"))) & "</td><td>" & Server.HtmlEncode(CStr(fila("Telefono"))) & "</td></tr>")
            Next
        Else
            html.Append("<tr><td colspan='3' class='customer-reference-note'>" & Server.HtmlEncode(referenciaLibre) & "</td></tr>")
        End If
        html.Append("</tbody></table></div>")
    End Sub
    Private Function Moneda(valor As Decimal) As String
        Return "B/. " & valor.ToString("N2", CulturaCliente)
    End Function
    Private Function FechaLarga(fecha As Date) As String
        Dim hoy = Date.UtcNow.AddHours(-5).Date
        Dim edad = hoy.Year - fecha.Year
        If fecha.Date > hoy.AddYears(-edad) Then edad -= 1
        Return fecha.ToString("d 'de' MMMM 'de' yyyy", CulturaCliente) & " (" & edad.ToString(CultureInfo.InvariantCulture) & " años)"
    End Function
    Private Function TextoLegible(valor As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Return "No registrado"
        Return String.Join(" ", valor.ToLowerInvariant().Split("_"c).Select(Function(p) If(p.Length = 0, p, Char.ToUpperInvariant(p(0)) & p.Substring(1))))
    End Function
    Private Function EstadoLegible(valor As String) As String
        Dim texto = TextoLegible(valor)
        If texto = "Vigente" OrElse texto = "Desembolsado" Then Return "Activo"
        Return texto
    End Function
    Protected Sub VolverPerfil(sender As Object, e As EventArgs) Handles btnVolverPerfil.Click
        pnlPerfil.Visible = False
        pnlListadoClientes.Visible = True
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Protected Sub EditarPerfil(sender As Object, e As EventArgs) Handles btnEditarPerfil.Click
        EjecutarCliente(Sub() MostrarFormulario(Servicio.Obtener(CInt(ViewState("PerfilId")))))
    End Sub
    Protected Sub CambiarEstadoPerfil(sender As Object, e As EventArgs) Handles btnCambiarEstadoPerfil.Click
        EjecutarCliente(Sub()
                     Dim id = CInt(ViewState("PerfilId"))
                     Dim cliente = Servicio.Obtener(id)
                     Servicio.CambiarEstado(id, Not cliente.Activo, cliente.Version)
                     MostrarPerfil(Servicio.Obtener(id))
                     Mensaje("Estado del cliente actualizado.", False)
                 End Sub)
    End Sub
    Private Function ValorCsv(valor As String) As String
        Dim texto = If(valor, "")
        Dim inicio = texto.TrimStart()
        If inicio.Length > 0 AndAlso "=+-@".Contains(inicio.Substring(0, 1)) Then texto = "'" & texto
        Return ChrW(34) & texto.Replace(ChrW(34), ChrW(34) & ChrW(34)) & ChrW(34)
    End Function
    Protected Sub Exportar(sender As Object, e As EventArgs) Handles btnExportar.Click
        EjecutarCliente(Sub()
                     Dim estado As Nullable(Of Boolean) = Nothing
                     If ddlEstado.SelectedValue <> "" Then estado = ddlEstado.SelectedValue = "1"
                     Dim datos = Servicio.Listar(txtBuscar.Text, estado, ddlProvinciaFiltro.SelectedValue, ddlPersonaFiltro.SelectedValue)
                     Dim csv As New System.Text.StringBuilder("Identificación;Nombre completo;Teléfono;Provincia;Tipo de persona;Estado" & vbCrLf)
                     For Each c In datos
                         csv.AppendLine(String.Join(";", New String() {ValorCsv(c.Identificacion), ValorCsv(c.NombreCompleto), ValorCsv(c.Telefono), ValorCsv(c.Provincia), ValorCsv(c.TipoPersona), ValorCsv(c.Estado)}))
                     Next
                     Response.Clear()
                     Response.ContentType = "text/csv"
                     Response.ContentEncoding = System.Text.Encoding.UTF8
                     Response.AddHeader("Content-Disposition", "attachment; filename=clientes.csv")
                     Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble())
                     Response.Write(csv.ToString())
                     Response.Flush()
                     Response.SuppressContent = True
                     Context.ApplicationInstance.CompleteRequest()
                 End Sub)
    End Sub
    Protected Sub Buscar(sender As Object, e As EventArgs) Handles btnBuscar.Click
        gvClientes.PageIndex = 0
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Protected Sub Paginar(sender As Object, e As GridViewPageEventArgs) Handles gvClientes.PageIndexChanging
        gvClientes.PageIndex = e.NewPageIndex
        EjecutarCliente(Sub() CargarListado())
    End Sub
    Protected Sub AccionCliente(sender As Object, e As GridViewCommandEventArgs) Handles gvClientes.RowCommand
        If e.CommandName <> "EditarCliente" AndAlso e.CommandName <> "CambiarEstado" AndAlso e.CommandName <> "VerCliente" Then Return
        EjecutarCliente(Sub()
                     Dim indice As Integer
                     If Not Integer.TryParse(Convert.ToString(e.CommandArgument), indice) OrElse indice < 0 OrElse indice >= gvClientes.DataKeys.Count Then Throw New ArgumentException("Seleccione un cliente válido.")
                     Dim clave = gvClientes.DataKeys(indice)
                     Dim id = CInt(clave.Values("Id"))
                     If e.CommandName = "VerCliente" Then
                         MostrarPerfil(Servicio.Obtener(id))
                     ElseIf e.CommandName = "EditarCliente" Then
                         MostrarFormulario(Servicio.Obtener(id))
                     Else
                         Servicio.CambiarEstado(id, Not CBool(clave.Values("Activo")), DirectCast(clave.Values("Version"), Byte()))
                         CargarListado()
                         Mensaje("Estado del cliente actualizado.", False)
                     End If
                 End Sub)
    End Sub
End Class
