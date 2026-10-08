Public Class Clientes
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub


    Protected Sub btnNuevoCliente_Click(sender As Object, e As EventArgs) Handles btnNuevoCliente.Click

        pnlFormularioCliente.Visible = True
        pnlListadoClientes.Visible = False

    End Sub


    Protected Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click

        pnlFormularioCliente.Visible = False
        pnlListadoClientes.Visible = True

    End Sub

End Class