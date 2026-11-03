Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Text

Public Class Auditoria
    Inherits FinanzasPagina

    Protected Sub Cargar(sender As Object,e As EventArgs) Handles Me.Load
        If Not Actor.Puede("ADMINISTRADOR") Then
            contenidoAdmin.Visible=False
            Notificar("No tienes permiso para consultar la auditoría.",True)
            Return
        End If
        If Not IsPostBack Then
            Ejecutar(Sub()
                ddlUsuario.Items.Clear() : ddlUsuario.Items.Add(New ListItem("Todos los usuarios","0"))
                Dim usuarios=ServicioFinanzas.ConsultarUsuariosAdministracion(Actor.Id,"",0,"TODOS")
                For Each fila As DataRow In usuarios.Rows
                    ddlUsuario.Items.Add(New ListItem(CStr(fila("NombreCompleto")),CStr(fila("Id"))))
                Next
                CargarRegistros()
            End Sub)
        Else
            Ejecutar(Sub() CargarRegistros())
        End If
    End Sub

    Private Function FechaFiltro(campo As TextBox) As Nullable(Of Date)
        If String.IsNullOrWhiteSpace(campo.Text) Then Return Nothing
        Return Fecha(campo)
    End Function

    Private Function Consultar() As DataTable
        Dim usuarioId As Integer=0
        Integer.TryParse(ddlUsuario.SelectedValue,usuarioId)
        Return ServicioFinanzas.ConsultarAuditoria(Actor.Id,usuarioId,ddlModulo.SelectedValue,ddlAccion.SelectedValue,FechaFiltro(txtDesde),FechaFiltro(txtHasta))
    End Function

    Private Sub CargarRegistros()
        Dim datos=Consultar()
        Vincular(grAuditoria,datos)
        litConteo.Text=datos.Rows.Count.ToString("N0",CultureInfo.GetCultureInfo("es-PA")) & " registros encontrados · página " & (grAuditoria.PageIndex+1).ToString() & " de " & Math.Max(1,grAuditoria.PageCount).ToString() & " (máximo 2,000). La bitácora es de solo lectura."
    End Sub

    Protected Sub Filtrar(sender As Object,e As EventArgs) Handles btnFiltrar.Click
        grAuditoria.PageIndex=0
        Ejecutar(Sub() CargarRegistros())
    End Sub

    Protected Sub Limpiar(sender As Object,e As EventArgs) Handles btnLimpiar.Click
        ddlUsuario.SelectedIndex=0 : ddlModulo.SelectedIndex=0 : ddlAccion.SelectedIndex=0
        txtDesde.Text="" : txtHasta.Text=""
        grAuditoria.PageIndex=0
        Ejecutar(Sub() CargarRegistros())
    End Sub

    Protected Sub PaginarAuditoria(sender As Object,e As GridViewPageEventArgs)
        grAuditoria.PageIndex=Math.Max(0,e.NewPageIndex)
        Ejecutar(Sub() CargarRegistros())
    End Sub

    Protected Sub MostrarDetalle(sender As Object,e As GridViewCommandEventArgs) Handles grAuditoria.RowCommand
        If e.CommandName<>"Detalle" Then Return
        Ejecutar(Sub()
            Dim rowIndex As Integer
            If Not Integer.TryParse(Convert.ToString(e.CommandArgument),rowIndex) OrElse rowIndex<0 OrElse rowIndex>=grAuditoria.DataKeys.Count Then Throw New ArgumentException("El registro ya no está disponible.")
            Dim clave=grAuditoria.DataKeys(rowIndex)
            Dim detalle=Fila(ServicioFinanzas.DetalleAuditoria(Actor.Id,CLng(clave("Id"))))
            litFecha.Text=Convert.ToString(detalle("Fecha"))
            litUsuario.Text=Convert.ToString(detalle("Usuario")) & " · " & Convert.ToString(detalle("Rol"))
            litAccion.Text=Convert.ToString(detalle("Modulo")) & " · " & Convert.ToString(detalle("Accion"))
            litReferencia.Text=Convert.ToString(detalle("Entidad")) & " #" & Convert.ToString(detalle("RegistroId"))
            litDescripcion.Text=Convert.ToString(detalle("Descripcion"))
            litAnterior.Text=If(String.IsNullOrWhiteSpace(Convert.ToString(detalle("ValorAnterior"))),"Sin valor anterior",Convert.ToString(detalle("ValorAnterior")))
            litNuevo.Text=If(String.IsNullOrWhiteSpace(Convert.ToString(detalle("ValorNuevo"))),"Sin valor nuevo",Convert.ToString(detalle("ValorNuevo")))
            litIp.Text=If(String.IsNullOrWhiteSpace(Convert.ToString(detalle("DireccionIp"))),"No registrada",Convert.ToString(detalle("DireccionIp")))
            modalDetalle.Visible=True
        End Sub)
    End Sub

    Protected Sub CerrarDetalle(sender As Object,e As EventArgs) Handles btnCerrarDetalle.Click
        modalDetalle.Visible=False
    End Sub

    Protected Sub Exportar(sender As Object,e As EventArgs) Handles btnExportar.Click
        Ejecutar(Sub()
            Dim datos=Consultar()
            Dim csv As New StringBuilder()
            csv.AppendLine("Fecha y hora,Usuario,Rol,Modulo,Accion,Entidad,Referencia,Descripcion,Valor anterior,Valor nuevo,Direccion IP")
            For Each fila As DataRow In datos.Rows
                Dim campos={"Fecha","Usuario","Rol","Modulo","Accion","Entidad","RegistroId","Descripcion","ValorAnterior","ValorNuevo","DireccionIp"}
                csv.AppendLine(String.Join(",",campos.Select(Function(nombre) EscaparCsv(If(datos.Columns.Contains(nombre),Convert.ToString(fila(nombre)),"")))))
            Next
            Response.Clear()
            Response.ContentType="text/csv"
            Response.ContentEncoding=Encoding.UTF8
            Response.AddHeader("Content-Disposition","attachment; filename=auditoria_capitalwest_" & DateTime.UtcNow.AddHours(-5).ToString("yyyyMMdd_HHmm",CultureInfo.InvariantCulture) & ".csv")
            Response.Write(ChrW(&HFEFF) & csv.ToString())
            Response.Flush()
            Response.SuppressContent=True
            Context.ApplicationInstance.CompleteRequest()
        End Sub)
    End Sub

    Private Shared Function EscaparCsv(valor As String) As String
        Dim limpio=If(valor," ").Replace(vbCr," ").Replace(vbLf," ").Trim()
        If limpio.StartsWith("=") OrElse limpio.StartsWith("+") OrElse limpio.StartsWith("-") OrElse limpio.StartsWith("@") Then limpio="'" & limpio
        Return ChrW(34) & limpio.Replace(ChrW(34),ChrW(34) & ChrW(34)) & ChrW(34)
    End Function
End Class
