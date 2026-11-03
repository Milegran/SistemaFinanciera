Option Strict On
Option Explicit On
Partial Public Class MetodosCalculoAdmin
    Protected WithEvents pnlMensaje As Global.System.Web.UI.HtmlControls.HtmlGenericControl
    Protected WithEvents mensaje As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents contenidoAdmin As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents grMetodos As Global.System.Web.UI.WebControls.GridView
    Protected WithEvents modalDetalle As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents btnCerrar As Global.System.Web.UI.WebControls.Button
    Protected WithEvents litNombre As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litCodigo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litDescripcion As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litClase As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litFormula As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litVariables As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litAlgoritmo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents litEjemplo As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents modalEditarMetodo As Global.System.Web.UI.WebControls.Panel
    Protected WithEvents hfMetodoId As Global.System.Web.UI.WebControls.HiddenField
    Protected WithEvents litCodigoEditar As Global.System.Web.UI.WebControls.Literal
    Protected WithEvents txtNombreMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtDescripcionMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtFormulaMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtVariablesMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtAlgoritmoMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents txtEjemploMetodo As Global.System.Web.UI.WebControls.TextBox
    Protected WithEvents btnCancelarEdicion As Global.System.Web.UI.WebControls.Button
    Protected WithEvents btnGuardarMetodo As Global.System.Web.UI.WebControls.Button
End Class
