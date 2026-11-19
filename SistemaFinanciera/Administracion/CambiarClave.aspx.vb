Option Strict On
Public Class CambiarClave
    Inherits FinanzasPagina
    Protected Sub PrepararPolitica(sender As Object, e As EventArgs) Handles Me.PreRender
        Try
            Dim reglas = ServicioFinanzas.PoliticaClaveActual()
            txtNueva.Attributes("data-password-policy") = "true"
            txtNueva.Attributes("data-obligatoria") = "1"
            For Each regla In reglas
                txtNueva.Attributes("data-" & regla.Key) = regla.Value.ToString(Globalization.CultureInfo.InvariantCulture)
            Next
            For Each campo In {txtNueva, txtConfirmar}
                campo.Attributes("minlength") = reglas("minimo").ToString(Globalization.CultureInfo.InvariantCulture)
                campo.MaxLength = reglas("maximo")
            Next
            txtConfirmar.Attributes("data-igual") = txtNueva.ClientID
        Catch ex As Exception
            Diagnostics.Trace.TraceError(ex.ToString())
            Notificar("No se pudo cargar la política de contraseña. Inténtelo de nuevo.", True)
            btnCambiar.Enabled = False
        End Try
    End Sub
    Protected Sub Guardar(sender As Object,e As EventArgs) Handles btnCambiar.Click
        If txtNueva.Text<>txtConfirmar.Text Then
            Notificar("Las contraseñas no coinciden.",True)
            Return
        End If
        Ejecutar(Sub()
            ServicioFinanzas.CambiarClavePropia(Actor.Id,txtNueva.Text)
            ' El cambio incrementa VersionSesion. Conservar solo esta sesión, con la versión nueva.
            Actor = ServicioFinanzas.Usuario(Actor.Id)
            AutenticacionSesion.Emitir(Context, Actor)
            Session.Abandon()
            Response.Redirect("~/Default.aspx",False)
            Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub
End Class
