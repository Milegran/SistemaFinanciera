Imports System.Globalization
Imports System.Linq
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Imports SistemaFinanciera.Entities

Public Class SiteMaster
    Inherits MasterPage
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

        If usuario Is Nothing Then
            litUsuarioNombre.Text = "Usuario"
            litUsuarioRol.Text = "Sin sesión"
            litUsuarioIniciales.Text = "CW"
            Return
        End If

        litUsuarioNombre.Text = If(String.IsNullOrWhiteSpace(usuario.Nombre), "Usuario", usuario.Nombre.Trim())
        litUsuarioRol.Text = If(usuario.Roles Is Nothing OrElse usuario.Roles.Length = 0,
                                "Sin rol",
                                String.Join(" · ", usuario.Roles.Select(Function(rol) RolVisible(rol))))
        litUsuarioIniciales.Text = Iniciales(litUsuarioNombre.Text)
    End Sub
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
