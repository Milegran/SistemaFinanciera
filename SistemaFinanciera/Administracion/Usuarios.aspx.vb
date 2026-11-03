Option Strict On
Imports System.Data

Public Class Usuarios
    Inherits FinanzasPagina

    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        ' El navegador no debe bloquear el botón Cancelar por los required del formulario.
        btnCancelar.Attributes("formnovalidate") = "formnovalidate"
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False
            Notificar("No tienes permiso para administrar usuarios.",True)
            Return
        End If
        If Not IsPostBack Then
            Ejecutar(Sub()
                Dim roles=ServicioFinanzas.RolesAdministracion(Actor.Id)
                ddlRolFiltro.Items.Clear() : ddlRolFiltro.Items.Add(New ListItem("Rol: Todos","0"))
                ddlRolFiltro.AppendDataBoundItems=True
                ddlRol.Items.Clear() : ddlRol.Items.Add(New ListItem("Seleccione un rol",""))
                For Each r As DataRow In roles.Rows
                    ddlRolFiltro.Items.Add(New ListItem(CStr(r("Nombre")),CStr(r("Id"))))
                    ddlRol.Items.Add(New ListItem(CStr(r("Nombre")),CStr(r("Id"))))
                Next
                CargarUsuarios()
                If String.Equals(Request.QueryString("form"),"nuevo",StringComparison.OrdinalIgnoreCase) Then Nuevo(Nothing,EventArgs.Empty)
            End Sub)
        ElseIf Not modalUsuario.Visible Then
            Ejecutar(Sub() CargarUsuarios())
        End If
    End Sub

    Private Sub CargarUsuarios()
        Dim resumen=ServicioFinanzas.ResumenUsuariosAdministracion(Actor.Id)
        If resumen.Rows.Count=1 Then
            kpiTotal.Text=CInt(resumen.Rows(0)("Total")).ToString("N0")
            kpiActivos.Text=CInt(resumen.Rows(0)("Activos")).ToString("N0")
            kpiInactivos.Text=CInt(resumen.Rows(0)("Inactivos")).ToString("N0")
            kpiRoles.Text=CInt(resumen.Rows(0)("Roles")).ToString("N0")
        End If
        Dim rol As Integer=0
        Integer.TryParse(ddlRolFiltro.SelectedValue,rol)
        Dim datos=ServicioFinanzas.ConsultarUsuariosAdministracion(Actor.Id,txtBuscar.Text,rol,ddlEstadoFiltro.SelectedValue)
        Vincular(grUsuarios,datos)
    End Sub

    Protected Sub Buscar(sender As Object,e As EventArgs) Handles btnBuscar.Click,ddlRolFiltro.SelectedIndexChanged,ddlEstadoFiltro.SelectedIndexChanged
        grUsuarios.PageIndex=0
        Ejecutar(Sub() CargarUsuarios())
    End Sub

    Protected Sub LimpiarFiltros(sender As Object,e As EventArgs) Handles btnLimpiar.Click
        txtBuscar.Text = String.Empty
        If ddlRolFiltro.Items.Count > 0 Then ddlRolFiltro.SelectedIndex = 0
        ddlEstadoFiltro.SelectedValue = "TODOS"
        grUsuarios.PageIndex = 0
        Ejecutar(Sub() CargarUsuarios())
    End Sub

    Protected Sub PaginarUsuarios(sender As Object,e As GridViewPageEventArgs)
        grUsuarios.PageIndex=e.NewPageIndex
        Ejecutar(Sub() CargarUsuarios())
    End Sub

    Protected Sub UsuariosRowDataBound(sender As Object,e As Global.System.Web.UI.WebControls.GridViewRowEventArgs)
        If e.Row.RowType <> Global.System.Web.UI.WebControls.DataControlRowType.DataRow Then Return
        Dim accion = TryCast(e.Row.FindControl("lnkEstado"), Global.System.Web.UI.WebControls.LinkButton)
        If accion Is Nothing Then Return
        Dim estaActivo = CBool(grUsuarios.DataKeys(e.Row.RowIndex).Values("Activo"))
        accion.OnClientClick = If(estaActivo,
            "return confirmarAccionUsuario(this, 'Desactivar usuario', 'La cuenta dejará de tener acceso. El historial se conservará.', 'Desactivar', 'danger');",
            "return confirmarAccionUsuario(this, 'Reactivar usuario', 'Se volverá a habilitar el acceso de esta cuenta.', 'Reactivar', '');")
    End Sub

    Protected Sub Nuevo(sender As Object,e As EventArgs) Handles btnNuevo.Click
        LimpiarFormulario()
        litTituloModal.Text="Crear usuario"
        litDialogLead.Text="Completa los datos de acceso del usuario."
        modalUsuario.Visible=True
    End Sub

    Protected Sub Cancelar(sender As Object,e As EventArgs) Handles btnCancelar.Click
        modalUsuario.Visible=False
        LimpiarModoModal()
    End Sub

    Protected Sub Guardar(sender As Object,e As EventArgs) Handles btnGuardar.Click
        Ejecutar(Sub()
            Dim id As Integer=0,rolId As Integer=0
            Integer.TryParse(hfUsuarioId.Value,id)
            If String.Equals(hfModoRestablecer.Value,"MANUAL",StringComparison.Ordinal) Then
                If Not String.Equals(txtClaveManual.Text,txtClaveManualConfirmacion.Text,StringComparison.Ordinal) Then Throw New ArgumentException("Las contraseñas no coinciden.")
                ServicioFinanzas.RestablecerClaveManualUsuarioAdministracion(Actor.Id,id,txtClaveManual.Text)
                modalUsuario.Visible=False
                LimpiarModoModal()
                Notificar("La contraseña fue actualizada. El usuario deberá cambiarla al ingresar.")
                CargarUsuarios()
                Return
            End If
            If Not Integer.TryParse(ddlRol.SelectedValue,rolId) Then Throw New ArgumentException("Seleccione un rol.")
            ServicioFinanzas.GuardarUsuarioAdministracion(Actor.Id,id,txtNombreUsuario.Text.Trim(),txtNombreCompleto.Text.Trim(),txtCorreo.Text.Trim(),rolId,chkActivo.Checked,txtClave.Text)
            modalUsuario.Visible=False
            LimpiarModoModal()
            Notificar(If(id=0,"Usuario creado correctamente. Se solicitará cambiar la clave en su primer ingreso.","Usuario actualizado correctamente."))
            CargarUsuarios()
        End Sub)
    End Sub

    Protected Sub UsuarioCommand(sender As Object,e As GridViewCommandEventArgs)
        Dim indice As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument),indice) OrElse indice<0 OrElse indice>=grUsuarios.DataKeys.Count Then Return
        Dim clave=grUsuarios.DataKeys(indice)
        Dim id=CInt(clave("Id"))
        If e.CommandName="EditarUsuario" Then
            Ejecutar(Sub()
                hfUsuarioId.Value=id.ToString(Globalization.CultureInfo.InvariantCulture)
                hfModoRestablecer.Value=""
                pnlDatosUsuario.Visible=True : pnlClaveManual.Visible=False : pnlClaveGenerada.Visible=False : pnlModalAcciones.Visible=True
                txtNombreUsuario.Text=CStr(clave("NombreUsuario"))
                txtNombreUsuario.ReadOnly=True
                txtNombreCompleto.Text=CStr(clave("NombreCompleto"))
                txtCorreo.Text=CStr(clave("Correo"))
                chkActivo.Checked=CBool(clave("Activo"))
                Dim roles=CStr(clave("RolIds")).Split(","c)
                If roles.Length>0 AndAlso ddlRol.Items.FindByValue(roles(0)) IsNot Nothing Then ddlRol.SelectedValue=roles(0)
                txtClave.Text=""
                txtClave.Attributes("placeholder")="Dejar vacío para conservar la clave"
                litTituloModal.Text="Editar usuario"
                litDialogLead.Text="Actualiza el perfil, el rol o las credenciales del usuario."
                btnGuardar.Text="Guardar cambios"
                btnGuardar.Visible=True : btnCancelar.Text="Cancelar"
                modalUsuario.Visible=True
            End Sub)
        ElseIf e.CommandName="Estado" Then
            Ejecutar(Sub()
                Dim activo=Not CBool(clave("Activo"))
                ServicioFinanzas.CambiarEstadoUsuarioAdministracion(Actor.Id,id,activo)
                CargarUsuarios()
                Notificar(If(activo,"Usuario reactivado.","Usuario desactivado; se conserva su historial."))
            End Sub)
        ElseIf e.CommandName="Restablecer" Then
            Ejecutar(Sub()
                If String.Equals(hfModoRestablecer.Value,"MANUAL",StringComparison.Ordinal) Then
                    hfUsuarioId.Value=id.ToString(Globalization.CultureInfo.InvariantCulture)
                    litTituloModal.Text="Definir contraseña manual"
                    litDialogLead.Text="Define una nueva clave para esta cuenta."
                    litUsuarioClaveManual.Text=CStr(clave("NombreCompleto"))
                    txtClaveManual.Text="" : txtClaveManualConfirmacion.Text=""
                    pnlDatosUsuario.Visible=False : pnlClaveManual.Visible=True : pnlClaveGenerada.Visible=False : pnlModalAcciones.Visible=True
                    btnGuardar.Text="Guardar contraseña" : btnGuardar.Visible=True : btnCancelar.Text="Cancelar"
                    modalUsuario.Visible=True
                    Return
                End If
                Dim temporal=ServicioFinanzas.RestablecerClaveUsuarioAdministracion(Actor.Id,id)
                hfUsuarioId.Value=id.ToString(Globalization.CultureInfo.InvariantCulture)
                hfModoRestablecer.Value=""
                litTituloModal.Text="Contraseña temporal generada"
                litDialogLead.Text="La contraseña se restableció correctamente."
                litClaveGenerada.Text=temporal
                pnlDatosUsuario.Visible=False : pnlClaveManual.Visible=False : pnlClaveGenerada.Visible=True : pnlModalAcciones.Visible=True
                txtNombreCompleto.Text=CStr(clave("NombreCompleto"))
                txtNombreUsuario.Text=CStr(clave("NombreUsuario"))
                txtNombreUsuario.ReadOnly=True
                txtCorreo.Text=CStr(clave("Correo"))
                chkActivo.Checked=CBool(clave("Activo"))
                modalUsuario.Visible=True
                btnGuardar.Visible=False : btnCancelar.Text="Cerrar"
            End Sub)
        End If
    End Sub

    Private Sub LimpiarFormulario()
        LimpiarModoModal()
        hfUsuarioId.Value=""
        txtNombreUsuario.ReadOnly=False
        txtNombreUsuario.Text="" : txtNombreCompleto.Text="" : txtCorreo.Text="" : txtClave.Text=""
        txtClave.Attributes.Remove("placeholder")
        chkActivo.Checked=True
        If ddlRol.Items.Count>0 Then ddlRol.SelectedIndex=0
        btnGuardar.Text="Guardar usuario" : btnGuardar.Visible=True : btnCancelar.Text="Cancelar"
    End Sub

    Private Sub LimpiarModoModal()
        hfModoRestablecer.Value=""
        litClaveGenerada.Text=""
        txtClaveManual.Text="" : txtClaveManualConfirmacion.Text=""
        pnlDatosUsuario.Visible=True : pnlClaveManual.Visible=False : pnlClaveGenerada.Visible=False : pnlModalAcciones.Visible=True
        btnGuardar.Text="Guardar usuario" : btnGuardar.Visible=True : btnCancelar.Text="Cancelar"
    End Sub
End Class
