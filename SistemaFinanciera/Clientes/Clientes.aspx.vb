Option Strict On
Imports System.Globalization
Imports SistemaFinanciera.Entities
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL

Public Class Clientes
    Inherits System.Web.UI.Page
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
    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        pnlMensaje.Visible = False
        pnlErrorFormulario.Visible = False
        If Not IsPostBack Then
            litKpiActivos.Text = "—"
            litKpiInactivos.Text = "—"
            Ejecutar(Sub()
                         Dim todos = Servicio.Listar("", Nothing)
                         ddlProvinciaFiltro.Items.Add(New ListItem("Provincia: Todas", ""))
                         For Each provincia In todos.Select(Function(c) c.Provincia).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(v) v)
                             ddlProvinciaFiltro.Items.Add(New ListItem(provincia, provincia))
                         Next
                         CargarListado()
                     End Sub)
        End If
    End Sub
    Private Sub Ejecutar(operacion As Action)
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
        Dim estado As Nullable(Of Boolean) = Nothing
        If ddlEstado.SelectedValue <> "" Then estado = ddlEstado.SelectedValue = "1"
        Dim todos = Servicio.Listar("", Nothing)
        litKpiActivos.Text = System.Linq.Enumerable.Count(todos, Function(c) c.Activo).ToString()
        litKpiInactivos.Text = System.Linq.Enumerable.Count(todos, Function(c) Not c.Activo).ToString()
        Dim seleccion = ddlProvinciaFiltro.SelectedValue
        ddlProvinciaFiltro.Items.Clear()
        ddlProvinciaFiltro.Items.Add(New ListItem("Provincia: Todas", ""))
        For Each provincia In todos.Select(Function(c) c.Provincia).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Function(v) v)
            ddlProvinciaFiltro.Items.Add(New ListItem(provincia, provincia))
        Next
        If ddlProvinciaFiltro.Items.FindByValue(seleccion) IsNot Nothing Then ddlProvinciaFiltro.SelectedValue = seleccion
        Dim clientes = Servicio.Listar(txtBuscar.Text, estado, ddlProvinciaFiltro.SelectedValue, ddlPersonaFiltro.SelectedValue)
        If gvClientes.PageIndex * gvClientes.PageSize >= clientes.Count Then gvClientes.PageIndex = 0
        gvClientes.DataSource = clientes
        gvClientes.DataBind()
        Dim primero = If(clientes.Count = 0, 0, gvClientes.PageIndex * gvClientes.PageSize + 1)
        Dim ultimo = Math.Min(clientes.Count, (gvClientes.PageIndex + 1) * gvClientes.PageSize)
        litConteo.Text = "Mostrando " & primero.ToString() & "–" & ultimo.ToString() & " de " & clientes.Count.ToString() & " clientes"
    End Sub
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
        ddlEstadoCivil.SelectedValue = If(c.EstadoCivil, "")
        txtTelefono.Text = If(c.Telefono, "")
        txtCorreo.Text = If(c.Correo, "")
        txtDireccion.Text = If(c.Direccion, "")
        txtProvincia.Text = If(c.Provincia, "")
        txtDistrito.Text = If(c.Distrito, "")
        txtCorregimiento.Text = If(c.Corregimiento, "")
        txtOcupacion.Text = If(c.Ocupacion, "")
        txtReferencias.Text = If(c.Referencias, "")
        txtFechaNacimiento.Text = If(c.FechaNacimiento.HasValue, c.FechaNacimiento.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "")
        txtIngresos.Text = c.Ingresos.ToString("0.00", CultureInfo.InvariantCulture)
        ActualizarTipoPersona()
        pnlFormularioCliente.Visible = True
        pnlListadoClientes.Visible = False
    End Sub
    Private Sub ActualizarTipoPersona()
        Dim natural = ddlTipoPersona.SelectedValue <> "JURIDICA"
        txtApellidos.Enabled = natural
        txtFechaNacimiento.Enabled = natural
        ddlSexo.Enabled = natural
        ddlEstadoCivil.Enabled = natural
        If Not natural Then
            ddlTipoIdentificacion.SelectedValue = "RUC"
            txtApellidos.Text = ""
            txtFechaNacimiento.Text = ""
            ddlSexo.SelectedValue = "NO_APLICA"
            ddlEstadoCivil.SelectedValue = "NO_APLICA"
        Else
            If ddlSexo.SelectedValue = "NO_APLICA" Then ddlSexo.SelectedValue = ""
            If ddlEstadoCivil.SelectedValue = "NO_APLICA" Then ddlEstadoCivil.SelectedValue = ""
        End If
    End Sub
    Protected Sub TipoPersonaCambiado(sender As Object, e As EventArgs) Handles ddlTipoPersona.SelectedIndexChanged
        ActualizarTipoPersona()
    End Sub
    Protected Sub NuevoCliente(sender As Object, e As EventArgs) Handles btnNuevoCliente.Click
        MostrarFormulario(New Cliente With {.TipoPersona = "NATURAL", .TipoIdentificacion = "CEDULA"})
    End Sub
    Protected Sub Cancelar(sender As Object, e As EventArgs) Handles btnCancelar.Click
        ClienteId = 0
        ViewState.Remove("ClienteVersion")
        pnlFormularioCliente.Visible = False
        pnlListadoClientes.Visible = True
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub Guardar(sender As Object, e As EventArgs) Handles btnGuardarCliente.Click
        Ejecutar(Sub()
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
                     c.Provincia = txtProvincia.Text.Trim()
                     c.Distrito = txtDistrito.Text.Trim()
                     c.Corregimiento = txtCorregimiento.Text.Trim()
                     c.Ocupacion = txtOcupacion.Text.Trim()
                     c.Referencias = txtReferencias.Text.Trim()
                     Dim id = Servicio.Guardar(c)
                     ClienteId = 0
                     ViewState.Remove("ClienteVersion")
                     pnlFormularioCliente.Visible = False
                     pnlListadoClientes.Visible = True
                     CargarListado()
                     Mensaje("Cliente guardado correctamente (número " & id.ToString() & ").", False)
                 End Sub)
    End Sub
    Protected Sub FiltroCambiado(sender As Object, e As EventArgs) Handles ddlEstado.SelectedIndexChanged, ddlProvinciaFiltro.SelectedIndexChanged, ddlPersonaFiltro.SelectedIndexChanged
        gvClientes.PageIndex = 0
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub Limpiar(sender As Object, e As EventArgs) Handles btnLimpiar.Click
        txtBuscar.Text = ""
        ddlEstado.SelectedValue = ""
        ddlProvinciaFiltro.SelectedValue = ""
        ddlPersonaFiltro.SelectedValue = ""
        gvClientes.PageIndex = 0
        Ejecutar(Sub() CargarListado())
    End Sub
    Public Function Iniciales(valor As Object) As String
        Dim palabras = Convert.ToString(valor).Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        Return String.Concat(palabras.Take(2).Select(Function(palabra) palabra.Substring(0, 1))).ToUpperInvariant()
    End Function
    Private Sub MostrarPerfil(c As Cliente)
        ViewState("PerfilId") = c.Id
        Dim html As New System.Text.StringBuilder()
        html.Append("<div class='profile-layout'><section class='profile-summary'><div class='profile-avatar'>" & Server.HtmlEncode(Iniciales(c.NombreCompleto)) & "</div><h2>" & Server.HtmlEncode(c.NombreCompleto) & "</h2><p>" & Server.HtmlEncode(c.Identificacion) & "</p><span class='status-pill'>" & Server.HtmlEncode(c.Estado) & "</span></section><section class='profile-details'><h3>Información del cliente</h3><dl>")
        Dim datos As String()() = {New String() {"Tipo de persona", If(c.TipoPersona = "NATURAL", "Natural", "Jurídica")}, New String() {"Identificación", c.Identificacion}, New String() {"Teléfono", c.Telefono}, New String() {"Correo", c.Correo}, New String() {"Fecha de nacimiento", If(c.FechaNacimiento.HasValue, c.FechaNacimiento.GetValueOrDefault().ToString("dd/MM/yyyy"), "No aplica")}, New String() {"Sexo", c.Sexo}, New String() {"Estado civil", c.EstadoCivil}, New String() {"Provincia", c.Provincia}, New String() {"Distrito", c.Distrito}, New String() {"Corregimiento", c.Corregimiento}, New String() {"Dirección", c.Direccion}, New String() {"Información laboral", c.Ocupacion}, New String() {"Ingresos mensuales", "B/. " & c.Ingresos.ToString("N2", CultureInfo.GetCultureInfo("es-PA"))}, New String() {"Referencias", c.Referencias}}
        For Each dato In datos
            html.Append("<div><dt>" & Server.HtmlEncode(dato(0)) & "</dt><dd>" & Server.HtmlEncode(dato(1)) & "</dd></div>")
        Next
        html.Append("</dl></section></div>")
        litPerfil.Text = html.ToString()
        pnlListadoClientes.Visible = False
        pnlFormularioCliente.Visible = False
        pnlPerfil.Visible = True
    End Sub
    Protected Sub VolverPerfil(sender As Object, e As EventArgs) Handles btnVolverPerfil.Click
        pnlPerfil.Visible = False
        pnlListadoClientes.Visible = True
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub EditarPerfil(sender As Object, e As EventArgs) Handles btnEditarPerfil.Click
        Ejecutar(Sub() MostrarFormulario(Servicio.Obtener(CInt(ViewState("PerfilId")))))
    End Sub
    Private Function ValorCsv(valor As String) As String
        Dim texto = If(valor, "")
        Dim inicio = texto.TrimStart()
        If inicio.Length > 0 AndAlso "=+-@".Contains(inicio.Substring(0, 1)) Then texto = "'" & texto
        Return ChrW(34) & texto.Replace(ChrW(34), ChrW(34) & ChrW(34)) & ChrW(34)
    End Function
    Protected Sub Exportar(sender As Object, e As EventArgs) Handles btnExportar.Click
        Ejecutar(Sub()
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
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub Paginar(sender As Object, e As GridViewPageEventArgs) Handles gvClientes.PageIndexChanging
        gvClientes.PageIndex = e.NewPageIndex
        Ejecutar(Sub() CargarListado())
    End Sub
    Protected Sub AccionCliente(sender As Object, e As GridViewCommandEventArgs) Handles gvClientes.RowCommand
        If e.CommandName <> "EditarCliente" AndAlso e.CommandName <> "CambiarEstado" AndAlso e.CommandName <> "VerCliente" Then Return
        Ejecutar(Sub()
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
