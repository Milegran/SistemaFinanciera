Option Strict On
Option Explicit On
Partial Public Class Prestamos
    Protected WithEvents pnlMensaje As Global.System.Web.UI.HtmlControls.HtmlGenericControl
    Protected WithEvents mensaje As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents formalizacion As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents solicitud As Global.System.Web.UI.WebControls.DropDownList
    Protected WithEvents primerPago As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents condiciones As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents desembolsar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents filtroTodos As Global.System.Web.UI.WebControls.LinkButton
    Protected WithEvents filtroVigentes As Global.System.Web.UI.WebControls.LinkButton
    Protected WithEvents filtroMora As Global.System.Web.UI.WebControls.LinkButton
    Protected WithEvents filtroCancelados As Global.System.Web.UI.WebControls.LinkButton
    Protected WithEvents filtroBusqueda As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents filtroEstado As Global.System.Web.UI.WebControls.DropDownList
    Protected WithEvents btnBuscar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents btnLimpiar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents litConteo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents listado As Global.System.Web.UI.WebControls.GridView
End Class
