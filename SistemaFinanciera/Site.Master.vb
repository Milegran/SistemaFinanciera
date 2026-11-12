Imports System.Globalization
Imports System.Linq
Imports System.Data
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Imports SistemaFinanciera.Entities

Public Class SiteMaster
    Inherits MasterPage
    Private usuarioActual As UsuarioFinanzas
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        Dim usuario = TryCast(Context.Items("CapitalWest.UsuarioFinanzas"), UsuarioFinanzas)
        If usuario Is Nothing AndAlso Request.IsAuthenticated Then
            Dim id As Integer
            If Integer.TryParse(Context.User.Identity.Name, id) Then
                Try
                    Dim conexion = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
                    If conexion IsNot Nothing Then usuario = New FinanzasServicio(New FinanzasRepositorio(conexion.ConnectionString)).Usuario(id)
                Catch ex As Exception
                    Diagnostics.Trace.TraceError("No se pudo cargar el perfil del usuario autenticado: {0}", ex.ToString())
                End Try
            End If
        End If

        usuarioActual = usuario
        Dim puedeAdministrar=usuario IsNot Nothing AndAlso usuario.Puede("ADMINISTRADOR")
        pnlAdminSidebar.Visible=puedeAdministrar
        lnkAdministracion.Visible=puedeAdministrar
        lnkAjustes.Visible=puedeAdministrar
        If usuario Is Nothing Then
            litUsuarioNombre.Text = "Usuario"
            litUsuarioRol.Text = "Sin sesión"
            litUsuarioIniciales.Text = "CW"
            pnlNotificaciones.Visible = False
            Return
        End If

        litUsuarioNombre.Text = If(String.IsNullOrWhiteSpace(usuario.Nombre), "Usuario", usuario.Nombre.Trim())
        litUsuarioRol.Text = If(usuario.Roles Is Nothing OrElse usuario.Roles.Length = 0,
                                "Sin rol",
                                String.Join(" · ", usuario.Roles.Select(Function(rol) RolVisible(rol))))
        litUsuarioIniciales.Text = Iniciales(litUsuarioNombre.Text)
    End Sub
    Protected Sub CargarNotificaciones(sender As Object, e As EventArgs) Handles Me.PreRender
        If usuarioActual Is Nothing Then Return
        pnlToastNotificacion.Visible = False
        Try
            Dim conexion = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If conexion Is Nothing Then Return
            Dim servicio As New FinanzasServicio(New FinanzasRepositorio(conexion.ConnectionString))
            Dim filas = servicio.ConsultarNotificaciones(usuarioActual.Id)
            rptNotificaciones.DataSource = filas
            rptNotificaciones.DataBind()
            pnlNotificacionesVacio.Visible = filas.Rows.Count = 0
            btnEliminarTodasNotificaciones.Visible = filas.Rows.Count > 0
            Dim noLeidas = If(filas.Rows.Count = 0, 0, Convert.ToInt32(filas.Rows(0)("NoLeidas")))
            litNotificacionesConteo.Text = If(noLeidas = 0, "Todo al día", noLeidas.ToString(CultureInfo.InvariantCulture) & " sin leer")
            litNotificacionesNoLeidas.Text = If(noLeidas > 99, "99+", If(noLeidas > 0, noLeidas.ToString(CultureInfo.InvariantCulture), ""))

            Dim ultimaVista As Long = 0
            Long.TryParse(Convert.ToString(Session("CW_UltimaNotificacionToast"), CultureInfo.InvariantCulture), ultimaVista)
            Dim nueva = filas.AsEnumerable().FirstOrDefault(Function(fila) Not Convert.ToBoolean(fila("Leida")) AndAlso CLng(fila("Id")) > ultimaVista)
            If nueva IsNot Nothing Then
                litToastTitulo.Text = Convert.ToString(nueva("Titulo"))
                litToastMensaje.Text = Convert.ToString(nueva("Mensaje"))
                pnlToastNotificacion.CssClass = "cw-notification-toast " & ClaseNotificacion(Convert.ToString(nueva("Modulo")))
                pnlToastNotificacion.Visible = True
                Session("CW_UltimaNotificacionToast") = CLng(nueva("Id")).ToString(CultureInfo.InvariantCulture)
            End If
        Catch ex As Exception
            Diagnostics.Trace.TraceError("No se pudieron cargar las notificaciones del usuario {0}: {1}", usuarioActual.Id, ex.ToString())
            pnlNotificacionesVacio.Visible = True
            pnlNotificacionesVacio.Controls.Clear()
            pnlNotificacionesVacio.Controls.Add(New LiteralControl("No se pudieron cargar. Verifica la conexión y la tabla de notificaciones de la base de datos."))
        End Try
    End Sub
    Protected Sub NotificacionesItemCommand(sender As Object, e As RepeaterCommandEventArgs) Handles rptNotificaciones.ItemCommand
        If usuarioActual Is Nothing Then Return
        Dim id As Long
        If Not Long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), id) OrElse id <= 0 Then Return
        Try
            Dim conexion = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If conexion Is Nothing Then Return
            Dim servicio As New FinanzasServicio(New FinanzasRepositorio(conexion.ConnectionString))
            If String.Equals(e.CommandName, "borrar", StringComparison.OrdinalIgnoreCase) Then
                servicio.EliminarNotificacion(usuarioActual.Id, id)
            ElseIf String.Equals(e.CommandName, "leer", StringComparison.OrdinalIgnoreCase) Then
                servicio.MarcarNotificacionLeida(usuarioActual.Id, id)
            End If
        Catch ex As Exception
            Diagnostics.Trace.TraceError("No se pudo actualizar una notificación del usuario {0}: {1}", usuarioActual.Id, ex.ToString())
        End Try
    End Sub
    Protected Sub EliminarTodasNotificacionesClick(sender As Object, e As EventArgs) Handles btnEliminarTodasNotificaciones.Click
        If usuarioActual Is Nothing Then Return
        Try
            Dim conexion = ConfigurationManager.ConnectionStrings("SistemaFinanciera")
            If conexion Is Nothing Then Return
            Dim servicio As New FinanzasServicio(New FinanzasRepositorio(conexion.ConnectionString))
            servicio.EliminarNotificaciones(usuarioActual.Id)
        Catch ex As Exception
            Diagnostics.Trace.TraceError("No se pudieron borrar todas las notificaciones del usuario {0}: {1}", usuarioActual.Id, ex.ToString())
        End Try
    End Sub
    Private Shared Function ClaseNotificacion(modulo As String) As String
        Select Case If(modulo, "").Trim().ToUpperInvariant()
            Case "SEGURIDAD" : Return "is-security"
            Case "PAGOS", "PRÉSTAMOS" : Return "is-finance"
            Case "SOLICITUDES" : Return "is-request"
            Case Else : Return "is-general"
        End Select
    End Function
    Private Shared Function RolVisible(codigo As String) As String
        Select Case If(codigo, "").Trim().ToUpperInvariant()
            Case "ADMINISTRADOR" : Return "Administrador"
            Case "OFICIAL_CREDITO" : Return "Oficial de crédito"
            Case "CAJERO" : Return "Cajero"
            Case "CONSULTA" : Return "Consulta"
            Case Else : Return CultureInfo.GetCultureInfo("es-PA").TextInfo.ToTitleCase(If(codigo, "Usuario").Replace("_", " ").ToLowerInvariant())
        End Select
    End Function
    Private Shared Function Iniciales(nombre As String) As String
        Dim partes = If(nombre, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If partes.Length = 0 Then Return "CW"
        If partes.Length = 1 Then Return partes(0).Substring(0, Math.Min(2, partes(0).Length)).ToUpperInvariant()
        Return (partes(0).Substring(0, 1) & partes(1).Substring(0, 1)).ToUpperInvariant()
    End Function
    Protected Sub CerrarSesionClick(sender As Object, e As EventArgs)
        System.Web.Security.FormsAuthentication.SignOut()
        Session.Clear()
        Response.Redirect("~/Acceso.aspx", True)
    End Sub
End Class
