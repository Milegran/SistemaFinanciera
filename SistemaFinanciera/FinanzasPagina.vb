Option Strict On
Imports System.Data
Imports System.Globalization
Imports System.Web.UI.HtmlControls
Imports SistemaFinanciera.BL
Imports SistemaFinanciera.DAL
Imports SistemaFinanciera.Entities

Public Class FinanzasPagina
    Inherits Page
    Protected Property Actor As UsuarioFinanzas
    Protected ReadOnly Property ServicioFinanzas As FinanzasServicio
        Get
            Return New FinanzasServicio(New FinanzasRepositorio(ConfigurationManager.ConnectionStrings("SistemaFinanciera").ConnectionString))
        End Get
    End Property
    Protected Overrides Sub OnInit(e As EventArgs)
        MyBase.OnInit(e)
        If Session("TokenFormulario") Is Nothing Then Session("TokenFormulario") = Guid.NewGuid().ToString("N")
        ViewStateUserKey = CStr(Session("TokenFormulario"))
        If Not Request.IsAuthenticated Then
            Response.Redirect("~/Acceso.aspx?expirada=1", True)
            Return
        End If
        Try
            Dim id As Integer
            If Not Integer.TryParse(User.Identity.Name, id) Then Throw New UnauthorizedAccessException()
            Actor = ServicioFinanzas.Usuario(id)
            Context.Items("CapitalWest.UsuarioFinanzas") = Actor
        Catch ex As Exception When TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is ArgumentException
            System.Web.Security.FormsAuthentication.SignOut()
            Response.Redirect("~/Acceso.aspx?expirada=1", True)
        End Try
    End Sub
    Protected Sub Notificar(texto As String, Optional esError As Boolean = False)
        Dim contenedor = Master.FindControl("MainContent")
        If contenedor Is Nothing Then Return
        Dim caja = TryCast(contenedor.FindControl("mensaje"), Literal)
        If caja Is Nothing Then Return
        Dim panel = TryCast(contenedor.FindControl("pnlMensaje"), HtmlGenericControl)
        If panel IsNot Nothing Then
            panel.Attributes("class") = If(esError, "alert alert-danger", "alert alert-success")
        End If
        caja.Text = texto
    End Sub
    Protected Sub Ejecutar(accion As Action)
        Try
            accion()
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is UnauthorizedAccessException
            Notificar(ex.Message, esError:=True)
        Catch ex As Exception
            Diagnostics.Trace.TraceError("Finanzas: {0}", ex.ToString())
            Notificar("No se pudo completar la operación. Compruebe la conexión o contacte al administrador.", esError:=True)
        End Try
    End Sub
    Protected Shared Function Numero(campo As TextBox, nombre As String) As Decimal
        Dim valor As Decimal
        If Not Decimal.TryParse(campo.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, valor) Then Throw New ArgumentException(nombre & ": escriba un número no negativo con punto decimal.")
        Return valor
    End Function
    Protected Shared Function Entero(campo As TextBox, nombre As String) As Integer
        Dim valor As Integer
        If Not Integer.TryParse(campo.Text, NumberStyles.None, CultureInfo.InvariantCulture, valor) Then Throw New ArgumentException(nombre & ": escriba un número entero.")
        Return valor
    End Function
    Protected Shared Function Fecha(campo As TextBox) As Date
        Dim valor As Date
        If Not Date.TryParseExact(campo.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, valor) Then Throw New ArgumentException("Seleccione una fecha válida.")
        Return valor
    End Function
    Protected Shared Function Seleccion(lista As DropDownList) As Integer
        Dim id As Integer
        If Not Integer.TryParse(lista.SelectedValue, id) OrElse id <= 0 Then Throw New ArgumentException("Seleccione una opción válida.")
        Return id
    End Function
    Protected Sub Lista(lista As ListControl, datos As DataTable, Optional valor As String = "Id", Optional etiqueta As String = "Nombre")
        lista.DataSource = datos : lista.DataValueField = valor : lista.DataTextField = etiqueta : lista.DataBind()
        If TypeOf lista Is DropDownList Then lista.Items.Insert(0, New ListItem("Seleccione…", ""))
    End Sub
    Protected Sub Vincular(grid As GridView, datos As Object)
        grid.DataSource = datos : grid.DataBind()
    End Sub
    Protected Shared Function Fila(tabla As DataTable) As DataRow
        If tabla.Rows.Count <> 1 Then Throw New ArgumentException("El registro ya no está disponible.")
        Return tabla.Rows(0)
    End Function
    Protected Shared Function Dinero(valor As Object) As String
        Return Convert.ToDecimal(valor).ToString("0.00", CultureInfo.InvariantCulture)
    End Function
End Class
