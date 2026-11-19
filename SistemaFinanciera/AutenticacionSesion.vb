Option Strict On
Imports System.Globalization
Imports System.Security.Principal
Imports System.Web.Security
Imports SistemaFinanciera.Entities

''' <summary>Vincula cada ticket a esta ejecución y a la versión de seguridad del usuario.</summary>
Public NotInheritable Class AutenticacionSesion
    Private Shared ReadOnly ejecucion As String = Guid.NewGuid().ToString("N")
    Private Sub New()
    End Sub

    Public Shared Function PerteneceAEstaEjecucion(principal As IPrincipal) As Boolean
        Dim identidad = TryCast(If(principal Is Nothing, Nothing, principal.Identity), FormsIdentity)
        If identidad Is Nothing OrElse Not identidad.IsAuthenticated OrElse identidad.Ticket.Expired Then Return False
        Dim partes = identidad.Ticket.UserData.Split(":"c)
        Return partes.Length = 3 AndAlso partes(0) = "cw1" AndAlso partes(1) = ejecucion
    End Function

    Public Shared Function VersionVigente(principal As IPrincipal, actor As UsuarioFinanzas) As Boolean
        If actor Is Nothing OrElse Not PerteneceAEstaEjecucion(principal) Then Return False
        Dim identidad = DirectCast(principal.Identity, FormsIdentity)
        Dim version As Long
        Return identidad.Name = actor.Id.ToString(CultureInfo.InvariantCulture) AndAlso
            Long.TryParse(identidad.Ticket.UserData.Split(":"c)(2), NumberStyles.None, CultureInfo.InvariantCulture, version) AndAlso
            version = actor.VersionSesion
    End Function

    Public Shared Sub Emitir(contexto As HttpContext, actor As UsuarioFinanzas)
        Dim ahora = DateTime.Now
        Dim datos = "cw1:" & ejecucion & ":" & actor.VersionSesion.ToString(CultureInfo.InvariantCulture)
        Dim ticket As New FormsAuthenticationTicket(2, actor.Id.ToString(CultureInfo.InvariantCulture), ahora,
            ahora.AddMinutes(actor.MinutosInactividad), False, datos, FormsAuthentication.FormsCookiePath)
        contexto.Response.Cookies.Set(New HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket)) With {
            .HttpOnly = True, .Secure = contexto.Request.IsSecureConnection, .Path = ticket.CookiePath,
            .SameSite = SameSiteMode.Lax})
    End Sub
End Class
