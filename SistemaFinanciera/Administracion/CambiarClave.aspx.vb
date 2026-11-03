Option Strict On
Public Class CambiarClave
    Inherits FinanzasPagina
    Protected Sub Guardar(sender As Object,e As EventArgs) Handles btnCambiar.Click
        If txtNueva.Text<>txtConfirmar.Text Then
            Notificar("Las contraseñas no coinciden.",True)
            Return
        End If
        Ejecutar(Sub()
            ServicioFinanzas.CambiarClavePropia(Actor.Id,txtNueva.Text)
            Response.Redirect("~/Default.aspx",False)
            Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub
End Class
