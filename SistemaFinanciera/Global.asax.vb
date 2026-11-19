Imports System.Web.Optimization

Public Class Global_asax
    Inherits HttpApplication

    Sub Application_PostAuthenticateRequest(sender As Object, e As EventArgs)
        ' La firma de FormsAuthentication puede sobrevivir a un reinicio; el identificador de ejecución no.
        If Request.IsAuthenticated AndAlso Not AutenticacionSesion.PerteneceAEstaEjecucion(Context.User) Then
            System.Web.Security.FormsAuthentication.SignOut()
            Context.User = New System.Security.Principal.GenericPrincipal(New System.Security.Principal.GenericIdentity(""), New String() {})
            System.Threading.Thread.CurrentPrincipal = Context.User
        End If
    End Sub

    Sub Application_Start(sender As Object, e As EventArgs)
        ' Se desencadena al iniciar la aplicación
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
    End Sub
End Class
