Option Strict On
Public Class RolesAdmin
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False : Notificar("No tienes permiso para consultar los roles.",True) : Return
        End If
        If Not IsPostBack Then
            Ejecutar(Sub()
                         repRoles.DataSource = ServicioFinanzas.RolesAdministracion(Actor.Id)
                         repRoles.DataBind()
                     End Sub)
        End If
    End Sub
End Class
