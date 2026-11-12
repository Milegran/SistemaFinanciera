Option Strict On
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Imports System.Web.Security
Public Class Acceso
    Inherits Page
    Private ReadOnly Property Servicio As FinanzasServicio
        Get
            Return New FinanzasServicio(New FinanzasRepositorio(ConfigurationManager.ConnectionStrings("SistemaFinanciera").ConnectionString))
        End Get
    End Property
    Private ReadOnly Property ServicioInicioRapido As FinanzasServicio
        Get
            Dim cadena As New System.Data.Common.DbConnectionStringBuilder() With {
                .ConnectionString = ConfigurationManager.ConnectionStrings("SistemaFinanciera").ConnectionString
            }
            cadena("Connection Timeout") = 2
            Return New FinanzasServicio(New FinanzasRepositorio(cadena.ConnectionString))
        End Get
    End Property
    Protected Overrides Sub OnInit(e As EventArgs)
        MyBase.OnInit(e)
        Response.Headers("X-Content-Type-Options") = "nosniff"
        Response.Headers("X-Frame-Options") = "DENY"
        Response.Headers("Referrer-Policy") = "no-referrer"
        Response.Headers("Content-Security-Policy") = "default-src 'self'; img-src 'self'; style-src 'self'; script-src 'self'; font-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'"
        Response.Cache.SetCacheability(HttpCacheability.NoCache)
        Response.Cache.SetNoStore()
        If Session("TokenFormulario") Is Nothing Then Session("TokenFormulario") = Guid.NewGuid().ToString("N")
        ViewStateUserKey = CStr(Session("TokenFormulario"))
    End Sub
    Protected Sub Cargar(sender As Object, e As EventArgs) Handles Me.Load
        Try
            If Not IsPostBack Then
                If Request.IsAuthenticated Then
                    Response.Redirect("~/Default.aspx", False)
                    Context.ApplicationInstance.CompleteRequest()
                    Return
                End If
                usuario.Attributes("autofocus") = "autofocus"
                If Request.QueryString("expirada") = "1" Then mensaje.Text = "Su sesión expiró. Inicie sesión nuevamente."
            End If
            alta.Visible = Request.IsLocal AndAlso Not ServicioInicioRapido.HayUsuarios()
            confirmacion.Visible = alta.Visible
            entrar.Text = If(alta.Visible, "Crear administrador e ingresar", "Ingresar")
            nombreCompleto.Attributes("required") = "required"
            nombreCompleto.Attributes("data-patron-nombre") = ClienteServicio.PatronNombre
            nombreCompleto.Attributes("data-regla") = "nombre-persona"
            correo.Attributes("required") = "required"
            claveConfirmada.Attributes("required") = "required"
            usuario.MaxLength = If(alta.Visible, 20, 254)
            usuario.Attributes("minlength") = If(alta.Visible, "3", "1")
            If alta.Visible Then
                clave.MaxLength = 20
                usuario.MaxLength = 20
                usuario.Attributes("data-regla") = "usuario-nuevo"
                usuario.Attributes("data-patron") = FinanzasServicio.PatronUsuarioNuevo
                usuario.Attributes("data-ayuda") = "Use 3–20 caracteres; empiece con una letra. Se admiten letras, números, puntos, guiones y guion bajo."
                clave.Attributes("minlength") = "8"
                clave.Attributes("autocomplete") = "new-password"
                claveConfirmada.Attributes("minlength") = "8"
                claveConfirmada.Attributes("data-igual") = clave.ClientID
            End If
        Catch ex As Exception
            Diagnostics.Trace.TraceError(ex.ToString())
            mensaje.Text = "No se pudo conectar con la base de datos. Compruebe MySQL en XAMPP."
            entrar.Enabled = False
        End Try
    End Sub
    Protected Sub EntrarClick(sender As Object, e As EventArgs) Handles entrar.Click
        Dim id As Integer
        Dim esAltaLocal = alta.Visible AndAlso Request.IsLocal
        Try
            If esAltaLocal Then
                If clave.Text <> claveConfirmada.Text Then Throw New ArgumentException("Las contraseñas no coinciden.")
                id = Servicio.CrearAdministrador(usuario.Text.Trim(), nombreCompleto.Text.Trim(), correo.Text.Trim(), clave.Text)
            Else
                id = Servicio.IniciarSesion(usuario.Text.Trim(), clave.Text)
            End If
        Catch ex As AccesoBloqueadoException
            mensaje.Text = ex.Message
            Return
        Catch ex As ArgumentException
            mensaje.Text = If(esAltaLocal, ex.Message, "No se pudo iniciar sesión. Verifique sus datos o intente de nuevo más tarde.")
            Return
        Catch ex As InvalidOperationException
            mensaje.Text = If(esAltaLocal, ex.Message, "No se pudo iniciar sesión. Verifique sus datos o intente de nuevo más tarde.")
            Return
        Catch ex As Exception
            Diagnostics.Trace.TraceError(ex.ToString())
            mensaje.Text = "No se pudo completar el acceso. Consulte al administrador."
            Return
        End Try
        ' Aplicar la duración configurada también a la primera cookie de acceso.
        Dim actor=Servicio.Usuario(id)
        Dim ahora=DateTime.Now
        Dim ticket As New FormsAuthenticationTicket(2,id.ToString(Globalization.CultureInfo.InvariantCulture),ahora,ahora.AddMinutes(actor.MinutosInactividad),False,"",FormsAuthentication.FormsCookiePath)
        Response.Cookies.Set(New HttpCookie(FormsAuthentication.FormsCookieName,FormsAuthentication.Encrypt(ticket)) With {.HttpOnly=True,.Secure=Request.IsSecureConnection,.Path=ticket.CookiePath})
        Session.Abandon()
        Response.Redirect("~/Default.aspx", False)
        Context.ApplicationInstance.CompleteRequest()
    End Sub
End Class
