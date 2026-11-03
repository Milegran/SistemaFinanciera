Option Strict On
Imports System.Data

Public Class MetodosCalculoAdmin
    Inherits FinanzasPagina
    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False : Notificar("No tienes permiso para administrar métodos de cálculo.",True) : Return
        End If
        If Not IsPostBack Then
            Ejecutar(Sub()
                CargarMetodos()
                If Request.QueryString("detalle")="1" AndAlso grMetodos.DataKeys.Count>0 Then AbrirDetalle(grMetodos.DataKeys(0))
            End Sub)
        ElseIf Not modalDetalle.Visible AndAlso Not modalEditarMetodo.Visible Then
            Ejecutar(Sub() CargarMetodos())
        End If
    End Sub
    Private Sub CargarMetodos()
        Vincular(grMetodos,ServicioFinanzas.MetodosAdministracion(Actor.Id))
    End Sub
    Protected Sub MetodosRowDataBound(sender As Object,e As Global.System.Web.UI.WebControls.GridViewRowEventArgs)
        If e.Row.RowType <> Global.System.Web.UI.WebControls.DataControlRowType.DataRow Then Return
        Dim accion = TryCast(e.Row.FindControl("lnkEstadoMetodo"), Global.System.Web.UI.WebControls.LinkButton)
        If accion Is Nothing Then Return
        Dim estaActivo = CBool(grMetodos.DataKeys(e.Row.RowIndex).Values("Activo"))
        accion.CssClass = "action-icon action-toggle"
        accion.ToolTip = If(estaActivo,"Desactivar método","Activar método")
        accion.Attributes("aria-label") = accion.ToolTip
        accion.OnClientClick = If(estaActivo, "return confirm('¿Desactivar el método para nuevos productos y solicitudes?');", "return confirm('¿Reactivar el método?');")
    End Sub
    Protected Sub ComandoMetodo(sender As Object,e As GridViewCommandEventArgs) Handles grMetodos.RowCommand
        Dim index As Integer
        If Not Integer.TryParse(Convert.ToString(e.CommandArgument),index) OrElse index<0 OrElse index>=grMetodos.DataKeys.Count Then Return
        Dim fila=grMetodos.DataKeys(index)
        Dim id=CInt(fila("Id"))
        If e.CommandName="Estado" Then
            Ejecutar(Sub()
                Dim activo=Not CBool(fila("Activo"))
                ServicioFinanzas.CambiarEstadoMetodoAdministracion(Actor.Id,id,activo)
                CargarMetodos()
                Notificar(If(activo,"Método activado.","Método desactivado para nuevas operaciones."))
            End Sub)
        ElseIf e.CommandName="Detalle" Then
            AbrirDetalle(fila)
        ElseIf e.CommandName="Editar" Then
            hfMetodoId.Value=id.ToString(Globalization.CultureInfo.InvariantCulture)
            txtNombreMetodo.Text=CStr(fila("Nombre"))
            txtDescripcionMetodo.Text=CStr(fila("Descripcion"))
            txtFormulaMetodo.Text=If(IsDBNull(fila("Formula")),"",CStr(fila("Formula")))
            txtVariablesMetodo.Text=If(IsDBNull(fila("Variables")),"",CStr(fila("Variables")))
            txtAlgoritmoMetodo.Text=If(IsDBNull(fila("Algoritmo")),"",CStr(fila("Algoritmo")))
            txtEjemploMetodo.Text=If(IsDBNull(fila("EjemploManual")),"",CStr(fila("EjemploManual")))
            litCodigoEditar.Text=CStr(fila("Codigo"))
            modalEditarMetodo.Visible=True
        End If
    End Sub
    Private Sub AbrirDetalle(fila As DataKey)
        litNombre.Text=CStr(fila("Nombre")) : litCodigo.Text=CStr(fila("Codigo"))
        litDescripcion.Text=CStr(fila("Descripcion")) : litClase.Text=If(IsDBNull(fila("ClaseImplementacion")),"Pendiente",CStr(fila("ClaseImplementacion")))
        litFormula.Text=If(IsDBNull(fila("Formula")),"No documentada",CStr(fila("Formula")))
        litVariables.Text=If(IsDBNull(fila("Variables")),"No documentadas",CStr(fila("Variables")))
        litAlgoritmo.Text=If(IsDBNull(fila("Algoritmo")),"No documentado",CStr(fila("Algoritmo")))
        litEjemplo.Text=If(IsDBNull(fila("EjemploManual")),"No documentado",CStr(fila("EjemploManual")))
        modalDetalle.Visible=True
    End Sub
    Protected Sub Cerrar(sender As Object,e As EventArgs) Handles btnCerrar.Click
        modalDetalle.Visible=False
    End Sub

    Protected Sub CancelarEdicion(sender As Object,e As EventArgs) Handles btnCancelarEdicion.Click
        modalEditarMetodo.Visible=False
    End Sub

    Protected Sub GuardarEdicion(sender As Object,e As EventArgs) Handles btnGuardarMetodo.Click
        Ejecutar(Sub()
            Dim id As Integer
            If Not Integer.TryParse(hfMetodoId.Value,id) Then Throw New ArgumentException("Seleccione un método de cálculo.")
            ServicioFinanzas.GuardarMetodoAdministracion(Actor.Id,id,txtNombreMetodo.Text,txtDescripcionMetodo.Text,txtFormulaMetodo.Text,txtVariablesMetodo.Text,txtAlgoritmoMetodo.Text,txtEjemploMetodo.Text)
            modalEditarMetodo.Visible=False
            CargarMetodos()
            Notificar("La ficha del método de cálculo se actualizó correctamente.")
        End Sub)
    End Sub
End Class
