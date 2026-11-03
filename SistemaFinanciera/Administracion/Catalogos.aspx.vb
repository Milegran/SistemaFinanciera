Option Strict On
Public Class CatalogosAdmin
    Inherits FinanzasPagina
    Private restaurarChecksEnviados As Boolean
    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False : Notificar("No tienes permiso para consultar catálogos.",True) : Return
        End If
        restaurarChecksEnviados = IsPostBack AndAlso Request.Form(btnGuardarEstados.UniqueID) IsNot Nothing
        Ejecutar(Sub() CargarCatalogo())
    End Sub
    Protected Sub SeleccionCatalogo(sender As Object,e As EventArgs) Handles ddlCatalogo.SelectedIndexChanged
        Ejecutar(Sub() CargarCatalogo())
    End Sub
    Private Sub CargarCatalogo()
        Dim datos=ServicioFinanzas.CatalogoAdministracion(Actor.Id,ddlCatalogo.SelectedValue)
        btnGuardarEstados.Visible=datos.Columns.Contains("Activo")
        grCatalogo.DataSource=datos : grCatalogo.DataBind()
        litTotal.Text=datos.Rows.Count.ToString("N0",Globalization.CultureInfo.GetCultureInfo("es-PA")) & " valores"
    End Sub

    Protected Sub CatalogoRowDataBound(sender As Object,e As GridViewRowEventArgs)
        If e.Row.RowType<>DataControlRowType.DataRow Then Return
        Dim vista=TryCast(e.Row.DataItem,DataRowView)
        If vista Is Nothing OrElse Not vista.Row.Table.Columns.Contains("Activo") Then Return
        Dim indice=vista.Row.Table.Columns.IndexOf("Activo")
        If indice<0 OrElse indice>=e.Row.Cells.Count Then Return
        Dim check As New CheckBox With {.ID="chkActivoCatalogo",.CssClass="catalog-active-check",.Checked=CBool(vista("Activo"))}
        check.InputAttributes("aria-label")="Estado activo para " & Convert.ToString(vista("Nombre"))
        e.Row.Cells(indice).Controls.Clear()
        e.Row.Cells(indice).HorizontalAlign=HorizontalAlign.Center
        e.Row.Cells(indice).Controls.Add(check)
        ' UniqueID incluye la ruta completa dentro del GridView solo después de agregar el control.
        If restaurarChecksEnviados Then check.Checked=Request.Form(check.UniqueID) IsNot Nothing
    End Sub

    Protected Sub GuardarEstados(sender As Object,e As EventArgs) Handles btnGuardarEstados.Click
        Ejecutar(Sub()
            If Not btnGuardarEstados.Visible Then Throw New ArgumentException("Este catálogo es de consulta y no permite editar estados.")
            Dim estados As New Dictionary(Of String,Boolean)(StringComparer.OrdinalIgnoreCase)
            For Each fila As GridViewRow In grCatalogo.Rows
                Dim check=TryCast(fila.FindControl("chkActivoCatalogo"),CheckBox)
                If check Is Nothing Then Continue For
                estados(CStr(grCatalogo.DataKeys(fila.RowIndex).Value))=check.Checked
            Next
            ServicioFinanzas.GuardarEstadosCatalogoAdministracion(Actor.Id,ddlCatalogo.SelectedValue,estados)
            restaurarChecksEnviados=False
            CargarCatalogo()
            Notificar("Los estados del catálogo se guardaron correctamente.")
        End Sub)
    End Sub
End Class
