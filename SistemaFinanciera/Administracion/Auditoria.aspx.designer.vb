Option Strict On
Option Explicit On
Partial Public Class Auditoria
    Protected WithEvents pnlMensaje As Global.System.Web.UI.HtmlControls.HtmlGenericControl
    Protected WithEvents mensaje As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents contenidoAdmin As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents ddlUsuario As Global.System.Web.UI.WebControls.DropDownList
    Protected WithEvents ddlModulo As Global.System.Web.UI.WebControls.DropDownList
    Protected WithEvents ddlAccion As Global.System.Web.UI.WebControls.DropDownList
    Protected WithEvents txtDesde As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtHasta As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents btnFiltrar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents btnLimpiar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents btnExportar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents litConteo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents grAuditoria As Global.System.Web.UI.WebControls.GridView
    Protected WithEvents modalDetalle As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents btnCerrarDetalle As Global.System.Web.UI.WebControls.Button
    Protected WithEvents litFecha As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litUsuario As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litAccion As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litReferencia As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litDescripcion As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litAnterior As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litNuevo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litIp As Global.System.Web.UI.WebControls.Literal
End Class
