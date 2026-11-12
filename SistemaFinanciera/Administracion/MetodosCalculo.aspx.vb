Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Text
Imports SistemaFinanciera.BL

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
        ElseIf Not modalDetalle.Visible AndAlso Not modalEditarMetodo.Visible AndAlso Not modalCrearMetodo.Visible Then
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
            txtFormulaMetodo.Text=ValorFila(fila,"Formula")
            txtVariablesMetodo.Text=ValorFila(fila,"Variables")
            txtAlgoritmoMetodo.Text=ValorFila(fila,"Algoritmo")
            txtEjemploMetodo.Text=ValorFila(fila,"EjemploManual")
            litCodigoEditar.Text=CStr(fila("Codigo"))
            Dim esExpresion=FinanzasServicio.EsMetodoExpresion(fila("ExprCapital"),fila("ExprInteres"))
            pnlExprInteresEditar.Visible=esExpresion : txtExprInteresMetodo.Text=ValorFila(fila,"ExprInteres")
            pnlExprCapitalEditar.Visible=esExpresion : txtExprCapitalMetodo.Text=ValorFila(fila,"ExprCapital")
            pnlExprSaldoEditar.Visible=esExpresion : txtExprSaldoMetodo.Text=ValorFila(fila,"ExprSaldo")
            pnlErrorEditar.Visible=False : litErrorEditar.Text=""
            modalEditarMetodo.Visible=True
        End If
    End Sub
    Private Shared Function ValorFila(fila As DataKey,nombre As String) As String
        Dim valor=fila(nombre)
        Return If(valor Is Nothing OrElse IsDBNull(valor),"",CStr(valor))
    End Function
    Private Shared Function MetodoConExpresion(fila As DataKey) As Boolean
        Return FinanzasServicio.EsMetodoExpresion(fila("ExprCapital"),fila("ExprInteres"))
    End Function
    Private Sub AbrirDetalle(fila As DataKey)
        litNombre.Text=CStr(fila("Nombre")) : litCodigo.Text=CStr(fila("Codigo"))
        litDescripcion.Text=CStr(fila("Descripcion")) : litClase.Text=If(IsDBNull(fila("ClaseImplementacion")),"Pendiente",CStr(fila("ClaseImplementacion")))
        litFormula.Text=If(IsDBNull(fila("Formula")),"No documentada",CStr(fila("Formula")))
        litVariables.Text=If(String.IsNullOrWhiteSpace(ValorFila(fila,"Variables")),"No documentadas",ValorFila(fila,"Variables"))
        litAlgoritmo.Text=If(String.IsNullOrWhiteSpace(ValorFila(fila,"Algoritmo")),"No documentado",ValorFila(fila,"Algoritmo"))
        litEjemplo.Text=If(String.IsNullOrWhiteSpace(ValorFila(fila,"EjemploManual")),"No documentado",ValorFila(fila,"EjemploManual"))
        Dim esExpresion=MetodoConExpresion(fila)
        pnlExprInteresDetalle.Visible=esExpresion : litExprInteres.Text=ValorFila(fila,"ExprInteres")
        pnlExprCapitalDetalle.Visible=esExpresion : litExprCapital.Text=ValorFila(fila,"ExprCapital")
        pnlExprSaldoDetalle.Visible=esExpresion : litExprSaldo.Text=If(String.IsNullOrWhiteSpace(ValorFila(fila,"ExprSaldo")),"saldo - capital (predeterminada)",ValorFila(fila,"ExprSaldo"))
        modalDetalle.Visible=True
    End Sub
    Protected Sub Cerrar(sender As Object,e As EventArgs) Handles btnCerrar.Click
        modalDetalle.Visible=False
    End Sub

    Protected Sub CancelarEdicion(sender As Object,e As EventArgs) Handles btnCancelarEdicion.Click
        modalEditarMetodo.Visible=False
    End Sub

    Protected Sub GuardarEdicion(sender As Object,e As EventArgs) Handles btnGuardarMetodo.Click
        Try
            Dim id As Integer
            If Not Integer.TryParse(hfMetodoId.Value,id) Then Throw New ArgumentException("Seleccione un método de cálculo.")
            ServicioFinanzas.GuardarMetodoAdministracion(Actor.Id,id,txtNombreMetodo.Text,txtDescripcionMetodo.Text,txtVariablesMetodo.Text,txtExprInteresMetodo.Text,txtExprCapitalMetodo.Text,txtExprSaldoMetodo.Text,txtFormulaMetodo.Text,txtAlgoritmoMetodo.Text,txtEjemploMetodo.Text)
            modalEditarMetodo.Visible=False
            CargarMetodos()
            Notificar("El método de cálculo se actualizó correctamente.")
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is UnauthorizedAccessException
            MostrarErrorEditar(ex.Message)
        Catch ex As Exception
            Diagnostics.Trace.TraceError("MetodosCalculo: {0}", ex.ToString())
            MostrarErrorEditar("No se pudo guardar el método. Compruebe la conexión o contacte al administrador.")
        End Try
    End Sub

    Private Sub MostrarErrorEditar(mensaje As String)
        litErrorEditar.Text=Server.HtmlEncode(mensaje)
        pnlErrorEditar.Visible=True
        modalEditarMetodo.Visible=True
    End Sub

    Protected Sub AbrirCreacion(sender As Object,e As EventArgs) Handles btnNuevoMetodo.Click
        Ejecutar(Sub()
            txtCodigoNuevoMetodo.Text=""
            txtNombreNuevoMetodo.Text=""
            txtDescripcionNuevoMetodo.Text=""
            txtFormulaNuevoMetodo.Text=""
            txtVariablesNuevoMetodo.Text=""
            txtExprInteresNuevoMetodo.Text=""
            txtExprCapitalNuevoMetodo.Text=""
            txtExprSaldoNuevoMetodo.Text=""
            txtAlgoritmoNuevoMetodo.Text=""
            txtEjemploNuevoMetodo.Text=""
            pnlErrorCrear.Visible=False : litErrorCrear.Text=""
            pnlPreviewCrear.Visible=False : litPreviewCrear.Text=""
            btnProbarNuevoMetodo.Attributes("formnovalidate")="formnovalidate"
            modalCrearMetodo.Visible=True
        End Sub)
    End Sub

    Protected Sub CancelarCreacion(sender As Object,e As EventArgs) Handles btnCancelarCreacion.Click
        modalCrearMetodo.Visible=False
    End Sub

    Protected Sub ProbarSistema(sender As Object,e As EventArgs) Handles btnProbarNuevoMetodo.Click
        Try
            Dim vista=ServicioFinanzas.ProbarSistemaExpresiones(txtVariablesNuevoMetodo.Text,txtExprInteresNuevoMetodo.Text,txtExprCapitalNuevoMetodo.Text,txtExprSaldoNuevoMetodo.Text)
            litPreviewCrear.Text="<span class=""preview-caption"">Vista previa: calendario de prueba de 12 cuotas con monto de B/. 1,000.00 y tasa anual 24% (nominal).</span>" & CalendarioHtml(vista)
            pnlPreviewCrear.Visible=True
            pnlErrorCrear.Visible=False : litErrorCrear.Text=""
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is UnauthorizedAccessException
            MostrarErrorCrear(ex.Message)
        Catch ex As Exception
            Diagnostics.Trace.TraceError("MetodosCalculo: {0}", ex.ToString())
            MostrarErrorCrear("No se pudo ejecutar la prueba. Compruebe la conexión o contacte al administrador.")
        End Try
        modalCrearMetodo.Visible=True
    End Sub

    Protected Sub GuardarNuevoMetodo(sender As Object,e As EventArgs) Handles btnGuardarNuevoMetodo.Click
        Try
            Dim id=ServicioFinanzas.CrearMetodoAdministracion(Actor.Id,txtCodigoNuevoMetodo.Text,txtNombreNuevoMetodo.Text,txtDescripcionNuevoMetodo.Text,txtVariablesNuevoMetodo.Text,txtExprInteresNuevoMetodo.Text,txtExprCapitalNuevoMetodo.Text,txtExprSaldoNuevoMetodo.Text,txtFormulaNuevoMetodo.Text,txtAlgoritmoNuevoMetodo.Text,txtEjemploNuevoMetodo.Text)
            modalCrearMetodo.Visible=False
            CargarMetodos()
            Notificar("Método de cálculo creado. Asócialo a un producto para usarlo en simulador y solicitudes.")
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is UnauthorizedAccessException
            MostrarErrorCrear(ex.Message)
        Catch ex As Exception
            Diagnostics.Trace.TraceError("MetodosCalculo: {0}", ex.ToString())
            MostrarErrorCrear("No se pudo crear el método. Compruebe la conexión o contacte al administrador.")
        End Try
    End Sub

    Private Sub MostrarErrorCrear(mensaje As String)
        litErrorCrear.Text=Server.HtmlEncode(mensaje)
        pnlErrorCrear.Visible=True
        pnlPreviewCrear.Visible=False
        modalCrearMetodo.Visible=True
    End Sub

    Private Shared Function CalendarioHtml(vista As DataTable) As String
        Dim sb As New StringBuilder()
        sb.Append("<table><thead><tr><th>Cuota</th><th>Fecha</th><th>Capital</th><th>Interés</th><th>Cuota total</th><th>Saldo</th></tr></thead><tbody>")
        For Each fila As DataRow In vista.Rows
            sb.Append("<tr><td>").Append(CInt(fila("Numero"))).Append("</td><td>").Append(CDate(fila("Fecha")).ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)).Append("</td><td>")
            sb.Append(CDec(fila("Capital")).ToString("0.00",CultureInfo.InvariantCulture)).Append("</td><td>")
            sb.Append(CDec(fila("Interes")).ToString("0.00",CultureInfo.InvariantCulture)).Append("</td><td>")
            sb.Append(CDec(fila("Cuota")).ToString("0.00",CultureInfo.InvariantCulture)).Append("</td><td>")
            sb.Append(CDec(fila("Saldo")).ToString("0.00",CultureInfo.InvariantCulture)).Append("</td></tr>")
        Next
        sb.Append("</tbody></table>")
        Return sb.ToString()
    End Function
End Class
